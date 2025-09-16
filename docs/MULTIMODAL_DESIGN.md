# WebFlux 멀티모달 처리 설계

> 이미지-텍스트 통합을 통한 차세대 RAG 시스템 구축

## 🎯 멀티모달 처리 개요

연구 문서에 따르면, **웹 페이지, 기술 매뉴얼, 보고서 등은 텍스트만으로는 전달하기 어려운 핵심 정보를 이미지, 다이어그램, 차트와 같은 시각적 요소를 통해 전달**합니다.

WebFlux는 **텍스트 기반화(Text-Grounding)** 접근법을 채택하여, 실용적이고 확장 가능한 멀티모달 RAG 시스템을 구현합니다.

### 핵심 설계 원칙

1. **텍스트 기반화**: 모든 시각적 콘텐츠를 텍스트로 변환하여 기존 텍스트 검색 기술 활용
2. **MLLM 활용**: GPT-4V, GPT-4o, LLaVA 등 강력한 다중모드 LLM 활용
3. **맥락적 연관**: 이미지 설명을 주변 텍스트와 연관시켜 통합 인덱싱
4. **프롬프트 엔지니어링**: 고품질 이미지 설명 생성을 위한 정교한 프롬프트
5. **점진적 구현**: 텍스트 우선, 이미지 추가의 단계적 접근

## 🏗️ 멀티모달 아키텍처

### 전체 처리 플로우

```
┌─────────────────────────────────────────────────────────────────┐
│                Multimodal Processing Pipeline                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │
│  │   Crawler   │───▶│  Extractor  │───▶│    Parser   │          │
│  └─────────────┘    └─────────────┘    └─────────────┘          │
│         │                   │                   │               │
│         ▼                   ▼                   ▼               │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │
│  │  URL Lists  │    │   Content   │    │ Structured  │          │
│  │             │    │ + ImageURLs │    │  Content    │          │
│  └─────────────┘    └─────────────┘    └─────────────┘          │
│                                                 │               │
│                                                 ▼               │
│  ┌──────────────────────────────────────────────────────────────┤
│  │              Multimodal Enhancement                          │
│  │                                                              │
│  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐      │
│  │  │   Image     │───▶│    MLLM     │───▶│   Context   │      │
│  │  │ Processing  │    │ Description │    │  Merging    │      │
│  │  └─────────────┘    └─────────────┘    └─────────────┘      │
│  └──────────────────────────────────────────────────────────────┤
│                                                 │               │
│                                                 ▼               │
│  ┌─────────────────────────────────────────────────────────────┐
│  │                 Chunking Engine                             │
│  │           (Text + Image Descriptions)                      │
│  └─────────────────────────────────────────────────────────────┘
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 핵심 컴포넌트

#### 1. MultimodalContentProcessor
```csharp
public class MultimodalContentProcessor : IMultimodalContentProcessor
{
    private readonly IImageToTextService _imageToTextService;
    private readonly IImageDownloader _imageDownloader;
    private readonly IContextualMerger _contextualMerger;
    private readonly ILogger<MultimodalContentProcessor> _logger;

    public async Task<ParsedWebContent> EnhanceWithMultimodalAsync(
        ParsedWebContent textContent,
        MultimodalOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!textContent.Images.Any() && !textContent.ImageUrls.Any())
        {
            return textContent; // 이미지가 없으면 그대로 반환
        }

        // 1. 이미지 다운로드 및 검증
        var validImages = await ProcessImagesAsync(textContent.ImageUrls, options, cancellationToken);

        // 2. 이미지-텍스트 변환
        var imageDescriptions = await GenerateImageDescriptionsAsync(validImages, textContent, options, cancellationToken);

        // 3. 맥락적 병합
        var enhancedContent = await _contextualMerger.MergeAsync(textContent, imageDescriptions, options);

        return enhancedContent;
    }

    private async Task<List<ProcessedImage>> ProcessImagesAsync(
        List<string> imageUrls,
        MultimodalOptions options,
        CancellationToken cancellationToken)
    {
        var processedImages = new List<ProcessedImage>();
        var semaphore = new SemaphoreSlim(options.MaxConcurrentImageProcessing);

        var tasks = imageUrls.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ProcessSingleImageAsync(url, options, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).ToList()!;
    }

    private async Task<ProcessedImage?> ProcessSingleImageAsync(
        string imageUrl,
        MultimodalOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. 이미지 다운로드
            var imageData = await _imageDownloader.DownloadAsync(imageUrl, cancellationToken);

            // 2. 이미지 검증
            if (!IsValidImage(imageData, options))
            {
                _logger.LogWarning("Invalid image skipped: {ImageUrl}", imageUrl);
                return null;
            }

            // 3. 이미지 메타데이터 추출
            var metadata = await ExtractImageMetadata(imageData);

            return new ProcessedImage
            {
                Url = imageUrl,
                Data = imageData.Data,
                ContentType = imageData.ContentType,
                Size = imageData.Data.Length,
                Width = metadata.Width,
                Height = metadata.Height,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image: {ImageUrl}", imageUrl);
            return null;
        }
    }
}

public class MultimodalOptions
{
    public bool EnableImageProcessing { get; set; } = true;
    public int MaxConcurrentImageProcessing { get; set; } = 3;
    public int MaxImageSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxImageWidth { get; set; } = 4096;
    public int MaxImageHeight { get; set; } = 4096;
    public string[] SupportedFormats { get; set; } = { "jpg", "jpeg", "png", "gif", "webp" };
    public ImageToTextOptions ImageToTextOptions { get; set; } = new();
    public ContextualMergingOptions MergingOptions { get; set; } = new();
}
```

## 🖼️ 이미지-텍스트 변환 (텍스트 기반화)

### 핵심 전략: 맥락 인식 이미지 설명

연구 문서에서 강조했듯이, **생성된 이미지 설명의 품질은 전적으로 MLLM에 제공되는 프롬프트에 의해 결정됩니다**.

#### 고품질 프롬프트 엔지니어링

```csharp
public class ContextAwareImageDescriptionGenerator
{
    private readonly ITextCompletionService _llmService;
    private readonly PromptTemplateEngine _promptEngine;

    public async Task<List<ImageDescription>> GenerateDescriptionsAsync(
        List<ProcessedImage> images,
        ParsedWebContent textContent,
        MultimodalOptions options,
        CancellationToken cancellationToken = default)
    {
        var descriptions = new List<ImageDescription>();

        foreach (var image in images)
        {
            try
            {
                // 1. 이미지 주변 텍스트 추출
                var contextText = ExtractImageContext(image, textContent);

                // 2. 맥락별 프롬프트 생성
                var prompt = await GenerateContextualPrompt(image, contextText, textContent.Title);

                // 3. MLLM으로 설명 생성
                var description = await _llmService.CompleteAsync(prompt, new TextCompletionOptions
                {
                    MaxTokens = options.ImageToTextOptions.MaxTextLength,
                    Temperature = 0.1f, // 일관성을 위해 낮은 온도
                    Model = options.ImageToTextOptions.PreferredModel
                });

                descriptions.Add(new ImageDescription
                {
                    ImageUrl = image.Url,
                    Description = description,
                    ContextText = contextText,
                    GeneratedAt = DateTime.UtcNow,
                    Confidence = CalculateConfidence(description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate description for image: {ImageUrl}", image.Url);
            }
        }

        return descriptions;
    }

    private async Task<string> GenerateContextualPrompt(
        ProcessedImage image,
        string contextText,
        string documentTitle)
    {
        // 문서 유형별 특화 프롬프트
        var documentType = ClassifyDocumentType(documentTitle, contextText);

        var promptTemplate = documentType switch
        {
            DocumentType.TechnicalDocumentation => GetTechnicalDocumentPrompt(),
            DocumentType.BlogPost => GetBlogPostPrompt(),
            DocumentType.NewsArticle => GetNewsArticlePrompt(),
            DocumentType.AcademicPaper => GetAcademicPrompt(),
            DocumentType.ProductPage => GetProductPrompt(),
            _ => GetGeneralPrompt()
        };

        return await _promptEngine.RenderAsync(promptTemplate, new
        {
            DocumentTitle = documentTitle,
            ContextText = contextText,
            ImageMetadata = $"크기: {image.Width}x{image.Height}, 형식: {image.ContentType}",
            MaxWords = 200
        });
    }

    private string GetTechnicalDocumentPrompt() => """
        # 기술 문서 이미지 분석

        다음은 기술 문서 "{DocumentTitle}"의 이미지입니다.

        ## 주변 맥락:
        {ContextText}

        ## 이미지 메타데이터:
        {ImageMetadata}

        ## 분석 요구사항:
        이 이미지를 기술 문서의 맥락에서 상세히 분석해주세요. 특히 다음 사항에 집중해주세요:

        1. **기술적 내용**: 다이어그램, 아키텍처, 플로우차트, 코드 스크린샷 등
        2. **데이터 시각화**: 표, 그래프, 차트의 구체적인 데이터와 트렌드
        3. **UI/UX 요소**: 인터페이스 스크린샷의 기능과 사용법
        4. **설정/구성**: 설정 화면이나 구성 다이어그램의 세부사항

        ## 출력 형식:
        **이미지 설명**: [이미지의 핵심 내용을 {MaxWords}단어 내외로 설명]
        **기술적 세부사항**: [기술적으로 중요한 정보]
        **맥락 연관성**: [주변 텍스트와의 연관성]

        설명은 RAG 시스템에서 검색 가능하도록 구체적이고 정확한 용어를 사용해주세요.
        """;

    private string GetBlogPostPrompt() => """
        # 블로그 포스트 이미지 분석

        다음은 "{DocumentTitle}" 블로그 포스트의 이미지입니다.

        ## 주변 맥락:
        {ContextText}

        ## 분석 요구사항:
        이 이미지를 블로그 포스트의 맥락에서 분석해주세요:

        1. **시각적 요소**: 무엇을 보여주는지 구체적으로 설명
        2. **스토리텔링**: 블로그 내용과 어떻게 연결되는지
        3. **감정/분위기**: 이미지가 전달하는 감정이나 분위기
        4. **실용적 정보**: 독자에게 유용한 구체적 정보

        {MaxWords}단어 내외로 독자가 이해하기 쉽게 설명해주세요.
        """;

    private string GetAcademicPrompt() => """
        # 학술 문서 이미지 분석

        다음은 학술 문서 "{DocumentTitle}"의 이미지입니다.

        ## 주변 맥락:
        {ContextText}

        ## 분석 요구사항:
        학술적 맥락에서 이 이미지를 분석해주세요:

        1. **연구 데이터**: 실험 결과, 통계, 측정값
        2. **방법론**: 실험 설정, 프로세스, 장비
        3. **이론적 모델**: 개념도, 수식, 프레임워크
        4. **비교 분석**: 대조군, 변화 추이, 패턴

        학술적 정확성을 중시하여 {MaxWords}단어 내외로 설명해주세요.
        """;
}
```

### 이미지 품질 및 관련성 평가

```csharp
public class ImageQualityAssessment
{
    public ImageQualityScore AssessImage(ProcessedImage image, string contextText)
    {
        var score = new ImageQualityScore();

        // 1. 기술적 품질 평가
        score.TechnicalQuality = AssessTechnicalQuality(image);

        // 2. 콘텐츠 관련성 평가
        score.ContentRelevance = AssessContentRelevance(image, contextText);

        // 3. 정보 밀도 평가
        score.InformationDensity = AssessInformationDensity(image);

        // 4. 처리 복잡도 평가
        score.ProcessingComplexity = AssessProcessingComplexity(image);

        return score;
    }

    private double AssessTechnicalQuality(ProcessedImage image)
    {
        var score = 1.0;

        // 해상도 체크
        if (image.Width < 100 || image.Height < 100)
            score *= 0.3; // 너무 작은 이미지

        if (image.Width > 4000 || image.Height > 4000)
            score *= 0.8; // 너무 큰 이미지는 처리 비용 증가

        // 파일 크기 체크
        if (image.Size < 1024) // 1KB 미만
            score *= 0.2; // 의미 없는 작은 이미지일 가능성

        if (image.Size > 5 * 1024 * 1024) // 5MB 초과
            score *= 0.7; // 처리 비용 증가

        // 형식 평가
        if (image.ContentType.Contains("svg"))
            score *= 1.2; // SVG는 벡터 정보 포함으로 유리

        return Math.Max(0.1, Math.Min(1.0, score));
    }

    private double AssessContentRelevance(ProcessedImage image, string contextText)
    {
        // 이미지 파일명에서 관련성 추출
        var fileName = Path.GetFileNameWithoutExtension(image.Url).ToLower();
        var contextWords = contextText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var relevanceScore = 0.0;

        // 키워드 매칭
        var relevantKeywords = new[] { "diagram", "chart", "graph", "screenshot", "example", "figure" };
        foreach (var keyword in relevantKeywords)
        {
            if (fileName.Contains(keyword) || contextText.ToLower().Contains(keyword))
                relevanceScore += 0.2;
        }

        // 맥락 단어와 파일명 매칭
        foreach (var word in contextWords.Where(w => w.Length > 4))
        {
            if (fileName.Contains(word))
                relevanceScore += 0.1;
        }

        return Math.Min(1.0, relevanceScore);
    }
}

public class ImageQualityScore
{
    public double TechnicalQuality { get; set; }
    public double ContentRelevance { get; set; }
    public double InformationDensity { get; set; }
    public double ProcessingComplexity { get; set; }

    public double OverallScore =>
        (TechnicalQuality * 0.3 + ContentRelevance * 0.4 +
         InformationDensity * 0.2 + ProcessingComplexity * 0.1);

    public bool ShouldProcess(double threshold = 0.5) => OverallScore >= threshold;
}
```

## 🔗 맥락적 병합 (Contextual Merging)

### 이미지 설명과 텍스트의 통합

```csharp
public class ContextualMerger : IContextualMerger
{
    private readonly ILogger<ContextualMerger> _logger;

    public async Task<ParsedWebContent> MergeAsync(
        ParsedWebContent textContent,
        List<ImageDescription> imageDescriptions,
        MultimodalOptions options)
    {
        var enhancedContent = textContent.DeepClone();

        // 1. 이미지 설명을 적절한 위치에 삽입
        foreach (var imageDesc in imageDescriptions)
        {
            var insertionPoints = FindOptimalInsertionPoints(imageDesc, enhancedContent);

            foreach (var point in insertionPoints)
            {
                await InsertImageDescription(imageDesc, point, enhancedContent, options.MergingOptions);
            }
        }

        // 2. 메타데이터 업데이트
        UpdateMetadataWithImageInfo(enhancedContent, imageDescriptions);

        // 3. 구조 정보 재계산
        enhancedContent.Structure = RecalculateStructure(enhancedContent);

        return enhancedContent;
    }

    private List<InsertionPoint> FindOptimalInsertionPoints(
        ImageDescription imageDesc,
        ParsedWebContent content)
    {
        var insertionPoints = new List<InsertionPoint>();

        // 1. 직접 참조 찾기
        var directReferences = FindDirectImageReferences(imageDesc.ImageUrl, content.MainContent);
        insertionPoints.AddRange(directReferences);

        // 2. 맥락적 위치 찾기
        if (!directReferences.Any())
        {
            var contextualPoints = FindContextualInsertionPoints(imageDesc, content);
            insertionPoints.AddRange(contextualPoints);
        }

        // 3. 섹션 기반 위치 찾기
        if (!insertionPoints.Any())
        {
            var sectionPoints = FindSectionBasedInsertionPoints(imageDesc, content);
            insertionPoints.AddRange(sectionPoints);
        }

        return insertionPoints.OrderBy(p => p.Priority).ToList();
    }

    private async Task InsertImageDescription(
        ImageDescription imageDesc,
        InsertionPoint point,
        ParsedWebContent content,
        ContextualMergingOptions options)
    {
        var formattedDescription = FormatImageDescription(imageDesc, options);

        switch (point.Type)
        {
            case InsertionType.InlineReference:
                // "이미지 참조: [설명]" 형태로 삽입
                content.MainContent = content.MainContent.Insert(point.Position,
                    $"\n\n**이미지**: {formattedDescription}\n\n");
                break;

            case InsertionType.SectionEnd:
                // 섹션 끝에 이미지 정보 블록 추가
                var section = content.Sections.FirstOrDefault(s => s.Id == point.SectionId);
                if (section != null)
                {
                    section.Content += $"\n\n### 관련 이미지\n{formattedDescription}";
                }
                break;

            case InsertionType.Contextual:
                // 관련 문단 근처에 맥락 정보로 삽입
                content.MainContent = content.MainContent.Insert(point.Position,
                    $"\n\n*[이미지 설명: {formattedDescription}]*\n\n");
                break;
        }
    }

    private string FormatImageDescription(
        ImageDescription imageDesc,
        ContextualMergingOptions options)
    {
        var formatted = new StringBuilder();

        // 기본 설명
        formatted.Append(imageDesc.Description);

        // 추가 정보 포함 옵션
        if (options.IncludeImageMetadata)
        {
            formatted.Append($" (이미지 출처: {imageDesc.ImageUrl})");
        }

        if (options.IncludeConfidenceScore && imageDesc.Confidence < 0.8)
        {
            formatted.Append($" [신뢰도: {imageDesc.Confidence:P0}]");
        }

        return formatted.ToString();
    }
}

public class InsertionPoint
{
    public InsertionType Type { get; set; }
    public int Position { get; set; }
    public string? SectionId { get; set; }
    public int Priority { get; set; } // 낮을수록 우선
    public double RelevanceScore { get; set; }
}

public enum InsertionType
{
    InlineReference,    // 직접 이미지 참조 위치
    SectionEnd,         // 섹션 끝
    Contextual,         // 맥락적 연관 위치
    Standalone          // 독립적 이미지 블록
}

public class ContextualMergingOptions
{
    public bool IncludeImageMetadata { get; set; } = false;
    public bool IncludeConfidenceScore { get; set; } = false;
    public InsertionStyle InsertionStyle { get; set; } = InsertionStyle.Contextual;
    public int MaxDescriptionLength { get; set; } = 500;
    public bool PreserveOriginalStructure { get; set; } = true;
}
```

## 📊 표 데이터 처리

### 표 추출 및 선형화

```csharp
public class TableProcessor : ITableProcessor
{
    public async Task<List<TableData>> ProcessTablesAsync(
        ParsedWebContent content,
        TableProcessingOptions options)
    {
        var tables = new List<TableData>();

        // HTML 표 추출
        var htmlTables = ExtractHtmlTables(content.MainContent);
        tables.AddRange(htmlTables);

        // 마크다운 표 추출
        var markdownTables = ExtractMarkdownTables(content.MainContent);
        tables.AddRange(markdownTables);

        // 각 표 선형화
        foreach (var table in tables)
        {
            table.LinearizedText = await LinearizeTable(table, options);
        }

        return tables;
    }

    private async Task<string> LinearizeTable(TableData table, TableProcessingOptions options)
    {
        return options.LinearizationStrategy switch
        {
            TableLinearizationStrategy.Markdown => LinearizeAsMarkdown(table),
            TableLinearizationStrategy.CSV => LinearizeAsCsv(table),
            TableLinearizationStrategy.NaturalLanguage => await LinearizeAsNaturalLanguage(table),
            TableLinearizationStrategy.Structured => LinearizeAsStructured(table),
            _ => LinearizeAsMarkdown(table)
        };
    }

    private string LinearizeAsMarkdown(TableData table)
    {
        var result = new StringBuilder();

        if (!string.IsNullOrEmpty(table.Caption))
        {
            result.AppendLine($"**표: {table.Caption}**");
            result.AppendLine();
        }

        // 헤더
        if (table.Headers.Any())
        {
            result.AppendLine("| " + string.Join(" | ", table.Headers) + " |");
            result.AppendLine("| " + string.Join(" | ", table.Headers.Select(_ => "---")) + " |");
        }

        // 데이터 행
        foreach (var row in table.Rows)
        {
            result.AppendLine("| " + string.Join(" | ", row.Cells) + " |");
        }

        return result.ToString();
    }

    private async Task<string> LinearizeAsNaturalLanguage(TableData table)
    {
        var prompt = $"""
            다음 표 데이터를 자연스러운 문장으로 변환해주세요:

            표 제목: {table.Caption}
            헤더: {string.Join(", ", table.Headers)}

            데이터:
            {string.Join("\n", table.Rows.Select(r => string.Join(" | ", r.Cells)))}

            요구사항:
            1. 표의 핵심 정보를 문장으로 설명
            2. 중요한 데이터 포인트 강조
            3. 패턴이나 트렌드가 있다면 언급
            4. RAG 검색에 최적화된 형태로 작성

            500단어 이내로 작성해주세요.
            """;

        return await _llmService.CompleteAsync(prompt, new TextCompletionOptions
        {
            MaxTokens = 600,
            Temperature = 0.3f
        });
    }
}

public class TableData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Caption { get; set; }
    public List<string> Headers { get; set; } = new();
    public List<TableRow> Rows { get; set; } = new();
    public int ColumnCount { get; set; }
    public int RowCount { get; set; }
    public string? LinearizedText { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class TableRow
{
    public List<string> Cells { get; set; } = new();
    public bool IsHeader { get; set; }
}
```

## 🎨 이미지 분류 및 처리 최적화

### 이미지 유형별 특화 처리

```csharp
public class ImageClassifier
{
    public ImageType ClassifyImage(ProcessedImage image, string contextText)
    {
        // 1. URL 패턴 분석
        var urlKeywords = ExtractUrlKeywords(image.Url);

        // 2. 맥락 분석
        var contextKeywords = ExtractContextKeywords(contextText);

        // 3. 이미지 메타데이터 분석
        var aspectRatio = (double)image.Width / image.Height;

        // 분류 로직
        if (urlKeywords.Any(k => k.Contains("chart") || k.Contains("graph")))
            return ImageType.Chart;

        if (urlKeywords.Any(k => k.Contains("screenshot") || k.Contains("ui")))
            return ImageType.Screenshot;

        if (urlKeywords.Any(k => k.Contains("diagram") || k.Contains("flow")))
            return ImageType.Diagram;

        if (aspectRatio > 2.0 || aspectRatio < 0.5)
            return ImageType.Banner;

        if (contextKeywords.Any(k => k.Contains("photo") || k.Contains("picture")))
            return ImageType.Photograph;

        return ImageType.General;
    }

    public ImageProcessingStrategy GetOptimalStrategy(ImageType imageType)
    {
        return imageType switch
        {
            ImageType.Chart => new ChartProcessingStrategy(),
            ImageType.Screenshot => new ScreenshotProcessingStrategy(),
            ImageType.Diagram => new DiagramProcessingStrategy(),
            ImageType.Photograph => new PhotographProcessingStrategy(),
            ImageType.Banner => new BannerProcessingStrategy(),
            _ => new GeneralImageProcessingStrategy()
        };
    }
}

public enum ImageType
{
    Chart,          // 차트, 그래프
    Screenshot,     // UI 스크린샷
    Diagram,        // 다이어그램, 플로우차트
    Photograph,     // 일반 사진
    Banner,         // 배너, 로고
    Icon,           // 아이콘
    General         // 기타
}

public abstract class ImageProcessingStrategy
{
    public abstract Task<ImageToTextResult> ProcessAsync(
        ProcessedImage image,
        string contextText,
        IImageToTextService imageToTextService);

    protected virtual ImageToTextOptions CreateOptions(ProcessedImage image, string contextText)
    {
        return new ImageToTextOptions
        {
            Language = "ko",
            DetailLevel = "Detailed",
            ContextPrompt = contextText
        };
    }
}

public class ChartProcessingStrategy : ImageProcessingStrategy
{
    public override async Task<ImageToTextResult> ProcessAsync(
        ProcessedImage image,
        string contextText,
        IImageToTextService imageToTextService)
    {
        var options = CreateOptions(image, contextText);
        options.ExtractionType = "DataVisualization";
        options.ContextPrompt = $"""
            이것은 차트나 그래프 이미지입니다. 다음 정보를 상세히 추출해주세요:
            1. 차트 유형 (막대, 선, 원형, 산점도 등)
            2. 축 레이블과 단위
            3. 데이터 시리즈와 범례
            4. 주요 데이터 포인트와 수치
            5. 트렌드나 패턴

            맥락: {contextText}
            """;

        return await imageToTextService.ExtractTextFromImageDataAsync(
            image.Data, image.ContentType, options);
    }
}

public class DiagramProcessingStrategy : ImageProcessingStrategy
{
    public override async Task<ImageToTextResult> ProcessAsync(
        ProcessedImage image,
        string contextText,
        IImageToTextService imageToTextService)
    {
        var options = CreateOptions(image, contextText);
        options.ExtractionType = "TechnicalDiagram";
        options.ContextPrompt = $"""
            이것은 기술적 다이어그램입니다. 다음을 중점적으로 분석해주세요:
            1. 다이어그램 유형 (플로우차트, 아키텍처, 네트워크 등)
            2. 주요 구성 요소와 노드
            3. 연결 관계와 데이터 플로우
            4. 프로세스나 시스템의 흐름
            5. 주요 결정점이나 분기점

            맥락: {contextText}
            """;

        return await imageToTextService.ExtractTextFromImageDataAsync(
            image.Data, image.ContentType, options);
    }
}
```

## 🔄 멀티모달 청킹 전략

### 이미지 정보를 포함한 청킹

```csharp
public class MultimodalChunkingStrategy : IChunkingStrategy
{
    private readonly IChunkingStrategy _baseStrategy;
    private readonly ILogger<MultimodalChunkingStrategy> _logger;

    public string StrategyName => $"Multimodal{_baseStrategy.StrategyName}";

    public async Task<IEnumerable<WebContentChunk>> ChunkAsync(
        ParsedWebContent content,
        ChunkingOptions options,
        CancellationToken cancellationToken = default)
    {
        // 1. 기본 청킹 수행
        var textChunks = await _baseStrategy.ChunkAsync(content, options, cancellationToken);

        // 2. 이미지 정보가 포함된 청크 식별
        var enhancedChunks = new List<WebContentChunk>();

        foreach (var chunk in textChunks)
        {
            var enhancedChunk = await EnhanceChunkWithImageInfo(chunk, content);
            enhancedChunks.Add(enhancedChunk);
        }

        // 3. 독립적인 이미지 청크 생성 (텍스트 연관성이 낮은 경우)
        var standaloneImageChunks = CreateStandaloneImageChunks(content, enhancedChunks);
        enhancedChunks.AddRange(standaloneImageChunks);

        return enhancedChunks;
    }

    private async Task<WebContentChunk> EnhanceChunkWithImageInfo(
        WebContentChunk textChunk,
        ParsedWebContent content)
    {
        var enhancedChunk = textChunk.DeepClone();

        // 청크 내 이미지 참조 찾기
        var imageReferences = FindImageReferences(textChunk, content.Images);

        if (imageReferences.Any())
        {
            // 이미지 정보를 메타데이터에 추가
            enhancedChunk.Metadata.Properties["HasImages"] = true;
            enhancedChunk.Metadata.Properties["ImageCount"] = imageReferences.Count;
            enhancedChunk.Metadata.Properties["ImageDescriptions"] =
                imageReferences.Select(img => new
                {
                    Url = img.Url,
                    Description = img.Description,
                    Type = img.ImageType
                }).ToList();

            // 청킹 전략 정보 업데이트
            enhancedChunk.ChunkingStrategy = StrategyName;
            enhancedChunk.ConfidenceScore *= 1.1; // 멀티모달 정보로 인한 품질 향상
        }

        return enhancedChunk;
    }

    private List<WebContentChunk> CreateStandaloneImageChunks(
        ParsedWebContent content,
        List<WebContentChunk> textChunks)
    {
        var standaloneChunks = new List<WebContentChunk>();

        // 텍스트 청크에 포함되지 않은 이미지들
        var unattachedImages = content.Images.Where(img =>
            !textChunks.Any(chunk => ContainsImageReference(chunk, img))).ToList();

        foreach (var image in unattachedImages)
        {
            var imageChunk = new WebContentChunk
            {
                Content = $"이미지: {image.Description}",
                SourceUrl = content.Url,
                ChunkingStrategy = StrategyName,
                Metadata = content.Metadata.Clone()
            };

            imageChunk.Metadata.Properties["IsImageOnlyChunk"] = true;
            imageChunk.Metadata.Properties["ImageUrl"] = image.Url;
            imageChunk.Metadata.Properties["ImageType"] = image.ImageType;

            standaloneChunks.Add(imageChunk);
        }

        return standaloneChunks;
    }
}
```

## 📈 성능 최적화 및 캐싱

### 이미지 처리 캐싱

```csharp
public class ImageProcessingCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ImageCacheOptions _options;

    public async Task<ImageToTextResult?> GetCachedResultAsync(string imageUrl, string promptHash)
    {
        var cacheKey = GenerateCacheKey(imageUrl, promptHash);

        // 1. 메모리 캐시 확인
        if (_memoryCache.TryGetValue(cacheKey, out ImageToTextResult? memoryResult))
        {
            return memoryResult;
        }

        // 2. 분산 캐시 확인
        var distributedResult = await _distributedCache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(distributedResult))
        {
            var result = JsonSerializer.Deserialize<ImageToTextResult>(distributedResult);

            // 메모리 캐시에도 저장
            _memoryCache.Set(cacheKey, result, _options.MemoryCacheDuration);

            return result;
        }

        return null;
    }

    public async Task CacheResultAsync(
        string imageUrl,
        string promptHash,
        ImageToTextResult result)
    {
        var cacheKey = GenerateCacheKey(imageUrl, promptHash);

        // 메모리 캐시 저장
        _memoryCache.Set(cacheKey, result, _options.MemoryCacheDuration);

        // 분산 캐시 저장
        var serialized = JsonSerializer.Serialize(result);
        await _distributedCache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.DistributedCacheDuration
        });
    }

    private string GenerateCacheKey(string imageUrl, string promptHash)
    {
        var combined = $"{imageUrl}:{promptHash}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return $"img_desc:{Convert.ToHexString(hash)[..16]}";
    }
}
```

## 🧪 품질 평가 및 검증

### 멀티모달 품질 메트릭

```csharp
public class MultimodalQualityEvaluator
{
    public MultimodalQualityMetrics EvaluateQuality(
        IEnumerable<WebContentChunk> chunks,
        ParsedWebContent originalContent)
    {
        var metrics = new MultimodalQualityMetrics();

        // 1. 이미지 커버리지 평가
        metrics.ImageCoverage = EvaluateImageCoverage(chunks, originalContent);

        // 2. 맥락적 일관성 평가
        metrics.ContextualConsistency = EvaluateContextualConsistency(chunks);

        // 3. 설명 품질 평가
        metrics.DescriptionQuality = EvaluateDescriptionQuality(chunks);

        // 4. 검색 친화성 평가
        metrics.SearchFriendliness = EvaluateSearchFriendliness(chunks);

        return metrics;
    }

    private double EvaluateImageCoverage(
        IEnumerable<WebContentChunk> chunks,
        ParsedWebContent originalContent)
    {
        if (!originalContent.Images.Any())
            return 1.0; // 이미지가 없으면 완벽한 커버리지

        var totalImages = originalContent.Images.Count;
        var coveredImages = chunks.Count(c =>
            c.Metadata.Properties.ContainsKey("HasImages") ||
            c.Metadata.Properties.ContainsKey("IsImageOnlyChunk"));

        return (double)coveredImages / totalImages;
    }

    private double EvaluateContextualConsistency(IEnumerable<WebContentChunk> chunks)
    {
        var consistencyScores = new List<double>();

        foreach (var chunk in chunks.Where(c => c.Metadata.Properties.ContainsKey("HasImages")))
        {
            // 이미지 설명과 텍스트 내용의 일관성 평가
            var imageDescriptions = chunk.Metadata.Properties["ImageDescriptions"] as List<object>;
            if (imageDescriptions?.Any() == true)
            {
                var score = EvaluateChunkConsistency(chunk.Content, imageDescriptions);
                consistencyScores.Add(score);
            }
        }

        return consistencyScores.Any() ? consistencyScores.Average() : 1.0;
    }
}

public class MultimodalQualityMetrics
{
    public double ImageCoverage { get; set; }
    public double ContextualConsistency { get; set; }
    public double DescriptionQuality { get; set; }
    public double SearchFriendliness { get; set; }

    public double OverallQuality =>
        (ImageCoverage * 0.25 + ContextualConsistency * 0.35 +
         DescriptionQuality * 0.25 + SearchFriendliness * 0.15);
}
```

---

이 멀티모달 설계는 연구 문서의 **텍스트 기반화** 접근법을 실제 구현 가능한 형태로 구체화했으며, 실용적이면서도 확장 가능한 아키텍처를 제공합니다. 특히 프롬프트 엔지니어링을 통한 고품질 이미지 설명 생성에 중점을 두어, RAG 시스템의 성능을 극대화할 수 있도록 설계되었습니다.