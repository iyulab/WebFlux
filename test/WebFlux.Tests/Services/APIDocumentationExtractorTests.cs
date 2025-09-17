using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using WebFlux.Configuration;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;

namespace WebFlux.Tests.Services;

/// <summary>
/// APIDocumentationExtractor 단위 테스트
/// API 문서 추출 및 분석 기능의 정확성과 안정성을 검증
/// </summary>
public class APIDocumentationExtractorTests
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<ILogger<APIDocumentationExtractor>> _mockLogger;
    private readonly Mock<IOptions<WebFluxConfiguration>> _mockOptions;
    private readonly APIDocumentationExtractor _extractor;

    public APIDocumentationExtractorTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockLogger = new Mock<ILogger<APIDocumentationExtractor>>();
        _mockOptions = new Mock<IOptions<WebFluxConfiguration>>();

        _mockOptions.Setup(x => x.Value).Returns(new WebFluxConfiguration());

        _extractor = new APIDocumentationExtractor(
            _mockHttpClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    [Fact]
    public async Task ExtractAPIMetadataAsync_WithOpenAPI3Document_ShouldParseCorrectly()
    {
        // Arrange
        var openApiContent = """
        {
            "openapi": "3.0.3",
            "info": {
                "title": "Pet Store API",
                "version": "1.0.0",
                "description": "A simple pet store API"
            },
            "servers": [
                {
                    "url": "https://api.petstore.com/v1",
                    "description": "Production server"
                }
            ],
            "paths": {
                "/pets": {
                    "get": {
                        "summary": "List all pets",
                        "operationId": "listPets",
                        "tags": ["pets"],
                        "parameters": [
                            {
                                "name": "limit",
                                "in": "query",
                                "schema": {
                                    "type": "integer",
                                    "maximum": 100
                                }
                            }
                        ],
                        "responses": {
                            "200": {
                                "description": "A paged array of pets",
                                "content": {
                                    "application/json": {
                                        "schema": {
                                            "$ref": "#/components/schemas/Pets"
                                        }
                                    }
                                }
                            }
                        }
                    },
                    "post": {
                        "summary": "Create a pet",
                        "operationId": "createPet",
                        "tags": ["pets"],
                        "requestBody": {
                            "required": true,
                            "content": {
                                "application/json": {
                                    "schema": {
                                        "$ref": "#/components/schemas/Pet"
                                    }
                                }
                            }
                        },
                        "responses": {
                            "201": {
                                "description": "Pet created successfully"
                            }
                        }
                    }
                }
            },
            "components": {
                "schemas": {
                    "Pet": {
                        "type": "object",
                        "required": ["id", "name"],
                        "properties": {
                            "id": {
                                "type": "integer",
                                "format": "int64"
                            },
                            "name": {
                                "type": "string"
                            }
                        }
                    },
                    "Pets": {
                        "type": "array",
                        "items": {
                            "$ref": "#/components/schemas/Pet"
                        }
                    }
                }
            }
        }
        """;

        SetupHttpClientMock("https://api.example.com/swagger.json", openApiContent);

        // Act
        var result = await _extractor.ExtractAPIMetadataAsync("https://api.example.com");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DocumentationFound.Count > 0);

        var apiDoc = result.DocumentationFound.First();
        Assert.Equal(APIDocumentationType.OpenAPI3, apiDoc.DocumentationType);
        Assert.Equal("Pet Store API", apiDoc.Title);
        Assert.Equal("1.0.0", apiDoc.Version);
        Assert.Equal("A simple pet store API", apiDoc.Description);
        Assert.Equal("https://api.petstore.com/v1", apiDoc.BaseUrl);

        // 엔드포인트 검증
        Assert.Equal(2, apiDoc.Endpoints.Count);
        var getPetsEndpoint = apiDoc.Endpoints.FirstOrDefault(e => e.Method == "GET" && e.Path == "/pets");
        Assert.NotNull(getPetsEndpoint);
        Assert.Equal("List all pets", getPetsEndpoint.Summary);
        Assert.Single(getPetsEndpoint.Parameters);
        Assert.Equal("limit", getPetsEndpoint.Parameters.First().Name);
    }

    [Fact]
    public async Task ExtractAPIMetadataAsync_WithSwagger2Document_ShouldParseCorrectly()
    {
        // Arrange
        var swagger2Content = """
        {
            "swagger": "2.0",
            "info": {
                "title": "User API",
                "version": "2.0.0",
                "description": "User management API"
            },
            "host": "api.users.com",
            "basePath": "/v2",
            "schemes": ["https"],
            "paths": {
                "/users": {
                    "get": {
                        "summary": "Get users",
                        "description": "Retrieve a list of users",
                        "parameters": [
                            {
                                "name": "page",
                                "in": "query",
                                "type": "integer",
                                "required": false
                            }
                        ],
                        "responses": {
                            "200": {
                                "description": "Success",
                                "schema": {
                                    "type": "array",
                                    "items": {
                                        "$ref": "#/definitions/User"
                                    }
                                }
                            }
                        }
                    }
                }
            },
            "definitions": {
                "User": {
                    "type": "object",
                    "properties": {
                        "id": {
                            "type": "integer"
                        },
                        "username": {
                            "type": "string"
                        }
                    }
                }
            }
        }
        """;

        SetupHttpClientMock("https://api.example.com/swagger.json", swagger2Content);

        // Act
        var result = await _extractor.ExtractAPIMetadataAsync("https://api.example.com");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DocumentationFound.Count > 0);

        var apiDoc = result.DocumentationFound.First();
        Assert.Equal(APIDocumentationType.Swagger2, apiDoc.DocumentationType);
        Assert.Equal("User API", apiDoc.Title);
        Assert.Equal("2.0.0", apiDoc.Version);
        Assert.Equal("https://api.users.com/v2", apiDoc.BaseUrl);
    }

    [Fact]
    public async Task ExtractAPIMetadataAsync_WithGraphQLSchema_ShouldParseCorrectly()
    {
        // Arrange
        var graphqlSchema = """
        type Query {
            user(id: ID!): User
            users(first: Int, after: String): UserConnection
        }

        type Mutation {
            createUser(input: CreateUserInput!): User
            updateUser(id: ID!, input: UpdateUserInput!): User
        }

        type User {
            id: ID!
            username: String!
            email: String!
            createdAt: DateTime!
        }

        type UserConnection {
            edges: [UserEdge!]!
            pageInfo: PageInfo!
        }

        type UserEdge {
            node: User!
            cursor: String!
        }

        input CreateUserInput {
            username: String!
            email: String!
        }

        input UpdateUserInput {
            username: String
            email: String
        }

        scalar DateTime
        """;

        SetupHttpClientMock("https://api.example.com/graphql/schema", graphqlSchema);

        // Act
        var result = await _extractor.ExtractAPIMetadataAsync("https://api.example.com");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DocumentationFound.Count > 0);

        var apiDoc = result.DocumentationFound.First();
        Assert.Equal(APIDocumentationType.GraphQL, apiDoc.DocumentationType);
        Assert.Contains(apiDoc.Endpoints, e => e.Method == "QUERY" && e.Path.Contains("user"));
        Assert.Contains(apiDoc.Endpoints, e => e.Method == "MUTATION" && e.Path.Contains("createUser"));
    }

    [Fact]
    public async Task AnalyzeEndpointsAsync_WithRESTAPI_ShouldCategorizeEndpoints()
    {
        // Arrange
        var apiMetadata = new APIMetadata
        {
            Title = "E-commerce API",
            DocumentationType = APIDocumentationType.OpenAPI3,
            Endpoints = new List<EndpointInfo>
            {
                new() { Method = "GET", Path = "/products", Summary = "List products" },
                new() { Method = "GET", Path = "/products/{id}", Summary = "Get product by ID" },
                new() { Method = "POST", Path = "/products", Summary = "Create product" },
                new() { Method = "PUT", Path = "/products/{id}", Summary = "Update product" },
                new() { Method = "DELETE", Path = "/products/{id}", Summary = "Delete product" },
                new() { Method = "GET", Path = "/users/{id}/orders", Summary = "Get user orders" },
                new() { Method = "POST", Path = "/users/{id}/orders", Summary = "Create order" }
            }
        };

        // Act
        var result = await _extractor.AnalyzeEndpointsAsync(apiMetadata);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EndpointCategories.ContainsKey("Products"));
        Assert.True(result.EndpointCategories.ContainsKey("Users"));

        var productsEndpoints = result.EndpointCategories["Products"];
        Assert.Equal(5, productsEndpoints.Count);

        // CRUD 패턴 검증
        Assert.Contains("CRUD Operations", result.APIPatterns);
        Assert.True(result.RESTCompliance > 0.8);
    }

    [Fact]
    public async Task GenerateUsageExamplesAsync_ForCURLFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var endpoint = new EndpointInfo
        {
            Method = "POST",
            Path = "/users",
            Summary = "Create a new user",
            RequestBodySchema = new SchemaDefinition
            {
                Type = "object",
                Properties = new Dictionary<string, SchemaDefinition>
                {
                    ["username"] = new() { Type = "string" },
                    ["email"] = new() { Type = "string" }
                }
            }
        };

        var apiMetadata = new APIMetadata
        {
            BaseUrl = "https://api.example.com",
            Endpoints = new List<EndpointInfo> { endpoint }
        };

        // Act
        var result = await _extractor.GenerateUsageExamplesAsync(apiMetadata, "curl");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CodeExamples.ContainsKey("curl"));

        var curlExample = result.CodeExamples["curl"];
        Assert.Contains("curl -X POST", curlExample);
        Assert.Contains("https://api.example.com/users", curlExample);
        Assert.Contains("Content-Type: application/json", curlExample);
        Assert.Contains("username", curlExample);
        Assert.Contains("email", curlExample);
    }

    [Fact]
    public async Task GenerateUsageExamplesAsync_ForJavaScriptFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var endpoint = new EndpointInfo
        {
            Method = "GET",
            Path = "/products",
            Summary = "Get products",
            Parameters = new List<ParameterInfo>
            {
                new() { Name = "category", In = "query", Type = "string" },
                new() { Name = "limit", In = "query", Type = "integer" }
            }
        };

        var apiMetadata = new APIMetadata
        {
            BaseUrl = "https://api.store.com",
            Endpoints = new List<EndpointInfo> { endpoint }
        };

        // Act
        var result = await _extractor.GenerateUsageExamplesAsync(apiMetadata, "javascript");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CodeExamples.ContainsKey("javascript"));

        var jsExample = result.CodeExamples["javascript"];
        Assert.Contains("fetch(", jsExample);
        Assert.Contains("https://api.store.com/products", jsExample);
        Assert.Contains("category=", jsExample);
        Assert.Contains("limit=", jsExample);
    }

    [Fact]
    public async Task ExtractAPIMetadataAsync_WithNoAPIDocumentation_ShouldReturnEmptyResult()
    {
        // Arrange
        SetupHttpClientMock("https://example.com/swagger.json", null, HttpStatusCode.NotFound);
        SetupHttpClientMock("https://example.com/api-docs", null, HttpStatusCode.NotFound);
        SetupHttpClientMock("https://example.com/graphql", null, HttpStatusCode.NotFound);

        // Act
        var result = await _extractor.ExtractAPIMetadataAsync("https://example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.DocumentationFound);
        Assert.False(result.ExtractionSuccessful);
    }

    [Fact]
    public async Task ExtractAPIMetadataAsync_WithInvalidJSON_ShouldHandleGracefully()
    {
        // Arrange
        var invalidJson = "{ invalid json content";
        SetupHttpClientMock("https://api.example.com/swagger.json", invalidJson);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            _extractor.ExtractAPIMetadataAsync("https://api.example.com"));

        // 예외가 발생하지 않고 우아하게 처리되어야 함
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("swagger.json", APIDocumentationType.OpenAPI3)]
    [InlineData("api-docs", APIDocumentationType.Swagger2)]
    [InlineData("openapi.json", APIDocumentationType.OpenAPI3)]
    [InlineData("graphql", APIDocumentationType.GraphQL)]
    [InlineData("schema.graphql", APIDocumentationType.GraphQL)]
    public void DetectDocumentationType_ForDifferentURLs_ShouldReturnCorrectType(
        string urlPath, APIDocumentationType expectedType)
    {
        // Act
        var detectedType = APIDocumentationExtractor.DetectDocumentationType($"https://api.example.com/{urlPath}");

        // Assert
        Assert.Equal(expectedType, detectedType);
    }

    [Fact]
    public async Task AnalyzeEndpointsAsync_WithMicroservicesAPI_ShouldDetectPatterns()
    {
        // Arrange
        var apiMetadata = new APIMetadata
        {
            Title = "Microservices Gateway",
            Endpoints = new List<EndpointInfo>
            {
                new() { Method = "GET", Path = "/user-service/users", Summary = "Users from user service" },
                new() { Method = "GET", Path = "/order-service/orders", Summary = "Orders from order service" },
                new() { Method = "GET", Path = "/product-service/products", Summary = "Products from product service" },
                new() { Method = "GET", Path = "/health", Summary = "Health check" },
                new() { Method = "GET", Path = "/metrics", Summary = "Metrics endpoint" }
            }
        };

        // Act
        var result = await _extractor.AnalyzeEndpointsAsync(apiMetadata);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Microservices Gateway", result.APIPatterns);
        Assert.Contains("Health Monitoring", result.APIPatterns);
        Assert.True(result.EndpointCategories.ContainsKey("User Service"));
        Assert.True(result.EndpointCategories.ContainsKey("Order Service"));
        Assert.True(result.EndpointCategories.ContainsKey("Product Service"));
    }

    private void SetupHttpClientMock(string url, string? content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        if (content == null)
        {
            _mockHttpClient
                .Setup(x => x.GetStringAsync(url, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException($"Response status code does not indicate success: {(int)statusCode}"));
        }
        else
        {
            _mockHttpClient
                .Setup(x => x.GetStringAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(content);
        }
    }
}