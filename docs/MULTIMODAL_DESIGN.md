# Multimodal 처리 설계 (Phase 3 계획)

> 이미지-텍스트 변환을 통한 멀티모달 RAG 지원

## 개요

**구현 상태**: ❌ 미구현 (Phase 3 계획)

WebFlux는 향후 멀티모달 콘텐츠 처리를 지원하여 이미지가 포함된 웹 페이지를 완전하게 처리할 예정입니다.

## 계획된 기능

### 이미지 처리 전략

**Text-Grounding 접근**:
- 이미지 → 텍스트 설명 변환
- 텍스트 기반 RAG 시스템과 호환
- 기존 파이프라인에 자연스럽게 통합

**처리 흐름**:
```
이미지 URL → IImageToTextService → 텍스트 설명 → 원본 텍스트와 병합 → 청킹
```

### 인터페이스 설계

```csharp
public interface IImageToTextService
{
    /// <summary>
    /// 웹 이미지에서 텍스트 설명을 추출합니다.
    /// </summary>
    Task<ImageToTextResult> ExtractTextFromWebImageAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 이미지 바이트 데이터에서 텍스트 설명을 추출합니다.
    /// </summary>
    Task<ImageToTextResult> ExtractTextFromImageDataAsync(
        byte[] imageData,
        string contentType,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지원하는 이미지 형식을 반환합니다.
    /// </summary>
    IEnumerable<string> GetSupportedImageFormats();
}

public class ImageToTextResult
{
    public string ExtractedText { get; set; }      // 이미지 설명
    public double Confidence { get; set; }         // 신뢰도 (0-1)
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public class ImageToTextOptions
{
    public string ExtractionType { get; set; } = "Description";  // OCR, Description, Detailed
    public string Language { get; set; } = "en";
    public string DetailLevel { get; set; } = "Detailed";
    public string? ContextPrompt { get; set; }
    public int MaxTextLength { get; set; } = 1000;
}
```

### 통합 방식

**AI Enhancement 단계에서 처리**:
```csharp
// Phase 3: Extraction 단계에서 이미지 URL 수집
var extractedContent = await extractor.ExtractAsync(html);
var imageUrls = extractedContent.ImageUrls;

// AI Enhancement에서 이미지 설명 생성
if (imageToTextService != null && imageUrls.Count > 0)
{
    foreach (var imageUrl in imageUrls)
    {
        var imageResult = await imageToTextService.ExtractTextFromWebImageAsync(imageUrl);
        if (imageResult.IsSuccess)
        {
            // 이미지 설명을 원본 텍스트에 추가
            extractedContent.Text += $"\n\n[Image: {imageResult.ExtractedText}]";
        }
    }
}
```

### 지원 예정 AI 모델

- **GPT-4V / GPT-4o**: OpenAI 멀티모달 모델
- **Claude 3**: Anthropic 멀티모달 모델
- **LLaVA**: 오픈소스 멀티모달 모델
- **BLIP-2**: 이미지 캡셔닝 모델

## 구현 계획

### Phase 3 목표

1. **IImageToTextService 인터페이스 구현**
2. **AI Enhancement 파이프라인에 이미지 처리 통합**
3. **이미지-텍스트 병합 전략 구현**
4. **품질 메트릭 및 신뢰도 평가**

### 예상 사용 예제

```csharp
// Phase 3 구현 예정
services.AddWebFlux(config =>
{
    config.Multimodal.Enabled = true;
    config.Multimodal.ProcessImages = true;
    config.Multimodal.ImageDetailLevel = "Detailed";
});

// 이미지-텍스트 서비스 등록 (소비자가 구현)
services.AddSingleton<IImageToTextService>(sp =>
    new Gpt4VisionImageToTextService(apiKey));

// 파이프라인 실행 시 자동으로 이미지 처리
var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);
```

## 참고 문서

- [INTERFACES.md](./INTERFACES.md) - 인터페이스 설계
- [PIPELINE_DESIGN.md](./PIPELINE_DESIGN.md) - 파이프라인 통합
