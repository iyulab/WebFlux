using System.Text.Json;
using System.Text.RegularExpressions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace WebFlux.Services;

/// <summary>
/// API 문서 추출 및 분석기
/// OpenAPI, Swagger, GraphQL 등 다양한 API 문서 형식을 분석하여 API 구조를 파악
/// </summary>
public class APIDocumentationExtractor : IAPIDocumentationExtractor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<APIDocumentationExtractor> _logger;

    /// <summary>
    /// 일반적인 API 문서 경로들
    /// </summary>
    private static readonly Dictionary<APIDocumentationType, List<string>> APIDocumentPaths = new()
    {
        [APIDocumentationType.OpenAPI30] = new() { "openapi.json", "openapi.yaml", "api-docs", "swagger.json", "swagger.yaml" },
        [APIDocumentationType.Swagger20] = new() { "swagger.json", "swagger.yaml", "api-docs/swagger.json" },
        [APIDocumentationType.GraphQL] = new() { "graphql", "graphql/schema", "api/graphql" },
        [APIDocumentationType.Postman] = new() { "postman_collection.json", "api.postman_collection.json" },
        [APIDocumentationType.RAML] = new() { "api.raml", "api.yaml" },
        [APIDocumentationType.APIBlueprint] = new() { "api.md", "apiary.apib" },
        [APIDocumentationType.AsyncAPI] = new() { "asyncapi.json", "asyncapi.yaml" },
        [APIDocumentationType.WSDL] = new() { "service.wsdl", "api.wsdl" }
    };

    public APIDocumentationExtractor(HttpClient httpClient, ILogger<APIDocumentationExtractor> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<APIDocumentationAnalysisResult> AnalyzeFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API 문서 분석 시작: {BaseUrl}", baseUrl);

        var result = new APIDocumentationAnalysisResult();
        var discoveredDocs = new List<APIDocumentInfo>();

        // 1. API 문서 발견
        foreach (var (docType, paths) in APIDocumentPaths)
        {
            foreach (var path in paths)
            {
                var docInfo = await TryDiscoverAPIDocumentAsync(baseUrl, path, docType, cancellationToken);
                if (docInfo != null)
                {
                    discoveredDocs.Add(docInfo);
                }
            }
        }

        // 2. HTML 페이지에서 API 문서 링크 발견
        await DiscoverAPIDocsFromHtmlAsync(baseUrl, discoveredDocs, cancellationToken);

        result.DiscoveredDocuments = discoveredDocs;

        if (discoveredDocs.Any())
        {
            // 3. 주요 API 문서 선택 및 분석
            var primaryDoc = SelectPrimaryAPIDocument(discoveredDocs);
            if (primaryDoc != null)
            {
                var apiMetadata = await ExtractAPIMetadataAsync(primaryDoc.Url, primaryDoc.DocumentationType, cancellationToken);
                result.PrimaryAPI = apiMetadata;

                // 4. 엔드포인트 분석
                result.EndpointAnalysis = await AnalyzeEndpointsAsync(apiMetadata);

                // 5. 스키마 분석
                result.SchemaAnalysis = await AnalyzeSchemasAsync(apiMetadata);

                // 6. API 품질 평가
                result.QualityEvaluation = await EvaluateAPIQualityAsync(apiMetadata);

                // 7. 사용 예제 생성
                result.UsageExamples = await GenerateUsageExamplesAsync(apiMetadata);
            }

            // 8. 품질 점수 계산
            result.QualityScore = CalculateOverallQualityScore(result);
        }

        _logger.LogInformation("API 문서 분석 완료: {DocCount}개 문서 발견, 품질 점수: {QualityScore:F2}",
            discoveredDocs.Count, result.QualityScore);

        return result;
    }

    /// <inheritdoc />
    public async Task<APIMetadata> ExtractAPIMetadataAsync(string apiDocUrl, APIDocumentationType documentationType, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _httpClient.GetStringAsync(apiDocUrl, cancellationToken);

            return documentationType switch
            {
                APIDocumentationType.OpenAPI30 or APIDocumentationType.OpenAPI31 => ParseOpenAPIDocument(content, apiDocUrl),
                APIDocumentationType.Swagger20 => ParseSwaggerDocument(content, apiDocUrl),
                APIDocumentationType.GraphQL => ParseGraphQLSchema(content, apiDocUrl),
                APIDocumentationType.Postman => ParsePostmanCollection(content, apiDocUrl),
                APIDocumentationType.RAML => ParseRAMLDocument(content, apiDocUrl),
                APIDocumentationType.AsyncAPI => ParseAsyncAPIDocument(content, apiDocUrl),
                APIDocumentationType.WSDL => ParseWSDLDocument(content, apiDocUrl),
                _ => new APIMetadata { DocumentationType = documentationType, RawContent = content }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API 문서 추출 실패: {Url}", apiDocUrl);
            return new APIMetadata { DocumentationType = documentationType };
        }
    }

    /// <inheritdoc />
    public async Task<EndpointAnalysisResult> AnalyzeEndpointsAsync(APIMetadata apiMetadata)
    {
        var result = new EndpointAnalysisResult
        {
            TotalEndpoints = apiMetadata.Endpoints.Count
        };

        // HTTP 메서드별 분포
        result.MethodDistribution = apiMetadata.Endpoints
            .GroupBy(e => e.Method.ToUpper())
            .ToDictionary(g => g.Key, g => g.Count());

        // 태그별 분포
        result.TagDistribution = apiMetadata.Endpoints
            .SelectMany(e => e.Tags)
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        // 보안이 필요한 엔드포인트
        result.SecuredEndpoints = apiMetadata.Endpoints.Count(e => e.Security.Any());

        // 사용 중단된 엔드포인트
        result.DeprecatedEndpoints = apiMetadata.Endpoints.Count(e => e.Deprecated);

        // 복잡도 점수 계산
        result.ComplexityScore = CalculateEndpointComplexity(apiMetadata.Endpoints);

        // 커버리지 점수 계산
        result.CoverageScore = CalculateEndpointCoverage(apiMetadata.Endpoints);

        return result;
    }

    /// <inheritdoc />
    public async Task<SchemaAnalysisResult> AnalyzeSchemasAsync(APIMetadata apiMetadata)
    {
        var result = new SchemaAnalysisResult
        {
            TotalSchemas = apiMetadata.Schemas.Count
        };

        // 타입별 분포
        result.TypeDistribution = apiMetadata.Schemas
            .GroupBy(s => s.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        // 재사용 가능한 스키마 (참조되는 스키마)
        result.ReusableSchemas = CountReusableSchemas(apiMetadata);

        // 검증 규칙이 있는 스키마
        result.ValidatedSchemas = CountValidatedSchemas(apiMetadata.Schemas);

        // 순환 참조 감지
        result.CircularReferences = DetectCircularReferences(apiMetadata.Schemas);

        // 스키마 품질 점수
        result.QualityScore = CalculateSchemaQuality(apiMetadata.Schemas);

        return result;
    }

    /// <inheritdoc />
    public async Task<APIQualityResult> EvaluateAPIQualityAsync(APIMetadata apiMetadata)
    {
        var result = new APIQualityResult();

        // 문서화 품질 평가
        result.DocumentationScore = EvaluateDocumentationQuality(apiMetadata);

        // 일관성 평가
        result.ConsistencyScore = EvaluateConsistency(apiMetadata);

        // 완성도 평가
        result.CompletenessScore = EvaluateCompleteness(apiMetadata);

        // 보안 평가
        result.SecurityScore = EvaluateSecurity(apiMetadata);

        // 전체 점수 계산
        result.OverallScore = (result.DocumentationScore + result.ConsistencyScore +
                              result.CompletenessScore + result.SecurityScore) / 4.0;

        // 품질 문제 감지
        result.Issues = DetectQualityIssues(apiMetadata);

        // 개선 권장사항 생성
        result.Recommendations = GenerateImprovementRecommendations(apiMetadata, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<APIUsageExamplesResult> GenerateUsageExamplesAsync(APIMetadata apiMetadata)
    {
        var result = new APIUsageExamplesResult();

        // 주요 엔드포인트 선택 (GET, POST 우선)
        var exampleEndpoints = apiMetadata.Endpoints
            .Where(e => e.Method.ToUpper() is "GET" or "POST")
            .Take(5)
            .ToList();

        foreach (var endpoint in exampleEndpoints)
        {
            // cURL 예제 생성
            result.CurlExamples.Add(GenerateCurlExample(endpoint, apiMetadata.BaseUrl));

            // JavaScript 예제 생성
            result.JavaScriptExamples.Add(GenerateJavaScriptExample(endpoint, apiMetadata.BaseUrl));

            // Python 예제 생성
            result.PythonExamples.Add(GeneratePythonExample(endpoint, apiMetadata.BaseUrl));

            // C# 예제 생성
            result.CSharpExamples.Add(GenerateCSharpExample(endpoint, apiMetadata.BaseUrl));
        }

        // Postman 컬렉션 생성
        result.PostmanCollection = GeneratePostmanCollection(apiMetadata);

        // SDK 생성 가능 여부 판단
        result.SDKGenerationPossible = apiMetadata.DocumentationType is
            APIDocumentationType.OpenAPI30 or APIDocumentationType.OpenAPI31 or APIDocumentationType.Swagger20;

        return result;
    }

    #region Private Helper Methods

    private async Task<APIDocumentInfo?> TryDiscoverAPIDocumentAsync(string baseUrl, string path, APIDocumentationType docType, CancellationToken cancellationToken)
    {
        try
        {
            var url = new Uri(new Uri(baseUrl), path).ToString();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

                return new APIDocumentInfo
                {
                    Url = url,
                    DocumentationType = docType,
                    FileSize = response.Content.Headers.ContentLength ?? 0,
                    DiscoveryMethod = "Direct URL check",
                    ContentType = contentType,
                    Version = DetectAPIVersion(await response.Content.ReadAsStringAsync(cancellationToken))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("API 문서 발견 실패: {Path} - {Error}", path, ex.Message);
        }

        return null;
    }

    private async Task DiscoverAPIDocsFromHtmlAsync(string baseUrl, List<APIDocumentInfo> discoveredDocs, CancellationToken cancellationToken)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(baseUrl, cancellationToken);

            // 일반적인 API 문서 링크 패턴
            var linkPatterns = new[]
            {
                @"href=[""']([^""']*(?:swagger|openapi|api-docs|graphql)[^""']*)[""']",
                @"href=[""']([^""']*\.(?:json|yaml|yml))[""'].*(?:api|swagger|openapi)",
                @"<link[^>]*rel=[""'](?:api|openapi|swagger)[""'][^>]*href=[""']([^""']*)[""']"
            };

            foreach (var pattern in linkPatterns)
            {
                var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var linkUrl = match.Groups[1].Value;
                    if (Uri.TryCreate(new Uri(baseUrl), linkUrl, out var absoluteUri))
                    {
                        var docType = DetectDocumentationType(linkUrl);
                        if (docType != APIDocumentationType.Unknown &&
                            !discoveredDocs.Any(d => d.Url == absoluteUri.ToString()))
                        {
                            discoveredDocs.Add(new APIDocumentInfo
                            {
                                Url = absoluteUri.ToString(),
                                DocumentationType = docType,
                                DiscoveryMethod = "HTML link extraction"
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("HTML에서 API 문서 링크 추출 실패: {Error}", ex.Message);
        }
    }

    private APIDocumentInfo? SelectPrimaryAPIDocument(List<APIDocumentInfo> discoveredDocs)
    {
        // 우선순위: OpenAPI 3.x > Swagger 2.0 > GraphQL > 기타
        var priorities = new Dictionary<APIDocumentationType, int>
        {
            [APIDocumentationType.OpenAPI31] = 100,
            [APIDocumentationType.OpenAPI30] = 95,
            [APIDocumentationType.Swagger20] = 80,
            [APIDocumentationType.GraphQL] = 70,
            [APIDocumentationType.AsyncAPI] = 60,
            [APIDocumentationType.RAML] = 50,
            [APIDocumentationType.Postman] = 40
        };

        return discoveredDocs
            .OrderByDescending(d => priorities.GetValueOrDefault(d.DocumentationType, 0))
            .ThenByDescending(d => d.FileSize)
            .FirstOrDefault();
    }

    private APIDocumentationType DetectDocumentationType(string url)
    {
        var urlLower = url.ToLower();

        if (urlLower.Contains("openapi")) return APIDocumentationType.OpenAPI30;
        if (urlLower.Contains("swagger")) return APIDocumentationType.Swagger20;
        if (urlLower.Contains("graphql")) return APIDocumentationType.GraphQL;
        if (urlLower.Contains("postman")) return APIDocumentationType.Postman;
        if (urlLower.Contains("raml")) return APIDocumentationType.RAML;
        if (urlLower.Contains("asyncapi")) return APIDocumentationType.AsyncAPI;
        if (urlLower.Contains("wsdl")) return APIDocumentationType.WSDL;

        return APIDocumentationType.Unknown;
    }

    private string DetectAPIVersion(string content)
    {
        // JSON에서 버전 정보 추출 시도
        try
        {
            if (content.TrimStart().StartsWith("{"))
            {
                var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                // OpenAPI 버전
                if (root.TryGetProperty("openapi", out var openapi))
                {
                    return openapi.GetString() ?? "";
                }

                // Swagger 버전
                if (root.TryGetProperty("swagger", out var swagger))
                {
                    return swagger.GetString() ?? "";
                }

                // info.version
                if (root.TryGetProperty("info", out var info) && info.TryGetProperty("version", out var version))
                {
                    return version.GetString() ?? "";
                }
            }
        }
        catch
        {
            // 파싱 실패 시 무시
        }

        return "";
    }

    private APIMetadata ParseOpenAPIDocument(string content, string url)
    {
        try
        {
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            var metadata = new APIMetadata
            {
                DocumentationType = root.TryGetProperty("openapi", out var openapi) &&
                                   openapi.GetString()?.StartsWith("3.1") == true
                    ? APIDocumentationType.OpenAPI31
                    : APIDocumentationType.OpenAPI30,
                RawContent = content
            };

            // 기본 정보 추출
            if (root.TryGetProperty("info", out var info))
            {
                metadata.Title = info.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";
                metadata.Description = info.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";
                metadata.Version = info.TryGetProperty("version", out var version) ? version.GetString() ?? "" : "";
            }

            // 서버 정보 추출
            if (root.TryGetProperty("servers", out var servers) && servers.ValueKind == JsonValueKind.Array)
            {
                foreach (var server in servers.EnumerateArray())
                {
                    metadata.Servers.Add(new ServerInfo
                    {
                        Url = server.TryGetProperty("url", out var serverUrl) ? serverUrl.GetString() ?? "" : "",
                        Description = server.TryGetProperty("description", out var serverDesc) ? serverDesc.GetString() ?? "" : ""
                    });
                }

                metadata.BaseUrl = metadata.Servers.FirstOrDefault()?.Url ?? "";
            }

            // 경로 및 엔드포인트 추출
            if (root.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        if (IsHttpMethod(method.Name))
                        {
                            var endpoint = ParseOpenAPIEndpoint(path.Name, method.Name, method.Value);
                            metadata.Endpoints.Add(endpoint);
                        }
                    }
                }
            }

            // 스키마 추출
            if (root.TryGetProperty("components", out var components) &&
                components.TryGetProperty("schemas", out var schemas))
            {
                foreach (var schema in schemas.EnumerateObject())
                {
                    var schemaDefinition = ParseOpenAPISchema(schema.Value);
                    schemaDefinition.Title = schema.Name;
                    metadata.Schemas.Add(schemaDefinition);
                }
            }

            // 태그 추출
            if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tags.EnumerateArray())
                {
                    metadata.Tags.Add(new APITag
                    {
                        Name = tag.TryGetProperty("name", out var tagName) ? tagName.GetString() ?? "" : "",
                        Description = tag.TryGetProperty("description", out var tagDesc) ? tagDesc.GetString() ?? "" : ""
                    });
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAPI 문서 파싱 실패: {Url}", url);
            return new APIMetadata { DocumentationType = APIDocumentationType.OpenAPI30, RawContent = content };
        }
    }

    private APIMetadata ParseSwaggerDocument(string content, string url)
    {
        // Swagger 2.0 파싱 로직 (OpenAPI와 유사하지만 구조가 다름)
        try
        {
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            var metadata = new APIMetadata
            {
                DocumentationType = APIDocumentationType.Swagger20,
                RawContent = content
            };

            // 기본 정보
            if (root.TryGetProperty("info", out var info))
            {
                metadata.Title = info.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";
                metadata.Description = info.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";
                metadata.Version = info.TryGetProperty("version", out var version) ? version.GetString() ?? "" : "";
            }

            // 호스트 및 베이스 경로
            var host = root.TryGetProperty("host", out var hostProp) ? hostProp.GetString() ?? "" : "";
            var basePath = root.TryGetProperty("basePath", out var basePathProp) ? basePathProp.GetString() ?? "" : "";
            var schemes = root.TryGetProperty("schemes", out var schemesProp) && schemesProp.ValueKind == JsonValueKind.Array
                ? schemesProp.EnumerateArray().FirstOrDefault().GetString() ?? "https"
                : "https";

            metadata.BaseUrl = $"{schemes}://{host}{basePath}";

            // 경로 파싱 (OpenAPI와 유사)
            if (root.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        if (IsHttpMethod(method.Name))
                        {
                            var endpoint = ParseSwaggerEndpoint(path.Name, method.Name, method.Value);
                            metadata.Endpoints.Add(endpoint);
                        }
                    }
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Swagger 문서 파싱 실패: {Url}", url);
            return new APIMetadata { DocumentationType = APIDocumentationType.Swagger20, RawContent = content };
        }
    }

    private APIMetadata ParseGraphQLSchema(string content, string url)
    {
        var metadata = new APIMetadata
        {
            DocumentationType = APIDocumentationType.GraphQL,
            Title = "GraphQL API",
            RawContent = content
        };

        // GraphQL 스키마 파싱 로직 (간소화)
        // 실제 구현에서는 GraphQL 스키마 파서 라이브러리 사용 권장

        return metadata;
    }

    private APIMetadata ParsePostmanCollection(string content, string url)
    {
        // Postman Collection 파싱
        var metadata = new APIMetadata
        {
            DocumentationType = APIDocumentationType.Postman,
            RawContent = content
        };

        try
        {
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            if (root.TryGetProperty("info", out var info))
            {
                metadata.Title = info.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
                metadata.Description = info.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";
            }

            // Postman 아이템에서 엔드포인트 추출
            if (root.TryGetProperty("item", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                ParsePostmanItems(items, metadata.Endpoints);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Postman 컬렉션 파싱 실패: {Url}", url);
        }

        return metadata;
    }

    private APIMetadata ParseRAMLDocument(string content, string url)
    {
        // RAML 파싱 (YAML 형식)
        var metadata = new APIMetadata
        {
            DocumentationType = APIDocumentationType.RAML,
            RawContent = content
        };

        // RAML 파싱 로직 구현 필요

        return metadata;
    }

    private APIMetadata ParseAsyncAPIDocument(string content, string url)
    {
        // AsyncAPI 파싱
        var metadata = new APIMetadata
        {
            DocumentationType = APIDocumentationType.AsyncAPI,
            RawContent = content
        };

        // AsyncAPI 파싱 로직 구현 필요

        return metadata;
    }

    private APIMetadata ParseWSDLDocument(string content, string url)
    {
        // WSDL 파싱 (XML 형식)
        var metadata = new APIMetadata
        {
            DocumentationType = APIDocumentationType.WSDL,
            RawContent = content
        };

        try
        {
            var xml = XDocument.Parse(content);
            // WSDL XML 파싱 로직
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WSDL 문서 파싱 실패: {Url}", url);
        }

        return metadata;
    }

    private bool IsHttpMethod(string methodName)
    {
        var httpMethods = new[] { "get", "post", "put", "delete", "patch", "options", "head", "trace" };
        return httpMethods.Contains(methodName.ToLower());
    }

    private EndpointInfo ParseOpenAPIEndpoint(string path, string method, JsonElement methodElement)
    {
        var endpoint = new EndpointInfo
        {
            Path = path,
            Method = method.ToUpper(),
            Summary = methodElement.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "" : "",
            Description = methodElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            Deprecated = methodElement.TryGetProperty("deprecated", out var deprecated) && deprecated.GetBoolean()
        };

        // 태그 추출
        if (methodElement.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            endpoint.Tags = tags.EnumerateArray()
                .Where(t => t.ValueKind == JsonValueKind.String)
                .Select(t => t.GetString()!)
                .ToList();
        }

        // 매개변수 추출
        if (methodElement.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Array)
        {
            foreach (var param in parameters.EnumerateArray())
            {
                endpoint.Parameters.Add(ParseOpenAPIParameter(param));
            }
        }

        // 요청 본문 추출
        if (methodElement.TryGetProperty("requestBody", out var requestBody))
        {
            endpoint.RequestBody = ParseOpenAPIRequestBody(requestBody);
        }

        // 응답 추출
        if (methodElement.TryGetProperty("responses", out var responses))
        {
            foreach (var response in responses.EnumerateObject())
            {
                endpoint.Responses[response.Name] = ParseOpenAPIResponse(response.Value);
            }
        }

        return endpoint;
    }

    private EndpointInfo ParseSwaggerEndpoint(string path, string method, JsonElement methodElement)
    {
        // Swagger 2.0 엔드포인트 파싱 (OpenAPI와 유사하지만 구조가 조금 다름)
        return ParseOpenAPIEndpoint(path, method, methodElement);
    }

    private ParameterInfo ParseOpenAPIParameter(JsonElement paramElement)
    {
        return new ParameterInfo
        {
            Name = paramElement.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            In = paramElement.TryGetProperty("in", out var inProp) ? inProp.GetString() ?? "" : "",
            Description = paramElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            Required = paramElement.TryGetProperty("required", out var required) && required.GetBoolean(),
            Schema = paramElement.TryGetProperty("schema", out var schema) ? ParseOpenAPISchema(schema) : null
        };
    }

    private RequestBodyInfo ParseOpenAPIRequestBody(JsonElement requestBodyElement)
    {
        var requestBody = new RequestBodyInfo
        {
            Description = requestBodyElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            Required = requestBodyElement.TryGetProperty("required", out var required) && required.GetBoolean()
        };

        if (requestBodyElement.TryGetProperty("content", out var content))
        {
            foreach (var mediaType in content.EnumerateObject())
            {
                requestBody.Content[mediaType.Name] = new MediaTypeInfo
                {
                    Schema = mediaType.Value.TryGetProperty("schema", out var schema) ? ParseOpenAPISchema(schema) : null
                };
            }
        }

        return requestBody;
    }

    private ResponseInfo ParseOpenAPIResponse(JsonElement responseElement)
    {
        var response = new ResponseInfo
        {
            Description = responseElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : ""
        };

        if (responseElement.TryGetProperty("content", out var content))
        {
            foreach (var mediaType in content.EnumerateObject())
            {
                response.Content[mediaType.Name] = new MediaTypeInfo
                {
                    Schema = mediaType.Value.TryGetProperty("schema", out var schema) ? ParseOpenAPISchema(schema) : null
                };
            }
        }

        return response;
    }

    private SchemaDefinition ParseOpenAPISchema(JsonElement schemaElement)
    {
        var schema = new SchemaDefinition
        {
            Type = schemaElement.TryGetProperty("type", out var type) ? type.GetString() ?? "" : "",
            Format = schemaElement.TryGetProperty("format", out var format) ? format.GetString() ?? "" : "",
            Title = schemaElement.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            Description = schemaElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            Ref = schemaElement.TryGetProperty("$ref", out var refProp) ? refProp.GetString() : null
        };

        // 속성들
        if (schemaElement.TryGetProperty("properties", out var properties))
        {
            foreach (var prop in properties.EnumerateObject())
            {
                schema.Properties[prop.Name] = ParseOpenAPISchema(prop.Value);
            }
        }

        // 필수 속성들
        if (schemaElement.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            schema.Required = required.EnumerateArray()
                .Where(r => r.ValueKind == JsonValueKind.String)
                .Select(r => r.GetString()!)
                .ToList();
        }

        // 배열 항목
        if (schemaElement.TryGetProperty("items", out var items))
        {
            schema.Items = ParseOpenAPISchema(items);
        }

        return schema;
    }

    private void ParsePostmanItems(JsonElement items, List<EndpointInfo> endpoints)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("request", out var request))
            {
                var endpoint = new EndpointInfo();

                if (request.TryGetProperty("method", out var method))
                {
                    endpoint.Method = method.GetString() ?? "";
                }

                if (request.TryGetProperty("url", out var url))
                {
                    if (url.ValueKind == JsonValueKind.String)
                    {
                        endpoint.Path = url.GetString() ?? "";
                    }
                    else if (url.TryGetProperty("raw", out var rawUrl))
                    {
                        endpoint.Path = rawUrl.GetString() ?? "";
                    }
                }

                if (item.TryGetProperty("name", out var name))
                {
                    endpoint.Summary = name.GetString() ?? "";
                }

                endpoints.Add(endpoint);
            }
            else if (item.TryGetProperty("item", out var nestedItems))
            {
                // 중첩된 아이템들 재귀 처리
                ParsePostmanItems(nestedItems, endpoints);
            }
        }
    }

    private double CalculateEndpointComplexity(List<EndpointInfo> endpoints)
    {
        if (!endpoints.Any()) return 0.0;

        var totalComplexity = endpoints.Sum(e =>
        {
            var complexity = 1.0; // 기본 복잡도
            complexity += e.Parameters.Count * 0.1; // 매개변수 복잡도
            complexity += e.Responses.Count * 0.05; // 응답 복잡도
            if (e.RequestBody != null) complexity += 0.2; // 요청 본문 복잡도
            if (e.Security.Any()) complexity += 0.1; // 보안 복잡도
            return complexity;
        });

        return Math.Min(totalComplexity / endpoints.Count / 2.0, 1.0); // 정규화
    }

    private double CalculateEndpointCoverage(List<EndpointInfo> endpoints)
    {
        if (!endpoints.Any()) return 0.0;

        var score = 0.0;
        var total = endpoints.Count;

        // 설명이 있는 엔드포인트
        score += endpoints.Count(e => !string.IsNullOrEmpty(e.Description)) / (double)total * 0.3;

        // 예제가 있는 엔드포인트
        score += endpoints.Count(e => e.Parameters.Any(p => p.Example != null)) / (double)total * 0.2;

        // 응답 스키마가 정의된 엔드포인트
        score += endpoints.Count(e => e.Responses.Any(r => r.Value.Content.Any())) / (double)total * 0.3;

        // 태그가 있는 엔드포인트
        score += endpoints.Count(e => e.Tags.Any()) / (double)total * 0.2;

        return Math.Min(score, 1.0);
    }

    private int CountReusableSchemas(APIMetadata apiMetadata)
    {
        var referencedSchemas = new HashSet<string>();

        // 엔드포인트에서 참조되는 스키마 찾기
        foreach (var endpoint in apiMetadata.Endpoints)
        {
            CountSchemaReferences(endpoint, referencedSchemas);
        }

        return referencedSchemas.Count;
    }

    private void CountSchemaReferences(EndpointInfo endpoint, HashSet<string> referencedSchemas)
    {
        // 매개변수 스키마 참조
        foreach (var param in endpoint.Parameters)
        {
            if (param.Schema?.Ref != null)
            {
                referencedSchemas.Add(param.Schema.Ref);
            }
        }

        // 요청 본문 스키마 참조
        if (endpoint.RequestBody != null)
        {
            foreach (var content in endpoint.RequestBody.Content.Values)
            {
                if (content.Schema?.Ref != null)
                {
                    referencedSchemas.Add(content.Schema.Ref);
                }
            }
        }

        // 응답 스키마 참조
        foreach (var response in endpoint.Responses.Values)
        {
            foreach (var content in response.Content.Values)
            {
                if (content.Schema?.Ref != null)
                {
                    referencedSchemas.Add(content.Schema.Ref);
                }
            }
        }
    }

    private int CountValidatedSchemas(List<SchemaDefinition> schemas)
    {
        return schemas.Count(s =>
            s.Minimum.HasValue || s.Maximum.HasValue ||
            s.MinLength.HasValue || s.MaxLength.HasValue ||
            !string.IsNullOrEmpty(s.Pattern) ||
            s.Enum.Any());
    }

    private int DetectCircularReferences(List<SchemaDefinition> schemas)
    {
        // 순환 참조 감지 로직 (간소화)
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var circularCount = 0;

        foreach (var schema in schemas)
        {
            if (!string.IsNullOrEmpty(schema.Title) && !visited.Contains(schema.Title))
            {
                if (HasCircularReference(schema, schemas, visited, recursionStack))
                {
                    circularCount++;
                }
            }
        }

        return circularCount;
    }

    private bool HasCircularReference(SchemaDefinition schema, List<SchemaDefinition> allSchemas,
        HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (string.IsNullOrEmpty(schema.Title)) return false;

        visited.Add(schema.Title);
        recursionStack.Add(schema.Title);

        // 속성에서 참조 확인
        foreach (var property in schema.Properties.Values)
        {
            if (!string.IsNullOrEmpty(property.Ref))
            {
                var refSchemaName = property.Ref.Split('/').LastOrDefault();
                if (recursionStack.Contains(refSchemaName))
                {
                    return true;
                }

                var refSchema = allSchemas.FirstOrDefault(s => s.Title == refSchemaName);
                if (refSchema != null && HasCircularReference(refSchema, allSchemas, visited, recursionStack))
                {
                    return true;
                }
            }
        }

        recursionStack.Remove(schema.Title);
        return false;
    }

    private double CalculateSchemaQuality(List<SchemaDefinition> schemas)
    {
        if (!schemas.Any()) return 0.0;

        var score = 0.0;
        var total = schemas.Count;

        // 설명이 있는 스키마
        score += schemas.Count(s => !string.IsNullOrEmpty(s.Description)) / (double)total * 0.3;

        // 검증 규칙이 있는 스키마
        score += CountValidatedSchemas(schemas) / (double)total * 0.3;

        // 예제가 있는 스키마
        score += schemas.Count(s => s.Example != null) / (double)total * 0.2;

        // 타입이 명확한 스키마
        score += schemas.Count(s => !string.IsNullOrEmpty(s.Type)) / (double)total * 0.2;

        return Math.Min(score, 1.0);
    }

    private double EvaluateDocumentationQuality(APIMetadata apiMetadata)
    {
        var score = 0.0;

        // 기본 정보 품질
        if (!string.IsNullOrEmpty(apiMetadata.Title)) score += 0.1;
        if (!string.IsNullOrEmpty(apiMetadata.Description)) score += 0.2;
        if (!string.IsNullOrEmpty(apiMetadata.Version)) score += 0.1;

        // 엔드포인트 문서화
        var totalEndpoints = apiMetadata.Endpoints.Count;
        if (totalEndpoints > 0)
        {
            var documentedEndpoints = apiMetadata.Endpoints.Count(e => !string.IsNullOrEmpty(e.Description));
            score += (documentedEndpoints / (double)totalEndpoints) * 0.4;
        }

        // 스키마 문서화
        var totalSchemas = apiMetadata.Schemas.Count;
        if (totalSchemas > 0)
        {
            var documentedSchemas = apiMetadata.Schemas.Count(s => !string.IsNullOrEmpty(s.Description));
            score += (documentedSchemas / (double)totalSchemas) * 0.2;
        }

        return Math.Min(score, 1.0);
    }

    private double EvaluateConsistency(APIMetadata apiMetadata)
    {
        var score = 1.0;

        // 네이밍 일관성 검사
        var pathNamingConsistency = EvaluatePathNamingConsistency(apiMetadata.Endpoints);
        score *= pathNamingConsistency;

        // 응답 구조 일관성
        var responseConsistency = EvaluateResponseConsistency(apiMetadata.Endpoints);
        score *= responseConsistency;

        return score;
    }

    private double EvaluateCompleteness(APIMetadata apiMetadata)
    {
        var score = 0.0;

        // 기본 CRUD 작업 완성도
        var methods = apiMetadata.Endpoints.Select(e => e.Method.ToUpper()).Distinct().ToList();
        if (methods.Contains("GET")) score += 0.25;
        if (methods.Contains("POST")) score += 0.25;
        if (methods.Contains("PUT") || methods.Contains("PATCH")) score += 0.25;
        if (methods.Contains("DELETE")) score += 0.25;

        return score;
    }

    private double EvaluateSecurity(APIMetadata apiMetadata)
    {
        var score = 0.5; // 기본 점수

        // 인증 방법 정의 여부
        if (apiMetadata.AuthMethods.Any()) score += 0.3;

        // 보안이 적용된 엔드포인트 비율
        var securedEndpoints = apiMetadata.Endpoints.Count(e => e.Security.Any());
        if (apiMetadata.Endpoints.Any())
        {
            score += (securedEndpoints / (double)apiMetadata.Endpoints.Count) * 0.2;
        }

        return Math.Min(score, 1.0);
    }

    private double EvaluatePathNamingConsistency(List<EndpointInfo> endpoints)
    {
        // 간단한 네이밍 일관성 평가 (kebab-case vs camelCase vs snake_case)
        if (!endpoints.Any()) return 1.0;

        var kebabCaseCount = endpoints.Count(e => e.Path.Contains('-'));
        var camelCaseCount = endpoints.Count(e => Regex.IsMatch(e.Path, @"[a-z][A-Z]"));
        var snakeCaseCount = endpoints.Count(e => e.Path.Contains('_'));

        var maxCount = Math.Max(kebabCaseCount, Math.Max(camelCaseCount, snakeCaseCount));
        return maxCount / (double)endpoints.Count;
    }

    private double EvaluateResponseConsistency(List<EndpointInfo> endpoints)
    {
        // 응답 구조 일관성 평가 (간소화)
        return 0.8; // 임시 값
    }

    private List<QualityIssue> DetectQualityIssues(APIMetadata apiMetadata)
    {
        var issues = new List<QualityIssue>();

        // 문서화 누락 검사
        foreach (var endpoint in apiMetadata.Endpoints)
        {
            if (string.IsNullOrEmpty(endpoint.Description))
            {
                issues.Add(new QualityIssue
                {
                    Severity = IssueSeverity.Medium,
                    Category = "Documentation",
                    Description = "엔드포인트 설명이 누락되었습니다",
                    Location = $"{endpoint.Method} {endpoint.Path}"
                });
            }
        }

        // 사용 중단된 엔드포인트 경고
        foreach (var endpoint in apiMetadata.Endpoints.Where(e => e.Deprecated))
        {
            issues.Add(new QualityIssue
            {
                Severity = IssueSeverity.High,
                Category = "Deprecation",
                Description = "사용 중단된 엔드포인트입니다",
                Location = $"{endpoint.Method} {endpoint.Path}"
            });
        }

        return issues;
    }

    private List<ImprovementRecommendation> GenerateImprovementRecommendations(APIMetadata apiMetadata, APIQualityResult qualityResult)
    {
        var recommendations = new List<ImprovementRecommendation>();

        if (qualityResult.DocumentationScore < 0.7)
        {
            recommendations.Add(new ImprovementRecommendation
            {
                Priority = RecommendationPriority.High,
                Title = "문서화 개선",
                Description = "모든 엔드포인트와 스키마에 상세한 설명을 추가하세요",
                ExpectedImpact = "API 사용성과 개발자 경험이 크게 향상됩니다"
            });
        }

        if (qualityResult.SecurityScore < 0.6)
        {
            recommendations.Add(new ImprovementRecommendation
            {
                Priority = RecommendationPriority.Critical,
                Title = "보안 강화",
                Description = "모든 민감한 엔드포인트에 적절한 인증을 적용하세요",
                ExpectedImpact = "API 보안이 크게 향상되고 무단 접근을 방지할 수 있습니다"
            });
        }

        return recommendations;
    }

    private CodeExample GenerateCurlExample(EndpointInfo endpoint, string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}{endpoint.Path}";
        var curl = $"curl -X {endpoint.Method} \"{url}\"";

        // 헤더 추가
        if (endpoint.Method.ToUpper() is "POST" or "PUT" or "PATCH")
        {
            curl += " -H \"Content-Type: application/json\"";
        }

        // 인증 헤더 추가 (예시)
        if (endpoint.Security.Any())
        {
            curl += " -H \"Authorization: Bearer YOUR_TOKEN\"";
        }

        // 요청 본문 추가
        if (endpoint.RequestBody != null)
        {
            curl += " -d '{\"example\": \"data\"}'";
        }

        return new CodeExample
        {
            Endpoint = $"{endpoint.Method} {endpoint.Path}",
            Description = endpoint.Summary,
            Code = curl,
            Language = "bash"
        };
    }

    private CodeExample GenerateJavaScriptExample(EndpointInfo endpoint, string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}{endpoint.Path}";
        var js = $@"const response = await fetch('{url}', {{
  method: '{endpoint.Method}',
  headers: {{
    'Content-Type': 'application/json',";

        if (endpoint.Security.Any())
        {
            js += "\n    'Authorization': 'Bearer YOUR_TOKEN',";
        }

        js += "\n  },";

        if (endpoint.RequestBody != null)
        {
            js += "\n  body: JSON.stringify({ example: 'data' }),";
        }

        js += "\n});";
        js += "\nconst data = await response.json();";

        return new CodeExample
        {
            Endpoint = $"{endpoint.Method} {endpoint.Path}",
            Description = endpoint.Summary,
            Code = js,
            Language = "javascript"
        };
    }

    private CodeExample GeneratePythonExample(EndpointInfo endpoint, string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}{endpoint.Path}";
        var python = $@"import requests

url = '{url}'
headers = {{'Content-Type': 'application/json'}}";

        if (endpoint.Security.Any())
        {
            python += "\nheaders['Authorization'] = 'Bearer YOUR_TOKEN'";
        }

        if (endpoint.RequestBody != null)
        {
            python += "\ndata = {'example': 'data'}";
            python += $"\nresponse = requests.{endpoint.Method.ToLower()}(url, headers=headers, json=data)";
        }
        else
        {
            python += $"\nresponse = requests.{endpoint.Method.ToLower()}(url, headers=headers)";
        }

        python += "\nprint(response.json())";

        return new CodeExample
        {
            Endpoint = $"{endpoint.Method} {endpoint.Path}",
            Description = endpoint.Summary,
            Code = python,
            Language = "python"
        };
    }

    private CodeExample GenerateCSharpExample(EndpointInfo endpoint, string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}{endpoint.Path}";
        var csharp = $@"using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
var url = ""{url}"";";

        if (endpoint.Security.Any())
        {
            csharp += "\nclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(\"Bearer\", \"YOUR_TOKEN\");";
        }

        if (endpoint.RequestBody != null)
        {
            csharp += @"
var data = new { example = ""data"" };
var json = JsonSerializer.Serialize(data);
var content = new StringContent(json, Encoding.UTF8, ""application/json"");";
            csharp += $"\nvar response = await client.{endpoint.Method.ToTitleCase()}Async(url, content);";
        }
        else
        {
            csharp += $"\nvar response = await client.{endpoint.Method.ToTitleCase()}Async(url);";
        }

        csharp += "\nvar result = await response.Content.ReadAsStringAsync();";

        return new CodeExample
        {
            Endpoint = $"{endpoint.Method} {endpoint.Path}",
            Description = endpoint.Summary,
            Code = csharp,
            Language = "csharp"
        };
    }

    private string GeneratePostmanCollection(APIMetadata apiMetadata)
    {
        // 간단한 Postman Collection 생성
        var collection = new
        {
            info = new
            {
                name = apiMetadata.Title,
                description = apiMetadata.Description,
                schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            item = apiMetadata.Endpoints.Take(10).Select(e => new
            {
                name = e.Summary,
                request = new
                {
                    method = e.Method,
                    header = new[] { new { key = "Content-Type", value = "application/json" } },
                    url = new
                    {
                        raw = $"{apiMetadata.BaseUrl}{e.Path}",
                        host = new[] { apiMetadata.BaseUrl }
                    }
                }
            })
        };

        return JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true });
    }

    private double CalculateOverallQualityScore(APIDocumentationAnalysisResult result)
    {
        var score = 0.0;

        // 문서 발견 점수
        score += Math.Min(result.DiscoveredDocuments.Count / 3.0, 0.2);

        // API 품질 점수
        if (result.QualityEvaluation != null)
        {
            score += result.QualityEvaluation.OverallScore * 0.5;
        }

        // 엔드포인트 분석 점수
        if (result.EndpointAnalysis != null)
        {
            score += result.EndpointAnalysis.CoverageScore * 0.2;
        }

        // 스키마 분석 점수
        if (result.SchemaAnalysis != null)
        {
            score += result.SchemaAnalysis.QualityScore * 0.1;
        }

        return Math.Min(score, 1.0);
    }

    #endregion
}

/// <summary>
/// 문자열 확장 메서드
/// </summary>
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }
}