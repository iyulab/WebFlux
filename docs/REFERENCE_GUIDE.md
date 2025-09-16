# WebFlux SDK 레퍼런스 가이드

> 개발자를 위한 완전한 WebFlux SDK 구현 레퍼런스

## 📚 문서 구조 개요

WebFlux SDK의 설계 문서는 다음과 같이 구성되어 있으며, 각 문서는 특정 관점에서 시스템을 설명합니다:

```
📁 docs/
├── 📄 ARCHITECTURE.md      - 전체 시스템 아키텍처
├── 📄 INTERFACES.md        - 인터페이스 계약 및 API 설계
├── 📄 CHUNKING_STRATEGIES.md - 7가지 청킹 전략 상세 설계
├── 📄 PIPELINE_DESIGN.md   - 4단계 처리 파이프라인 설계
├── 📄 MULTIMODAL_DESIGN.md - 이미지-텍스트 통합 처리 설계
├── 📄 PERFORMANCE_DESIGN.md - 성능 최적화 및 확장성 설계
└── 📄 REFERENCE_GUIDE.md   - 이 문서 (통합 가이드)

📁 claudedocs/
└── 📄 project_context.md   - 프로젝트 현재 상태 및 컨텍스트

📄 TASKS.md                 - 5단계 구현 로드맵
📄 README.md                - 사용자용 SDK 문서
```

## 🎯 구현 우선순위 가이드

### Phase 1: Foundation (기반 구축) - 즉시 시작 가능
**목표**: 개발 가능한 기반 인프라 완성

#### 필수 구현 순서
1. **프로젝트 구조** (`src/WebFlux/`)
   ```
   WebFlux/
   ├── Core/
   │   ├── Interfaces/
   │   ├── Models/
   │   └── Exceptions/
   ├── Infrastructure/
   │   ├── Crawlers/
   │   ├── Extractors/
   │   └── ChunkingStrategies/
   ├── Testing/
   │   └── Mocks/
   └── Extensions/
       └── DependencyInjection/
   ```

2. **핵심 인터페이스** (INTERFACES.md 참조)
   - `ITextCompletionService` - AI 서비스 추상화
   - `IWebContentProcessor` - 메인 파이프라인
   - `ICrawler`, `IContentExtractor`, `IChunkingStrategy`

3. **도메인 모델** (ARCHITECTURE.md 참조)
   - `WebContentChunk` - RAG 최적화 데이터 구조
   - `CrawlOptions`, `ChunkingOptions` - 설정 모델
   - `ProcessingResult<T>` - 결과 래퍼

#### 개발 시작점
```csharp
// 1. 가장 먼저 구현할 인터페이스
public interface IWebContentProcessor
{
    Task<ProcessingResult<IEnumerable<WebContentChunk>>> ProcessAsync(
        string baseUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);
}

// 2. Mock 서비스로 테스트 가능한 기반 구축
public class MockTextCompletionService : ITextCompletionService
{
    // 실제 LLM 없이 테스트 가능한 구현
}
```

### Phase 2: Core Processing (핵심 처리) - 4주차 목표
**참조**: PIPELINE_DESIGN.md

#### 구현 우선순위
1. **BreadthFirstCrawler** - 가장 단순한 크롤링 전략
2. **HtmlContentExtractor** - 가장 중요한 추출기
3. **FixedSizeChunkingStrategy** - 가장 단순한 청킹
4. **기본 파이프라인** - 4단계 연결

#### 검증 기준
- 실제 웹사이트 크롤링 성공
- 청크 생성 및 품질 검증
- 기본 성능: 10페이지/분 (최종 목표의 10%)

### Phase 3: Advanced Chunking (고급 청킹) - 8주차 목표
**참조**: CHUNKING_STRATEGIES.md

#### 구현 순서 (복잡도 순)
1. **ParagraphChunkingStrategy** (복잡도: 낮음)
2. **SmartChunkingStrategy** (복잡도: 중간)
3. **SemanticChunkingStrategy** (복잡도: 높음)
4. **AutoChunkingStrategy** (복잡도: 매우 높음)

## 🏗️ 아키텍처 구현 가이드

### Clean Architecture 구현

#### 의존성 규칙
```csharp
// ❌ 잘못된 의존성
public class WebContentProcessor
{
    private readonly OpenAiService _openAi; // 구체적 구현체 의존
}

// ✅ 올바른 의존성
public class WebContentProcessor
{
    private readonly ITextCompletionService _llmService; // 인터페이스 의존
}
```

#### 레이어 분리
```
Application Layer (IWebContentProcessor)
    ↓ (의존성 역전)
Domain Layer (Business Logic + Interfaces)
    ↓ (의존성 역전)
Infrastructure Layer (구체적 구현)
```

### 의존성 주입 설정

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebFlux(this IServiceCollection services)
    {
        // 핵심 서비스
        services.AddScoped<IWebContentProcessor, WebContentProcessor>();

        // 팩토리 서비스
        services.AddScoped<ICrawlerFactory, CrawlerFactory>();
        services.AddScoped<IChunkingStrategyFactory, ChunkingStrategyFactory>();

        // 크롤러 등록 (모든 전략)
        services.AddTransient<ICrawler, BreadthFirstCrawler>();
        services.AddTransient<ICrawler, DepthFirstCrawler>();
        services.AddTransient<ICrawler, SitemapCrawler>();

        // 청킹 전략 등록 (7가지)
        services.AddTransient<IChunkingStrategy, FixedSizeChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, ParagraphChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, SmartChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, SemanticChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, IntelligentChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, MemoryOptimizedChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, AutoChunkingStrategy>();

        return services;
    }
}
```

## 🔧 청킹 전략 구현 가이드

### 전략 선택 알고리즘

```csharp
public class ChunkingStrategySelector
{
    public string SelectOptimalStrategy(ParsedWebContent content, ChunkingOptions options)
    {
        // 1. 명시적 지정 우선
        if (options.Strategy != "Auto")
            return options.Strategy;

        // 2. 콘텐츠 분석 기반 선택
        var analysis = AnalyzeContent(content);

        // 3. 결정 트리
        if (analysis.StructuralComplexity > 0.8)
            return "Smart";    // 구조가 복잡하면 구조 기반

        if (analysis.SemanticDensity > 0.7)
            return "Intelligent"; // 의미 밀도가 높으면 LLM 기반

        if (content.MainContent.Length > 50000)
            return "MemoryOptimized"; // 큰 콘텐츠는 메모리 최적화

        return "Semantic"; // 기본값
    }
}
```

### 청킹 품질 보장

```csharp
public class ChunkingQualityController
{
    public async Task<IEnumerable<WebContentChunk>> EnsureQuality(
        IEnumerable<WebContentChunk> chunks,
        ParsedWebContent originalContent,
        double minimumQuality = 0.75)
    {
        var qualityScore = _evaluator.EvaluateQuality(chunks, originalContent);

        if (qualityScore.OverallQuality < minimumQuality)
        {
            // 품질이 낮으면 대안 전략 시도
            return await RetryWithAlternativeStrategy(originalContent);
        }

        return chunks;
    }
}
```

## ⚡ 성능 최적화 구현 가이드

### 병렬 처리 패턴

#### Channel 기반 파이프라인
```csharp
public async IAsyncEnumerable<ProcessingResult<WebContentChunk>>
    ProcessStreamingAsync(string baseUrl)
{
    // 채널 생성
    var crawlChannel = Channel.CreateBounded<CrawlResult>(100);
    var extractChannel = Channel.CreateBounded<RawWebContent>(50);

    // 병렬 처리 시작
    var crawlTask = CrawlAsync(baseUrl, crawlChannel.Writer);
    var extractTask = ExtractAsync(crawlChannel.Reader, extractChannel.Writer);

    // 스트리밍 결과 반환
    await foreach (var rawContent in extractChannel.Reader.ReadAllAsync())
    {
        var chunks = await ProcessContent(rawContent);
        yield return ProcessingResult<WebContentChunk>.Success(chunks);
    }
}
```

#### 메모리 압박 제어
```csharp
public class MemoryPressureController
{
    public async Task<bool> ShouldThrottleAsync()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        var threshold = _config.MaxMemoryUsage;

        if (memoryUsage > threshold * 0.8)
        {
            GC.Collect(0, GCCollectionMode.Optimized);
            await Task.Delay(100);
            return memoryUsage > threshold;
        }

        return false;
    }
}
```

### 성능 목표 달성 전략

| 목표 | 구현 방법 | 측정 방법 |
|------|-----------|-----------|
| **100페이지/분** | 병렬 크롤링 + 캐싱 | `throughput_WebCrawl` 메트릭 |
| **84% 메모리 절감** | 스트리밍 + GC 최적화 | 메모리 사용량 비교 |
| **청크 완성도 81%+** | 품질 평가 + 재시도 | 자동 품질 평가 |

## 🖼️ 멀티모달 구현 가이드

### 텍스트 기반화 패턴

```csharp
public class ImageToTextProcessor
{
    public async Task<string> ProcessImageAsync(string imageUrl, string contextText)
    {
        // 1. 이미지 다운로드 및 검증
        var imageData = await _imageDownloader.DownloadAsync(imageUrl);
        if (!IsValidImage(imageData)) return string.Empty;

        // 2. 맥락 기반 프롬프트 생성
        var prompt = GenerateContextualPrompt(imageUrl, contextText);

        // 3. MLLM으로 설명 생성
        var description = await _imageToTextService.ExtractTextFromImageAsync(
            imageData, new ImageToTextOptions { ContextPrompt = prompt });

        return description.ExtractedText;
    }

    private string GenerateContextualPrompt(string imageUrl, string context)
    {
        return $"""
        다음 이미지를 분석해주세요:

        맥락: {context}

        RAG 검색에 최적화된 상세한 설명을 제공해주세요.
        """;
    }
}
```

### 맥락적 통합 패턴

```csharp
public class ContextualMerger
{
    public async Task<ParsedWebContent> MergeImageDescriptions(
        ParsedWebContent textContent,
        List<ImageDescription> imageDescriptions)
    {
        var enhancedContent = textContent.DeepClone();

        foreach (var imageDesc in imageDescriptions)
        {
            // 이미지와 관련된 텍스트 섹션 찾기
            var relatedSection = FindRelatedSection(imageDesc, enhancedContent);
            if (relatedSection != null)
            {
                // 섹션에 이미지 정보 통합
                relatedSection.Content += $"\n\n**관련 이미지**: {imageDesc.Description}";
            }
        }

        return enhancedContent;
    }
}
```

## 🧪 테스팅 가이드

### 단위 테스트 패턴

```csharp
[TestClass]
public class ChunkingStrategyTests
{
    private readonly MockTextCompletionService _mockLLM;
    private readonly SmartChunkingStrategy _strategy;

    public ChunkingStrategyTests()
    {
        _mockLLM = new MockTextCompletionService();
        _strategy = new SmartChunkingStrategy();
    }

    [TestMethod]
    public async Task ChunkAsync_WithStructuredContent_ShouldPreserveHeaders()
    {
        // Arrange
        var content = CreateTestContent();
        var options = new ChunkingOptions { MaxChunkSize = 512 };

        // Act
        var chunks = await _strategy.ChunkAsync(content, options);

        // Assert
        Assert.IsTrue(chunks.Any());
        Assert.IsTrue(chunks.All(c => c.Content.Length <= options.MaxChunkSize));

        // 헤더 정보가 메타데이터에 보존되었는지 확인
        var headerChunks = chunks.Where(c =>
            c.Metadata.Properties.ContainsKey("HeaderText"));
        Assert.IsTrue(headerChunks.Any());
    }
}
```

### 통합 테스트 패턴

```csharp
[TestClass]
public class WebContentProcessorIntegrationTests
{
    [TestMethod]
    public async Task ProcessAsync_WithRealWebsite_ShouldGenerateChunks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWebFlux();
        services.AddScoped<ITextCompletionService, MockTextCompletionService>();

        var provider = services.BuildServiceProvider();
        var processor = provider.GetRequiredService<IWebContentProcessor>();

        // Act
        var result = await processor.ProcessAsync("https://example.com",
            new CrawlOptions { MaxPages = 5 });

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Result!.Any());

        // 품질 검증
        var qualityScore = EvaluateChunkQuality(result.Result);
        Assert.IsTrue(qualityScore > 0.75);
    }
}
```

### 성능 테스트 패턴

```csharp
[TestClass]
public class PerformanceTests
{
    [TestMethod]
    public async Task ProcessAsync_100Pages_ShouldMeetThroughputTarget()
    {
        // Arrange
        var processor = CreateProcessor();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await processor.ProcessAsync("https://test-site.com",
            new CrawlOptions { MaxPages = 100 });

        stopwatch.Stop();

        // Assert - 100페이지를 1분 내에 처리해야 함
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromMinutes(1));

        var throughput = 100.0 / stopwatch.Elapsed.TotalMinutes;
        Assert.IsTrue(throughput >= 100); // 100페이지/분
    }
}
```

## 🔍 디버깅 및 문제 해결

### 로깅 전략

```csharp
public class WebContentProcessor : IWebContentProcessor
{
    private readonly ILogger<WebContentProcessor> _logger;

    public async Task<ProcessingResult<IEnumerable<WebContentChunk>>> ProcessAsync(...)
    {
        using var activity = _logger.BeginScope("Processing {BaseUrl}", baseUrl);

        try
        {
            _logger.LogInformation("Starting web content processing");

            var result = await ProcessInternal(baseUrl, crawlOptions, chunkingOptions);

            _logger.LogInformation("Processing completed successfully. Chunks: {ChunkCount}",
                result.Result?.Count() ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed for {BaseUrl}", baseUrl);
            throw;
        }
    }
}
```

### 일반적인 문제 해결

#### 문제: 크롤링 속도가 느림
```csharp
// 해결방안: 병렬 처리 수 증가
var crawlOptions = new CrawlOptions
{
    MaxConcurrentRequests = Environment.ProcessorCount * 2, // 기본값의 2배
    DelayBetweenRequests = TimeSpan.FromMilliseconds(100)   // 지연 감소
};
```

#### 문제: 메모리 사용량 과다
```csharp
// 해결방안: 스트리밍 처리 사용
await foreach (var result in processor.ProcessWithProgressAsync(baseUrl))
{
    // 청크별 즉시 처리
    await ProcessSingleChunk(result);

    // 주기적 메모리 정리
    if (processedCount % 50 == 0)
    {
        GC.Collect(0, GCCollectionMode.Optimized);
    }
}
```

#### 문제: 청킹 품질 저하
```csharp
// 해결방안: Auto 전략 사용 + 품질 임계값 설정
var chunkingOptions = new ChunkingOptions
{
    Strategy = "Auto",                    // 자동 최적화
    QualityThreshold = 0.8,              // 높은 품질 요구
    EnableQualityRetry = true            // 품질 미달 시 재시도
};
```

## 📈 모니터링 및 메트릭

### 핵심 메트릭

```csharp
public class WebFluxMetrics
{
    // 성능 메트릭
    public static readonly Counter PagesProcessedCounter =
        Metrics.CreateCounter("webflux_pages_processed_total", "Total processed pages");

    public static readonly Histogram ProcessingDuration =
        Metrics.CreateHistogram("webflux_processing_duration_seconds", "Processing duration");

    public static readonly Gauge MemoryUsage =
        Metrics.CreateGauge("webflux_memory_usage_bytes", "Current memory usage");

    // 품질 메트릭
    public static readonly Histogram ChunkQuality =
        Metrics.CreateHistogram("webflux_chunk_quality_score", "Chunk quality score");

    public static readonly Counter ChunkingErrors =
        Metrics.CreateCounter("webflux_chunking_errors_total", "Chunking errors");
}
```

### 대시보드 구성

#### Grafana 대시보드 예시
- **처리량**: Pages/minute, Chunks/minute
- **성능**: 평균 처리 시간, P95 처리 시간
- **메모리**: 사용량, GC 빈도
- **품질**: 평균 청크 품질, 에러율
- **시스템**: CPU 사용률, 스레드 수

## 🚀 배포 및 운영

### Docker 구성

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/ .

# 성능 최적화 환경 변수
ENV DOTNET_GCServer=1
ENV DOTNET_GCConcurrent=1
ENV DOTNET_GCRetainVM=1

ENTRYPOINT ["dotnet", "WebFlux.dll"]
```

### 환경별 구성

```csharp
// appsettings.Production.json
{
  "WebFlux": {
    "Performance": {
      "MaxWorkers": null,           // CPU 코어 수만큼 자동 설정
      "MemoryThreshold": "2GB",     // 메모리 임계값
      "CacheSize": 10000           // 캐시 항목 수
    },
    "Chunking": {
      "DefaultStrategy": "Auto",    // 프로덕션에서는 Auto 전략
      "QualityThreshold": 0.8      // 높은 품질 요구
    }
  }
}
```

## 📋 체크리스트

### Phase 1 완료 체크리스트
- [ ] 프로젝트 구조 생성
- [ ] 핵심 인터페이스 정의
- [ ] Mock 서비스 구현
- [ ] DI 컨테이너 설정
- [ ] 기본 단위 테스트 작성

### Phase 2 완료 체크리스트
- [ ] 기본 크롤러 구현
- [ ] HTML 추출기 구현
- [ ] 기본 청킹 전략 구현
- [ ] 파이프라인 연동
- [ ] 통합 테스트 통과

### Phase 3 완료 체크리스트
- [ ] 7가지 청킹 전략 모두 구현
- [ ] Auto 전략 동작 확인
- [ ] 품질 평가 시스템 구현
- [ ] 성능 벤치마크 달성

### 품질 검증 체크리스트
- [ ] 코드 커버리지 90% 이상
- [ ] 모든 단위 테스트 통과
- [ ] 통합 테스트 통과
- [ ] 성능 목표 달성 (100페이지/분)
- [ ] 메모리 효율성 검증 (84% 절감)

---

이 레퍼런스 가이드는 WebFlux SDK의 전체 구현을 위한 실용적인 지침을 제공합니다. 각 Phase별로 순차적으로 구현하면서 품질과 성능을 확보할 수 있도록 설계되었습니다.