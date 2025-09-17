using System.Text.Json;
using System.Text.RegularExpressions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using Microsoft.Extensions.Logging;

namespace WebFlux.Services;

/// <summary>
/// 패키지 생태계 메타데이터 분석기
/// 다양한 패키지 관리 파일을 분석하여 프로젝트 구조와 기술 스택을 파악
/// </summary>
public class PackageEcosystemAnalyzer : IPackageEcosystemAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PackageEcosystemAnalyzer> _logger;

    /// <summary>
    /// 일반적인 패키지 파일 경로들
    /// </summary>
    private static readonly Dictionary<PackageEcosystemType, List<string>> PackageFilePaths = new()
    {
        [PackageEcosystemType.NodeJs] = new() { "package.json", "yarn.lock", "package-lock.json" },
        [PackageEcosystemType.Python] = new() { "requirements.txt", "setup.py", "pyproject.toml", "Pipfile", "poetry.lock" },
        [PackageEcosystemType.CSharp] = new() { "*.csproj", "packages.config", "Directory.Build.props", "nuget.config" },
        [PackageEcosystemType.Java] = new() { "pom.xml", "build.gradle", "gradle.properties", "build.xml" },
        [PackageEcosystemType.Php] = new() { "composer.json", "composer.lock" },
        [PackageEcosystemType.Ruby] = new() { "Gemfile", "Gemfile.lock", "*.gemspec" },
        [PackageEcosystemType.Go] = new() { "go.mod", "go.sum" },
        [PackageEcosystemType.Rust] = new() { "Cargo.toml", "Cargo.lock" },
        [PackageEcosystemType.Swift] = new() { "Package.swift", "Package.resolved" },
        [PackageEcosystemType.Dart] = new() { "pubspec.yaml", "pubspec.lock" }
    };

    public PackageEcosystemAnalyzer(HttpClient httpClient, ILogger<PackageEcosystemAnalyzer> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PackageEcosystemAnalysisResult> AnalyzeFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("패키지 생태계 분석 시작: {BaseUrl}", baseUrl);

        var result = new PackageEcosystemAnalysisResult();
        var discoveredFiles = new List<PackageFileInfo>();

        // 1. 패키지 파일 발견
        foreach (var (ecosystemType, filePaths) in PackageFilePaths)
        {
            foreach (var filePath in filePaths)
            {
                var packageFileInfo = await TryDiscoverPackageFileAsync(baseUrl, filePath, ecosystemType, cancellationToken);
                if (packageFileInfo != null)
                {
                    discoveredFiles.Add(packageFileInfo);
                }
            }
        }

        result.DiscoveredPackageFiles = discoveredFiles;

        if (discoveredFiles.Any())
        {
            // 2. 주요 패키지 파일 분석 (우선순위: 메인 패키지 파일)
            var primaryPackageFile = SelectPrimaryPackageFile(discoveredFiles);
            if (primaryPackageFile != null)
            {
                var packageMetadata = await AnalyzePackageFileAsync(primaryPackageFile.Url, primaryPackageFile.EcosystemType, cancellationToken);

                // 3. 기술 스택 분석
                result.PrimaryTechStack = await AnalyzeTechStackAsync(packageMetadata);

                // 4. 복잡도 분석
                result.ComplexityAnalysis = await EvaluateProjectComplexityAsync(packageMetadata);

                // 5. 보안 분석
                result.SecurityAnalysis = await AnalyzeSecurityRisksAsync(packageMetadata);
            }

            // 6. 품질 점수 계산
            result.QualityScore = CalculateQualityScore(result);
        }

        _logger.LogInformation("패키지 생태계 분석 완료: {FileCount}개 파일 발견, 품질 점수: {QualityScore:F2}",
            discoveredFiles.Count, result.QualityScore);

        return result;
    }

    /// <inheritdoc />
    public async Task<PackageMetadata> AnalyzePackageFileAsync(string packageFileUrl, PackageEcosystemType packageType, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _httpClient.GetStringAsync(packageFileUrl, cancellationToken);

            return packageType switch
            {
                PackageEcosystemType.NodeJs => ParseNodeJsPackage(content, packageFileUrl),
                PackageEcosystemType.Python => ParsePythonPackage(content, packageFileUrl),
                PackageEcosystemType.Php => ParsePhpPackage(content, packageFileUrl),
                PackageEcosystemType.CSharp => ParseCSharpPackage(content, packageFileUrl),
                PackageEcosystemType.Java => ParseJavaPackage(content, packageFileUrl),
                _ => new PackageMetadata { EcosystemType = packageType, RawContent = content }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "패키지 파일 분석 실패: {Url}", packageFileUrl);
            return new PackageMetadata { EcosystemType = packageType };
        }
    }

    /// <inheritdoc />
    public async Task<TechStackAnalysisResult> AnalyzeTechStackAsync(PackageMetadata packageMetadata)
    {
        var result = new TechStackAnalysisResult
        {
            PrimaryLanguage = GetPrimaryLanguage(packageMetadata.EcosystemType)
        };

        // 프레임워크 감지
        result.Frameworks = DetectFrameworks(packageMetadata);

        // 데이터베이스 감지
        result.Databases = DetectDatabases(packageMetadata);

        // 빌드 도구 감지
        result.BuildTools = DetectBuildTools(packageMetadata);

        // 테스트 프레임워크 감지
        result.TestingFrameworks = DetectTestingFrameworks(packageMetadata);

        // 프로젝트 타입 감지
        result.ProjectType = DetectProjectType(packageMetadata);

        // 아키텍처 패턴 감지
        result.ArchitecturalPatterns = DetectArchitecturalPatterns(packageMetadata);

        // 기술 성숙도 점수 계산
        result.TechMaturityScore = CalculateTechMaturityScore(packageMetadata, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<ProjectComplexityResult> EvaluateProjectComplexityAsync(PackageMetadata packageMetadata)
    {
        var result = new ProjectComplexityResult
        {
            DependencyCount = packageMetadata.Dependencies.Count + packageMetadata.DevDependencies.Count
        };

        // 프로젝트 규모 결정
        result.Scale = result.DependencyCount switch
        {
            < 10 => ProjectScale.Small,
            < 50 => ProjectScale.Medium,
            < 200 => ProjectScale.Large,
            _ => ProjectScale.Enterprise
        };

        // 성숙도 레벨 평가
        result.MaturityLevel = EvaluateMaturityLevel(packageMetadata);

        // 복잡도 요인 분석
        result.ComplexityFactors = AnalyzeComplexityFactors(packageMetadata);

        // 복잡도 점수 계산
        result.ComplexityScore = CalculateComplexityScore(result);

        // 유지보수성 점수 계산
        result.MaintainabilityScore = CalculateMaintainabilityScore(packageMetadata, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<SecurityAnalysisResult> AnalyzeSecurityRisksAsync(PackageMetadata packageMetadata)
    {
        var result = new SecurityAnalysisResult();

        // 알려진 취약성 검사 (간소화된 구현)
        result.KnownVulnerabilities = DetectKnownVulnerabilities(packageMetadata);

        // 위험한 의존성 감지
        result.RiskyDependencies = DetectRiskyDependencies(packageMetadata);

        // 보안 권장사항 생성
        result.Recommendations = GenerateSecurityRecommendations(packageMetadata, result);

        // 라이선스 호환성 검사
        result.LicenseCompatibility = AnalyzeLicenseCompatibility(packageMetadata);

        // 보안 점수 계산
        result.SecurityScore = CalculateSecurityScore(result);

        return result;
    }

    #region Private Helper Methods

    private async Task<PackageFileInfo?> TryDiscoverPackageFileAsync(string baseUrl, string filePath, PackageEcosystemType ecosystemType, CancellationToken cancellationToken)
    {
        try
        {
            var url = new Uri(new Uri(baseUrl), filePath).ToString();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new PackageFileInfo
                {
                    Url = url,
                    FileName = filePath,
                    EcosystemType = ecosystemType,
                    FileSize = response.Content.Headers.ContentLength ?? 0,
                    DiscoveryMethod = "Direct URL check"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("패키지 파일 발견 실패: {FilePath} - {Error}", filePath, ex.Message);
        }

        return null;
    }

    private PackageFileInfo? SelectPrimaryPackageFile(List<PackageFileInfo> discoveredFiles)
    {
        // 우선순위: 메인 패키지 파일 > 락 파일
        var priorities = new Dictionary<string, int>
        {
            ["package.json"] = 100,
            ["composer.json"] = 95,
            ["requirements.txt"] = 90,
            ["pom.xml"] = 85,
            ["build.gradle"] = 80,
            ["Cargo.toml"] = 75,
            ["go.mod"] = 70,
            ["pubspec.yaml"] = 65,
            ["Package.swift"] = 60,
            ["Gemfile"] = 55
        };

        return discoveredFiles
            .OrderByDescending(f => priorities.GetValueOrDefault(f.FileName, 0))
            .FirstOrDefault();
    }

    private PackageMetadata ParseNodeJsPackage(string content, string url)
    {
        try
        {
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            var metadata = new PackageMetadata
            {
                EcosystemType = PackageEcosystemType.NodeJs,
                ProjectName = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Version = root.TryGetProperty("version", out var version) ? version.GetString() ?? "" : "",
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                License = root.TryGetProperty("license", out var license) ? license.GetString() ?? "" : "",
                RawContent = content
            };

            // 저장소 URL 추출
            if (root.TryGetProperty("repository", out var repo))
            {
                if (repo.ValueKind == JsonValueKind.Object && repo.TryGetProperty("url", out var repoUrl))
                {
                    metadata.RepositoryUrl = repoUrl.GetString() ?? "";
                }
                else if (repo.ValueKind == JsonValueKind.String)
                {
                    metadata.RepositoryUrl = repo.GetString() ?? "";
                }
            }

            // 키워드 추출
            if (root.TryGetProperty("keywords", out var keywords) && keywords.ValueKind == JsonValueKind.Array)
            {
                metadata.Keywords = keywords.EnumerateArray()
                    .Where(k => k.ValueKind == JsonValueKind.String)
                    .Select(k => k.GetString()!)
                    .ToList();
            }

            // 의존성 추출
            if (root.TryGetProperty("dependencies", out var deps))
            {
                metadata.Dependencies = ParseDependencies(deps, DependencyType.Production);
            }

            if (root.TryGetProperty("devDependencies", out var devDeps))
            {
                metadata.DevDependencies = ParseDependencies(devDeps, DependencyType.Development);
            }

            // 스크립트 추출
            if (root.TryGetProperty("scripts", out var scripts))
            {
                foreach (var script in scripts.EnumerateObject())
                {
                    metadata.Scripts[script.Name] = script.Value.GetString() ?? "";
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Node.js 패키지 파싱 실패: {Url}", url);
            return new PackageMetadata { EcosystemType = PackageEcosystemType.NodeJs, RawContent = content };
        }
    }

    private PackageMetadata ParsePythonPackage(string content, string url)
    {
        var metadata = new PackageMetadata
        {
            EcosystemType = PackageEcosystemType.Python,
            RawContent = content
        };

        if (url.EndsWith("requirements.txt"))
        {
            // requirements.txt 파싱
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed))
                    continue;

                var dependency = ParsePythonRequirement(trimmed);
                if (dependency != null)
                {
                    metadata.Dependencies.Add(dependency);
                }
            }
        }
        // setup.py, pyproject.toml 등의 파싱도 필요시 구현

        return metadata;
    }

    private PackageMetadata ParsePhpPackage(string content, string url)
    {
        try
        {
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            var metadata = new PackageMetadata
            {
                EcosystemType = PackageEcosystemType.Php,
                ProjectName = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                RawContent = content
            };

            // PHP 특화 파싱 로직 구현
            if (root.TryGetProperty("require", out var require))
            {
                metadata.Dependencies = ParseDependencies(require, DependencyType.Production);
            }

            if (root.TryGetProperty("require-dev", out var requireDev))
            {
                metadata.DevDependencies = ParseDependencies(requireDev, DependencyType.Development);
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PHP 패키지 파싱 실패: {Url}", url);
            return new PackageMetadata { EcosystemType = PackageEcosystemType.Php, RawContent = content };
        }
    }

    private PackageMetadata ParseCSharpPackage(string content, string url)
    {
        // .csproj 파일 XML 파싱 또는 packages.config 파싱
        var metadata = new PackageMetadata
        {
            EcosystemType = PackageEcosystemType.CSharp,
            RawContent = content
        };

        // XML 파싱 로직 구현 필요
        return metadata;
    }

    private PackageMetadata ParseJavaPackage(string content, string url)
    {
        // pom.xml 또는 build.gradle 파싱
        var metadata = new PackageMetadata
        {
            EcosystemType = PackageEcosystemType.Java,
            RawContent = content
        };

        // XML/Gradle 파싱 로직 구현 필요
        return metadata;
    }

    private List<DependencyInfo> ParseDependencies(JsonElement depsElement, DependencyType type)
    {
        var dependencies = new List<DependencyInfo>();

        foreach (var dep in depsElement.EnumerateObject())
        {
            dependencies.Add(new DependencyInfo
            {
                Name = dep.Name,
                VersionConstraint = dep.Value.GetString() ?? "",
                Type = type,
                Category = CategorizePackage(dep.Name),
                PopularityScore = EstimatePopularity(dep.Name) // 간단한 추정
            });
        }

        return dependencies;
    }

    private DependencyInfo? ParsePythonRequirement(string requirement)
    {
        // requirements.txt 라인 파싱: package==1.0.0, package>=1.0
        var match = Regex.Match(requirement, @"^([a-zA-Z0-9\-_]+)([><=!]+.*)?$");
        if (match.Success)
        {
            return new DependencyInfo
            {
                Name = match.Groups[1].Value,
                VersionConstraint = match.Groups[2].Value,
                Type = DependencyType.Production,
                Category = CategorizePackage(match.Groups[1].Value)
            };
        }

        return null;
    }

    private string GetPrimaryLanguage(PackageEcosystemType ecosystemType)
    {
        return ecosystemType switch
        {
            PackageEcosystemType.NodeJs => "JavaScript/TypeScript",
            PackageEcosystemType.Python => "Python",
            PackageEcosystemType.CSharp => "C#",
            PackageEcosystemType.Java => "Java",
            PackageEcosystemType.Php => "PHP",
            PackageEcosystemType.Ruby => "Ruby",
            PackageEcosystemType.Go => "Go",
            PackageEcosystemType.Rust => "Rust",
            PackageEcosystemType.Swift => "Swift",
            PackageEcosystemType.Dart => "Dart",
            _ => "Unknown"
        };
    }

    private List<FrameworkInfo> DetectFrameworks(PackageMetadata packageMetadata)
    {
        var frameworks = new List<FrameworkInfo>();

        // 프레임워크 감지 패턴
        var frameworkPatterns = new Dictionary<string, (string Category, double Popularity)>
        {
            ["react"] = ("Frontend", 0.95),
            ["vue"] = ("Frontend", 0.85),
            ["angular"] = ("Frontend", 0.80),
            ["express"] = ("Backend", 0.90),
            ["django"] = ("Backend", 0.85),
            ["flask"] = ("Backend", 0.80),
            ["laravel"] = ("Backend", 0.85),
            ["spring"] = ("Backend", 0.90),
            ["@nestjs"] = ("Backend", 0.75)
        };

        foreach (var dep in packageMetadata.Dependencies.Concat(packageMetadata.DevDependencies))
        {
            foreach (var pattern in frameworkPatterns)
            {
                if (dep.Name.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
                {
                    frameworks.Add(new FrameworkInfo
                    {
                        Name = pattern.Key,
                        Version = dep.VersionConstraint,
                        Category = pattern.Value.Category,
                        PopularityScore = pattern.Value.Popularity
                    });
                }
            }
        }

        return frameworks;
    }

    private List<string> DetectDatabases(PackageMetadata packageMetadata)
    {
        var databases = new List<string>();
        var dbPatterns = new[] { "mysql", "postgres", "mongodb", "redis", "sqlite", "oracle", "mssql" };

        foreach (var dep in packageMetadata.Dependencies)
        {
            foreach (var pattern in dbPatterns)
            {
                if (dep.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    databases.Add(pattern.ToUpperInvariant());
                }
            }
        }

        return databases.Distinct().ToList();
    }

    private List<string> DetectBuildTools(PackageMetadata packageMetadata)
    {
        var buildTools = new List<string>();

        // 스크립트에서 빌드 도구 감지
        foreach (var script in packageMetadata.Scripts)
        {
            if (script.Value.Contains("webpack")) buildTools.Add("Webpack");
            if (script.Value.Contains("rollup")) buildTools.Add("Rollup");
            if (script.Value.Contains("vite")) buildTools.Add("Vite");
            if (script.Value.Contains("parcel")) buildTools.Add("Parcel");
            if (script.Value.Contains("gulp")) buildTools.Add("Gulp");
            if (script.Value.Contains("grunt")) buildTools.Add("Grunt");
        }

        return buildTools.Distinct().ToList();
    }

    private List<string> DetectTestingFrameworks(PackageMetadata packageMetadata)
    {
        var testFrameworks = new List<string>();
        var testPatterns = new[] { "jest", "mocha", "jasmine", "cypress", "playwright", "selenium", "pytest", "unittest" };

        foreach (var dep in packageMetadata.DevDependencies)
        {
            foreach (var pattern in testPatterns)
            {
                if (dep.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    testFrameworks.Add(pattern);
                }
            }
        }

        return testFrameworks.Distinct().ToList();
    }

    private ProjectType DetectProjectType(PackageMetadata packageMetadata)
    {
        // 의존성과 스크립트를 기반으로 프로젝트 타입 추정
        var hasWebFramework = packageMetadata.Dependencies.Any(d =>
            d.Name.Contains("react", StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains("vue", StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains("angular", StringComparison.OrdinalIgnoreCase));

        var hasServerFramework = packageMetadata.Dependencies.Any(d =>
            d.Name.Contains("express", StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains("fastify", StringComparison.OrdinalIgnoreCase));

        if (hasWebFramework) return ProjectType.WebApplication;
        if (hasServerFramework) return ProjectType.API;

        // 라이브러리 패턴 감지
        if (packageMetadata.Scripts.ContainsKey("build") && !packageMetadata.Scripts.ContainsKey("start"))
        {
            return ProjectType.Library;
        }

        return ProjectType.Unknown;
    }

    private List<string> DetectArchitecturalPatterns(PackageMetadata packageMetadata)
    {
        var patterns = new List<string>();

        // 아키텍처 패턴 감지 로직
        if (packageMetadata.Dependencies.Any(d => d.Name.Contains("redux")))
            patterns.Add("Redux Pattern");

        if (packageMetadata.Dependencies.Any(d => d.Name.Contains("rxjs")))
            patterns.Add("Reactive Pattern");

        return patterns;
    }

    private double CalculateTechMaturityScore(PackageMetadata packageMetadata, TechStackAnalysisResult techStack)
    {
        // 기술 성숙도 점수 계산 로직
        var score = 0.5; // 기본 점수

        // 인기 있는 프레임워크 사용 시 점수 증가
        score += techStack.Frameworks.Sum(f => f.PopularityScore) * 0.2;

        // 테스트 프레임워크 존재 시 점수 증가
        if (techStack.TestingFrameworks.Any())
            score += 0.2;

        return Math.Min(score, 1.0);
    }

    private ProjectMaturityLevel EvaluateMaturityLevel(PackageMetadata packageMetadata)
    {
        if (string.IsNullOrEmpty(packageMetadata.Version))
            return ProjectMaturityLevel.Unknown;

        if (packageMetadata.Version.StartsWith("0."))
            return ProjectMaturityLevel.Experimental;

        if (packageMetadata.Version.StartsWith("1.0") || packageMetadata.Version.StartsWith("0.9"))
            return ProjectMaturityLevel.Beta;

        return ProjectMaturityLevel.Stable;
    }

    private List<ComplexityFactor> AnalyzeComplexityFactors(PackageMetadata packageMetadata)
    {
        var factors = new List<ComplexityFactor>();

        // 의존성 수
        factors.Add(new ComplexityFactor
        {
            Name = "Dependency Count",
            Score = Math.Min(packageMetadata.Dependencies.Count / 50.0, 1.0),
            Description = $"{packageMetadata.Dependencies.Count} 개의 의존성"
        });

        // 스크립트 복잡도
        factors.Add(new ComplexityFactor
        {
            Name = "Build Complexity",
            Score = Math.Min(packageMetadata.Scripts.Count / 10.0, 1.0),
            Description = $"{packageMetadata.Scripts.Count} 개의 빌드 스크립트"
        });

        return factors;
    }

    private double CalculateComplexityScore(ProjectComplexityResult result)
    {
        if (!result.ComplexityFactors.Any())
            return 0.0;

        return result.ComplexityFactors.Average(f => f.Score);
    }

    private double CalculateMaintainabilityScore(PackageMetadata packageMetadata, ProjectComplexityResult complexity)
    {
        var score = 1.0;

        // 복잡도에 따른 유지보수성 감소
        score -= complexity.ComplexityScore * 0.3;

        // 테스트 존재 여부
        var hasTests = packageMetadata.DevDependencies.Any(d =>
            d.Name.Contains("test", StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains("jest", StringComparison.OrdinalIgnoreCase));

        if (hasTests) score += 0.2;

        return Math.Max(score, 0.0);
    }

    private List<VulnerabilityInfo> DetectKnownVulnerabilities(PackageMetadata packageMetadata)
    {
        // 실제 구현에서는 CVE 데이터베이스나 보안 API와 연동
        var vulnerabilities = new List<VulnerabilityInfo>();

        // 간단한 패턴 기반 감지 (예시)
        foreach (var dep in packageMetadata.Dependencies)
        {
            if (dep.Name.Contains("lodash") && dep.VersionConstraint.Contains("4.17.10"))
            {
                vulnerabilities.Add(new VulnerabilityInfo
                {
                    CveId = "CVE-2019-10744",
                    AffectedPackage = dep.Name,
                    Severity = VulnerabilitySeverity.Medium,
                    Description = "Prototype pollution vulnerability",
                    FixedInVersion = "4.17.12"
                });
            }
        }

        return vulnerabilities;
    }

    private List<RiskyDependency> DetectRiskyDependencies(PackageMetadata packageMetadata)
    {
        var riskyDeps = new List<RiskyDependency>();

        foreach (var dep in packageMetadata.Dependencies)
        {
            // 사용 중단된 패키지 감지 (예시)
            if (dep.Name.Contains("request"))
            {
                riskyDeps.Add(new RiskyDependency
                {
                    PackageName = dep.Name,
                    RiskType = RiskType.Deprecated,
                    RiskScore = 0.7,
                    RiskDescription = "이 패키지는 더 이상 유지보수되지 않습니다"
                });
            }
        }

        return riskyDeps;
    }

    private List<SecurityRecommendation> GenerateSecurityRecommendations(PackageMetadata packageMetadata, SecurityAnalysisResult securityResult)
    {
        var recommendations = new List<SecurityRecommendation>();

        if (securityResult.KnownVulnerabilities.Any())
        {
            recommendations.Add(new SecurityRecommendation
            {
                Type = RecommendationType.UpdateDependency,
                Title = "취약한 의존성 업데이트",
                Description = "알려진 보안 취약성이 있는 패키지를 최신 버전으로 업데이트하세요",
                Priority = 0.9
            });
        }

        return recommendations;
    }

    private LicenseCompatibilityResult AnalyzeLicenseCompatibility(PackageMetadata packageMetadata)
    {
        var result = new LicenseCompatibilityResult
        {
            CommercialUseAllowed = true,
            CompatibilityScore = 1.0
        };

        // GPL 라이선스 감지
        if (packageMetadata.License.Contains("GPL", StringComparison.OrdinalIgnoreCase))
        {
            result.CommercialUseAllowed = false;
            result.CompatibilityScore = 0.5;
        }

        return result;
    }

    private double CalculateSecurityScore(SecurityAnalysisResult result)
    {
        var score = 1.0;

        // 취약성에 따른 점수 감소
        score -= result.KnownVulnerabilities.Count * 0.2;
        score -= result.RiskyDependencies.Count * 0.1;

        return Math.Max(score, 0.0);
    }

    private string CategorizePackage(string packageName)
    {
        var categories = new Dictionary<string, string>
        {
            ["framework"] = "Framework",
            ["library"] = "Library",
            ["util"] = "Utility",
            ["test"] = "Testing",
            ["build"] = "Build Tool",
            ["babel"] = "Build Tool",
            ["webpack"] = "Build Tool",
            ["eslint"] = "Linting",
            ["prettier"] = "Formatting"
        };

        foreach (var category in categories)
        {
            if (packageName.Contains(category.Key, StringComparison.OrdinalIgnoreCase))
            {
                return category.Value;
            }
        }

        return "Other";
    }

    private double EstimatePopularity(string packageName)
    {
        // 간단한 인기도 추정 (실제 구현에서는 NPM/PyPI API 사용)
        var popularPackages = new Dictionary<string, double>
        {
            ["react"] = 0.95,
            ["vue"] = 0.85,
            ["lodash"] = 0.90,
            ["express"] = 0.90,
            ["axios"] = 0.85
        };

        return popularPackages.GetValueOrDefault(packageName.ToLower(), 0.5);
    }

    private double CalculateQualityScore(PackageEcosystemAnalysisResult result)
    {
        var score = 0.0;

        // 파일 발견 점수
        score += Math.Min(result.DiscoveredPackageFiles.Count / 5.0, 0.3);

        // 기술 스택 분석 품질
        if (result.PrimaryTechStack != null)
        {
            score += result.PrimaryTechStack.TechMaturityScore * 0.3;
        }

        // 보안 점수
        if (result.SecurityAnalysis != null)
        {
            score += result.SecurityAnalysis.SecurityScore * 0.4;
        }

        return Math.Min(score, 1.0);
    }

    #endregion
}