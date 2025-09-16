using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// Mock 이미지-텍스트 변환 서비스 (테스트용)
/// 실제 Vision AI 서비스 대신 가짜 이미지 설명 제공
/// </summary>
public class MockImageToTextService : IImageToTextService
{
    private readonly Random _random = new();
    private readonly Dictionary<string, string[]> _imageDescriptions;
    private int _requestCount = 0;
    private bool _simulateErrors = false;
    private TimeSpan _responseDelay = TimeSpan.FromMilliseconds(200);

    public MockImageToTextService()
    {
        _imageDescriptions = new Dictionary<string, string[]>
        {
            ["business"] = new[]
            {
                "A professional business meeting with people discussing charts and graphs around a conference table.",
                "Modern office environment with employees working on laptops and collaborative workspaces.",
                "Business presentation slide showing quarterly financial results and growth projections.",
                "Corporate team brainstorming session with sticky notes and whiteboards filled with strategic plans."
            },
            ["technology"] = new[]
            {
                "Modern data center with servers and networking equipment in organized racks.",
                "Software developer working on multiple monitors displaying code and development tools.",
                "Cloud computing infrastructure diagram showing distributed systems architecture.",
                "AI and machine learning workflow visualization with neural network representations."
            },
            ["web"] = new[]
            {
                "Website mockup displaying responsive design across desktop, tablet, and mobile devices.",
                "Web development environment with HTML, CSS, and JavaScript code visible on screen.",
                "User interface design wireframes and prototypes for e-commerce application.",
                "Web analytics dashboard showing traffic metrics, conversion rates, and user behavior data."
            },
            ["document"] = new[]
            {
                "Professional report document with charts, tables, and formatted text content.",
                "Contract or legal document with structured paragraphs and signature areas.",
                "Technical specification document with diagrams and detailed requirements.",
                "Marketing brochure featuring product images and promotional content layout."
            },
            ["chart"] = new[]
            {
                "Bar chart displaying sales performance across different quarters with upward trend.",
                "Pie chart showing market share distribution among top competitors in the industry.",
                "Line graph illustrating customer growth metrics over the past two years.",
                "Dashboard with multiple KPI visualizations including revenue, costs, and profit margins."
            },
            ["screenshot"] = new[]
            {
                "Application interface screenshot showing user dashboard with navigation and data tables.",
                "Mobile app screenshot displaying login form and user onboarding flow.",
                "E-commerce website screenshot featuring product catalog and shopping cart functionality.",
                "Social media platform screenshot with timeline, posts, and user interaction elements."
            },
            ["generic"] = new[]
            {
                "Image contains various visual elements including text, graphics, and structural components.",
                "Visual content with mixed media elements, layout structures, and informational graphics.",
                "Composite image featuring text overlays, charts, and design elements in organized layout.",
                "Professional content display with formatted text, visual aids, and structured information."
            }
        };
    }

    /// <summary>
    /// 이미지를 텍스트로 변환
    /// </summary>
    /// <param name="imageData">이미지 바이트 데이터</param>
    /// <param name="prompt">추가 지시사항</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>이미지 설명 텍스트</returns>
    public async Task<string> ConvertImageToTextAsync(
        byte[] imageData,
        string? prompt = null,
        CancellationToken cancellationToken = default)
    {
        _requestCount++;

        // 응답 지연 시뮬레이션
        await Task.Delay(_responseDelay, cancellationToken);

        // 오류 시뮬레이션
        if (_simulateErrors && _requestCount % 15 == 0)
        {
            throw new InvalidOperationException("Mock vision API quota exceeded (simulated error)");
        }

        if (_requestCount % 75 == 0)
        {
            throw new ArgumentException("Mock image format not supported (simulated error)");
        }

        // 이미지 타입 추정 (파일 시그니처 기반)
        var imageType = EstimateImageType(imageData);

        // 프롬프트 기반 카테고리 결정
        var category = DetermineContentCategory(prompt, imageType);
        var descriptions = _imageDescriptions[category];
        var baseDescription = descriptions[_random.Next(descriptions.Length)];

        // 이미지 메타데이터 추가
        var metadata = AnalyzeImageMetadata(imageData);
        var enhancedDescription = EnhanceDescription(baseDescription, metadata, prompt);

        return enhancedDescription;
    }

    /// <summary>
    /// 이미지 URL을 텍스트로 변환
    /// </summary>
    /// <param name="imageUrl">이미지 URL</param>
    /// <param name="prompt">추가 지시사항</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>이미지 설명 텍스트</returns>
    public async Task<string> ConvertImageToTextAsync(
        string imageUrl,
        string? prompt = null,
        CancellationToken cancellationToken = default)
    {
        _requestCount++;

        // URL 유효성 검사
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid image URL format", nameof(imageUrl));
        }

        // 응답 지연 시뮬레이션 (네트워크 다운로드 시간 포함)
        await Task.Delay(_responseDelay + TimeSpan.FromMilliseconds(300), cancellationToken);

        // URL 기반 이미지 타입 추정
        var imageType = EstimateImageTypeFromUrl(imageUrl);

        // 네트워크 오류 시뮬레이션
        if (_simulateErrors && _requestCount % 20 == 0)
        {
            throw new HttpRequestException("Mock network error: Unable to download image (simulated)");
        }

        var category = DetermineContentCategory(prompt, imageType);
        var descriptions = _imageDescriptions[category];
        var baseDescription = descriptions[_random.Next(descriptions.Length)];

        // URL 메타데이터 추가
        var urlMetadata = new ImageMetadata
        {
            Width = _random.Next(800, 1920),
            Height = _random.Next(600, 1080),
            Format = imageType,
            FileSizeBytes = _random.Next(100_000, 5_000_000),
            Source = "URL"
        };

        var enhancedDescription = EnhanceDescription(baseDescription, urlMetadata, prompt);

        return enhancedDescription;
    }

    /// <summary>
    /// 서비스 상태 확인
    /// </summary>
    /// <returns>서비스 상태 정보</returns>
    public Task<ServiceHealthInfo> GetHealthAsync()
    {
        return Task.FromResult(new ServiceHealthInfo
        {
            ServiceName = "MockImageToTextService",
            IsHealthy = !_simulateErrors || _requestCount % 7 != 0,
            ResponseTimeMs = (int)_responseDelay.TotalMilliseconds + 300, // 이미지 처리 시간 포함
            RequestCount = _requestCount,
            LastError = _simulateErrors && _requestCount % 7 == 0 ? "Simulated vision processing degradation" : null,
            AdditionalInfo = new Dictionary<string, object>
            {
                ["SimulateErrors"] = _simulateErrors,
                ["ResponseDelay"] = _responseDelay.TotalMilliseconds,
                ["SupportedFormats"] = "JPEG, PNG, GIF, WebP, BMP",
                ["MaxImageSize"] = "10MB",
                ["AvailableCategories"] = string.Join(", ", _imageDescriptions.Keys)
            }
        });
    }

    /// <summary>
    /// 오류 시뮬레이션 활성화/비활성화
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetErrorSimulation(bool enabled)
    {
        _simulateErrors = enabled;
    }

    /// <summary>
    /// 응답 지연 시간 설정
    /// </summary>
    /// <param name="delay">지연 시간</param>
    public void SetResponseDelay(TimeSpan delay)
    {
        _responseDelay = delay;
    }

    /// <summary>
    /// 요청 횟수 재설정
    /// </summary>
    public void ResetRequestCount()
    {
        _requestCount = 0;
    }

    /// <summary>
    /// 이미지 바이트 데이터에서 타입 추정
    /// </summary>
    /// <param name="imageData">이미지 데이터</param>
    /// <returns>이미지 타입</returns>
    private string EstimateImageType(byte[] imageData)
    {
        if (imageData == null || imageData.Length < 4)
            return "unknown";

        // 파일 시그니처 확인
        var signature = imageData.Take(4).ToArray();

        // JPEG: FF D8 FF
        if (signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF)
            return "JPEG";

        // PNG: 89 50 4E 47
        if (signature[0] == 0x89 && signature[1] == 0x50 && signature[2] == 0x4E && signature[3] == 0x47)
            return "PNG";

        // GIF: 47 49 46 38
        if (signature[0] == 0x47 && signature[1] == 0x49 && signature[2] == 0x46 && signature[3] == 0x38)
            return "GIF";

        return "unknown";
    }

    /// <summary>
    /// URL에서 이미지 타입 추정
    /// </summary>
    /// <param name="imageUrl">이미지 URL</param>
    /// <returns>이미지 타입</returns>
    private string EstimateImageTypeFromUrl(string imageUrl)
    {
        var extension = Path.GetExtension(imageUrl).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "JPEG",
            ".png" => "PNG",
            ".gif" => "GIF",
            ".webp" => "WebP",
            ".bmp" => "BMP",
            _ => "unknown"
        };
    }

    /// <summary>
    /// 이미지 메타데이터 분석 (시뮬레이션)
    /// </summary>
    /// <param name="imageData">이미지 데이터</param>
    /// <returns>메타데이터</returns>
    private ImageMetadata AnalyzeImageMetadata(byte[] imageData)
    {
        return new ImageMetadata
        {
            Width = _random.Next(400, 2560),
            Height = _random.Next(300, 1440),
            Format = EstimateImageType(imageData),
            FileSizeBytes = imageData.Length,
            Source = "Bytes"
        };
    }

    /// <summary>
    /// 프롬프트와 이미지 타입 기반 콘텐츠 카테고리 결정
    /// </summary>
    /// <param name="prompt">사용자 프롬프트</param>
    /// <param name="imageType">이미지 타입</param>
    /// <returns>콘텐츠 카테고리</returns>
    private string DetermineContentCategory(string? prompt, string imageType)
    {
        var text = prompt?.ToLowerInvariant() ?? "";

        if (text.Contains("business") || text.Contains("meeting") || text.Contains("office") || text.Contains("비즈니스"))
            return "business";

        if (text.Contains("technology") || text.Contains("code") || text.Contains("software") || text.Contains("기술"))
            return "technology";

        if (text.Contains("website") || text.Contains("web") || text.Contains("ui") || text.Contains("웹사이트"))
            return "web";

        if (text.Contains("chart") || text.Contains("graph") || text.Contains("data") || text.Contains("차트"))
            return "chart";

        if (text.Contains("screenshot") || text.Contains("screen") || text.Contains("스크린샷"))
            return "screenshot";

        if (text.Contains("document") || text.Contains("report") || text.Contains("문서"))
            return "document";

        return "generic";
    }

    /// <summary>
    /// 기본 설명에 메타데이터와 프롬프트 정보 추가
    /// </summary>
    /// <param name="baseDescription">기본 설명</param>
    /// <param name="metadata">이미지 메타데이터</param>
    /// <param name="prompt">사용자 프롬프트</param>
    /// <returns>향상된 설명</returns>
    private string EnhanceDescription(string baseDescription, ImageMetadata metadata, string? prompt)
    {
        var enhanced = baseDescription;

        // 메타데이터 정보 추가
        enhanced += $"\n\nImage specifications: {metadata.Width}x{metadata.Height}px, {metadata.Format} format";

        if (metadata.FileSizeBytes > 0)
        {
            var sizeKb = metadata.FileSizeBytes / 1024;
            enhanced += $", {sizeKb:N0}KB file size";
        }

        // 프롬프트 특화 정보 추가
        if (!string.IsNullOrEmpty(prompt))
        {
            if (prompt.Contains("detail", StringComparison.OrdinalIgnoreCase))
            {
                enhanced += "\n\nDetailed analysis: The image contains well-structured visual elements with clear typography, appropriate color scheme, and professional layout design.";
            }

            if (prompt.Contains("text", StringComparison.OrdinalIgnoreCase))
            {
                enhanced += "\n\nText content: Multiple text elements are visible including headings, body text, and numerical data presented in a readable format.";
            }

            if (prompt.Contains("color", StringComparison.OrdinalIgnoreCase))
            {
                var colors = new[] { "blue", "green", "red", "orange", "purple", "gray", "black", "white" };
                var randomColors = colors.OrderBy(x => _random.Next()).Take(3);
                enhanced += $"\n\nColor analysis: Predominant colors include {string.Join(", ", randomColors)} with good contrast ratios.";
            }
        }

        return enhanced;
    }

    /// <summary>
    /// 이미지 메타데이터 클래스
    /// </summary>
    private class ImageMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = string.Empty;
        public int FileSizeBytes { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}