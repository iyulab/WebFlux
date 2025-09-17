using Microsoft.Playwright;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// Playwright 기반 동적 콘텐츠 추출기
/// JavaScript가 렌더링된 후의 완전한 DOM에서 구조화된 텍스트와 메타데이터 추출
/// Phase 5A.1: 멀티모달 처리를 위한 이미지 추출 및 스크린샷 기능 지원
/// </summary>
public class PlaywrightContentExtractor : IContentExtractor
{
    private readonly IPlaywright _playwright;
    private readonly ILogger<PlaywrightContentExtractor> _logger;
    private readonly IImageToTextService? _imageToTextService;
    private readonly ExtractionStatistics _statistics = new();

    public PlaywrightContentExtractor(
        IPlaywright playwright,
        ILogger<PlaywrightContentExtractor> logger,
        IImageToTextService? imageToTextService = null)
    {
        _playwright = playwright ?? throw new ArgumentNullException(nameof(playwright));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageToTextService = imageToTextService;
    }

    /// <summary>
    /// HTML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    public async Task<ExtractedContent> ExtractFromHtmlAsync(
        string htmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        return await ExtractWithPlaywrightAsync(htmlContent, sourceUrl, cancellationToken);
    }

    /// <summary>
    /// 마크다운 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    public async Task<ExtractedContent> ExtractFromMarkdownAsync(
        string markdownContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        // 마크다운은 그대로 텍스트로 처리
        var metadata = new ExtractedMetadata
        {
            SourceUrl = sourceUrl,
            ContentType = "text/markdown",
            ExtractedAt = DateTimeOffset.UtcNow
        };

        return new ExtractedContent
        {
            Text = markdownContent,
            Metadata = metadata,
            WordCount = markdownContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CharacterCount = markdownContent.Length
        };
    }

    /// <summary>
    /// JSON 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    public async Task<ExtractedContent> ExtractFromJsonAsync(
        string jsonContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        // JSON은 텍스트로 변환
        var text = JsonToText(jsonContent);
        var metadata = new ExtractedMetadata
        {
            SourceUrl = sourceUrl,
            ContentType = "application/json",
            ExtractedAt = DateTimeOffset.UtcNow
        };

        return new ExtractedContent
        {
            Text = text,
            Metadata = metadata,
            WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CharacterCount = text.Length
        };
    }

    /// <summary>
    /// XML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    public async Task<ExtractedContent> ExtractFromXmlAsync(
        string xmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        // XML을 HTML처럼 처리 (태그 제거)
        var text = ExtractTextFromMarkup(xmlContent);
        var metadata = new ExtractedMetadata
        {
            SourceUrl = sourceUrl,
            ContentType = "text/xml",
            ExtractedAt = DateTimeOffset.UtcNow
        };

        return new ExtractedContent
        {
            Text = text,
            Metadata = metadata,
            WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CharacterCount = text.Length
        };
    }

    /// <summary>
    /// 일반 텍스트 콘텐츠를 처리합니다.
    /// </summary>
    public async Task<ExtractedContent> ExtractFromTextAsync(
        string textContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        var metadata = new ExtractedMetadata
        {
            SourceUrl = sourceUrl,
            ContentType = "text/plain",
            ExtractedAt = DateTimeOffset.UtcNow
        };

        return new ExtractedContent
        {
            Text = textContent,
            Metadata = metadata,
            WordCount = textContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CharacterCount = textContent.Length
        };
    }

    /// <summary>
    /// 콘텐츠 유형을 자동으로 감지하여 추출합니다.
    /// </summary>
    public async Task<ExtractedContent> ExtractAutoAsync(
        string content,
        string sourceUrl,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        // 콘텐츠 타입 감지
        var detectedType = DetectContentType(content, contentType);

        return detectedType switch
        {
            "text/html" => await ExtractFromHtmlAsync(content, sourceUrl, cancellationToken),
            "text/markdown" => await ExtractFromMarkdownAsync(content, sourceUrl, cancellationToken),
            "application/json" => await ExtractFromJsonAsync(content, sourceUrl, cancellationToken),
            "text/xml" => await ExtractFromXmlAsync(content, sourceUrl, cancellationToken),
            _ => await ExtractFromTextAsync(content, sourceUrl, cancellationToken)
        };
    }

    /// <summary>
    /// 지원하는 콘텐츠 타입 목록을 반환합니다.
    /// </summary>
    public IReadOnlyList<string> GetSupportedContentTypes()
    {
        return new[]
        {
            "text/html",
            "text/markdown",
            "application/json",
            "text/xml",
            "text/plain"
        };
    }

    /// <summary>
    /// 추출 통계를 반환합니다.
    /// </summary>
    public ExtractionStatistics GetStatistics()
    {
        return _statistics;
    }

    #region Private Methods

    /// <summary>
    /// Playwright를 사용한 동적 콘텐츠 추출
    /// </summary>
    private async Task<ExtractedContent> ExtractWithPlaywrightAsync(
        string content,
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        IBrowser? browser = null;
        IPage? page = null;

        try
        {
            // 브라우저 시작
            browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            });

            page = await browser.NewPageAsync();

            // 페이지 설정
            await page.SetViewportSizeAsync(1920, 1080);
            await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            });

            // URL 또는 HTML 콘텐츠 로드
            if (Uri.IsWellFormedUriString(sourceUrl, UriKind.Absolute))
            {
                // URL로 직접 이동
                await page.GotoAsync(sourceUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });
            }
            else
            {
                // HTML 콘텐츠를 data URL로 로드
                var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
                await page.GotoAsync($"data:text/html;base64,{base64Html}");
            }

            // JavaScript 실행 완료까지 추가 대기
            await page.WaitForTimeoutAsync(3000);

            // 동적 요소들 감지 및 대기
            await WaitForDynamicContent(page);

            // 메타데이터 추출
            var metadata = await ExtractMetadataFromPage(page, sourceUrl);

            // Phase 5A.1: 이미지 추출 및 처리
            var images = await ExtractImagesFromPage(page, sourceUrl);
            var imageTexts = new List<string>();

            if (_imageToTextService != null && images.Count > 0)
            {
                _logger.LogInformation("Extracting text from {ImageCount} images", images.Count);

                foreach (var image in images.Take(10)) // 최대 10개 이미지만 처리
                {
                    try
                    {
                        var imageText = await _imageToTextService.ExtractTextFromWebImageAsync(
                            image.Url,
                            new ImageToTextOptions { IncludeContext = true },
                            cancellationToken);

                        if (imageText.IsSuccess && !string.IsNullOrWhiteSpace(imageText.ExtractedText))
                        {
                            imageTexts.Add($"[Image: {imageText.ExtractedText}]");
                            _logger.LogDebug("Extracted text from image {ImageUrl}: {Text}",
                                image.Url, imageText.ExtractedText.Substring(0, Math.Min(100, imageText.ExtractedText.Length)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text from image {ImageUrl}", image.Url);
                    }
                }
            }

            // 텍스트 추출
            var visibleText = await page.InnerTextAsync("body");
            var cleanedText = CleanExtractedText(visibleText);

            // 이미지 텍스트를 메인 텍스트와 통합
            var combinedText = cleanedText;
            if (imageTexts.Count > 0)
            {
                combinedText += "\n\n" + string.Join("\n", imageTexts);
                metadata.OriginalMetadata["images_processed"] = imageTexts.Count;
                metadata.OriginalMetadata["total_images_found"] = images.Count;
            }

            _statistics.TotalExtractions++;

            return new ExtractedContent
            {
                Text = combinedText,
                Metadata = metadata,
                WordCount = combinedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                CharacterCount = combinedText.Length,
                Images = images,
                OriginalUrl = sourceUrl,
                ExtractionTimestamp = DateTimeOffset.UtcNow,
                ExtractionMethod = "Playwright + MLLM",
                ProcessingTimeMs = (int)(DateTimeOffset.UtcNow - metadata.ExtractedAt).TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _statistics.ErrorCount++;

            // 폴백: HTML 문자열 직접 처리
            var fallbackText = ExtractTextFromMarkup(content);
            var fallbackMetadata = new ExtractedMetadata
            {
                SourceUrl = sourceUrl,
                ContentType = "text/html",
                ExtractedAt = DateTimeOffset.UtcNow,
                ExtractionErrors = new List<string> { ex.Message }
            };

            return new ExtractedContent
            {
                Text = fallbackText,
                Metadata = fallbackMetadata,
                WordCount = fallbackText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                CharacterCount = fallbackText.Length
            };
        }
        finally
        {
            if (page != null) await page.CloseAsync();
            if (browser != null) await browser.CloseAsync();
        }
    }

    /// <summary>
    /// 페이지에서 메타데이터 추출
    /// </summary>
    private async Task<ExtractedMetadata> ExtractMetadataFromPage(IPage page, string sourceUrl)
    {
        var metadata = new ExtractedMetadata
        {
            SourceUrl = sourceUrl,
            ContentType = "text/html",
            ExtractedAt = DateTimeOffset.UtcNow,
            Keywords = new List<string>(),
            OriginalMetadata = new Dictionary<string, object>()
        };

        try
        {
            // 제목 추출
            metadata.Title = await page.TitleAsync();

            // 메타 태그에서 description 추출
            var descElement = await page.QuerySelectorAsync("meta[name='description']");
            if (descElement != null)
            {
                metadata.Description = await descElement.GetAttributeAsync("content") ?? "";
            }

            // 메타 태그에서 keywords 추출
            var keywordsElement = await page.QuerySelectorAsync("meta[name='keywords']");
            if (keywordsElement != null)
            {
                var keywords = await keywordsElement.GetAttributeAsync("content");
                if (!string.IsNullOrEmpty(keywords))
                {
                    metadata.Keywords.AddRange(keywords.Split(',').Select(k => k.Trim()));
                }
            }

            // 헤딩 구조 추출
            await ExtractHeadingStructure(page, metadata);
        }
        catch
        {
            // 메타데이터 추출 실패 시 무시
        }

        return metadata;
    }

    /// <summary>
    /// 페이지에서 이미지 정보 추출 (Phase 5A.1: 멀티모달 처리)
    /// </summary>
    private async Task<List<ImageInfo>> ExtractImagesFromPage(IPage page, string sourceUrl)
    {
        var images = new List<ImageInfo>();

        try
        {
            _logger.LogDebug("Extracting images from page: {Url}", sourceUrl);

            var imageElements = await page.QuerySelectorAllAsync("img[src]");
            var position = 0;

            foreach (var imgElement in imageElements.Take(20)) // 최대 20개 이미지만 처리
            {
                try
                {
                    var src = await imgElement.GetAttributeAsync("src");
                    var alt = await imgElement.GetAttributeAsync("alt");
                    var title = await imgElement.GetAttributeAsync("title");

                    if (string.IsNullOrWhiteSpace(src)) continue;

                    // 상대 URL을 절대 URL로 변환
                    var imageUrl = src;
                    if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                    {
                        if (Uri.TryCreate(new Uri(sourceUrl), src, out var absoluteUri))
                        {
                            imageUrl = absoluteUri.ToString();
                        }
                        else
                        {
                            continue; // 유효하지 않은 URL 건너뛰기
                        }
                    }

                    // 이미지 크기 및 형식 감지
                    var boundingBox = await imgElement.BoundingBoxAsync();
                    var dimensions = boundingBox != null
                        ? $"{(int)boundingBox.Width}x{(int)boundingBox.Height}"
                        : null;

                    // 이미지 주변 컨텍스트 추출
                    var context = await ExtractImageContext(imgElement, page);

                    var imageInfo = new ImageInfo
                    {
                        Url = imageUrl,
                        AltText = alt,
                        Title = title,
                        Dimensions = dimensions,
                        Position = position++,
                        Context = context,
                        Format = DetermineImageFormat(imageUrl)
                    };

                    images.Add(imageInfo);
                    _logger.LogTrace("Found image: {Url} (alt: {Alt})", imageUrl, alt ?? "none");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract image information from element");
                }
            }

            _logger.LogDebug("Extracted {ImageCount} images from page", images.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract images from page");
        }

        return images;
    }

    /// <summary>
    /// 이미지 주변 컨텍스트 추출
    /// </summary>
    private async Task<string?> ExtractImageContext(IElementHandle imgElement, IPage page)
    {
        try
        {
            // 이미지의 부모 요소에서 텍스트 컨텍스트 추출
            var parent = await imgElement.EvaluateHandleAsync("el => el.parentElement");
            if (parent != null)
            {
                var contextText = await parent.InnerTextAsync();
                if (!string.IsNullOrWhiteSpace(contextText) && contextText.Length < 200)
                {
                    return contextText.Trim();
                }
            }

            // 이미지 근처의 캡션이나 figcaption 찾기
            var figcaption = await page.EvaluateAsync<string?>(
                @"(img) => {
                    const figure = img.closest('figure');
                    if (figure) {
                        const caption = figure.querySelector('figcaption');
                        if (caption) return caption.textContent;
                    }

                    const next = img.nextElementSibling;
                    if (next && (next.tagName === 'P' || next.className.includes('caption'))) {
                        return next.textContent;
                    }

                    return null;
                }",
                imgElement);

            return figcaption;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// URL에서 이미지 형식 감지
    /// </summary>
    private string? DetermineImageFormat(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl);
            var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "JPEG",
                ".png" => "PNG",
                ".gif" => "GIF",
                ".webp" => "WebP",
                ".svg" => "SVG",
                ".bmp" => "BMP",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 헤딩 구조 추출
    /// </summary>
    private async Task ExtractHeadingStructure(IPage page, ExtractedMetadata metadata)
    {
        try
        {
            var headings = await page.QuerySelectorAllAsync("h1, h2, h3, h4, h5, h6");
            if (headings.Count > 0)
            {
                var headingStructure = new List<object>();

                foreach (var heading in headings.Take(20))
                {
                    var tagName = await heading.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
                    var text = await heading.InnerTextAsync();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        headingStructure.Add(new
                        {
                            Level = int.Parse(tagName.Substring(1)),
                            Text = text.Trim()
                        });
                    }
                }

                if (headingStructure.Count > 0)
                {
                    metadata.OriginalMetadata["heading_structure"] = headingStructure;
                    metadata.OriginalMetadata["heading_count"] = headingStructure.Count;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// 동적 콘텐츠 로딩 대기
    /// </summary>
    private async Task WaitForDynamicContent(IPage page)
    {
        var dynamicSelectors = new[]
        {
            "[data-loading]", "[data-lazy]", ".lazy", ".loading",
            ".skeleton", ".spinner", "[aria-busy='true']"
        };

        foreach (var selector in dynamicSelectors)
        {
            try
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                if (elements.Count > 0)
                {
                    await page.WaitForTimeoutAsync(2000);
                    break;
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 추출된 텍스트 정제
    /// </summary>
    private string CleanExtractedText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // 불필요한 공백 및 특수문자 정리
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"[\r\n]+", "\n");

        var lines = text.Split('\n');
        var cleanLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => line.Length > 10)
            .Where(line => !IsNavigationNoise(line))
            .ToList();

        return string.Join("\n", cleanLines).Trim();
    }

    /// <summary>
    /// 네비게이션 노이즈 판별
    /// </summary>
    private bool IsNavigationNoise(string line)
    {
        var noisePatterns = new[]
        {
            "cookie", "privacy", "terms", "skip to main",
            "navigation", "menu", "search", "login", "sign in"
        };

        return noisePatterns.Any(pattern =>
            line.ToLower().Contains(pattern) && line.Length < 50);
    }

    /// <summary>
    /// 마크업에서 텍스트 추출 (폴백)
    /// </summary>
    private string ExtractTextFromMarkup(string markup)
    {
        // Script, style 태그 제거
        markup = Regex.Replace(markup, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markup = Regex.Replace(markup, @"<style[^>]*?>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // HTML 태그 제거
        markup = Regex.Replace(markup, @"<[^>]+>", " ");

        // HTML 엔티티 디코딩
        markup = System.Net.WebUtility.HtmlDecode(markup);

        // 공백 정규화
        markup = Regex.Replace(markup, @"\s+", " ");

        return markup.Trim();
    }

    /// <summary>
    /// JSON을 읽을 수 있는 텍스트로 변환
    /// </summary>
    private string JsonToText(string json)
    {
        try
        {
            // JSON을 파싱하여 값들만 추출
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var sb = new StringBuilder();

            ExtractJsonValues(doc.RootElement, sb);

            return sb.ToString().Trim();
        }
        catch
        {
            // JSON 파싱 실패 시 원본 반환
            return json;
        }
    }

    /// <summary>
    /// JSON 값 추출 (재귀)
    /// </summary>
    private void ExtractJsonValues(System.Text.Json.JsonElement element, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.String:
                sb.AppendLine(element.GetString());
                break;
            case System.Text.Json.JsonValueKind.Number:
                sb.AppendLine(element.ToString());
                break;
            case System.Text.Json.JsonValueKind.True:
            case System.Text.Json.JsonValueKind.False:
                sb.AppendLine(element.ToString());
                break;
            case System.Text.Json.JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractJsonValues(item, sb);
                }
                break;
            case System.Text.Json.JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    ExtractJsonValues(prop.Value, sb);
                }
                break;
        }
    }

    /// <summary>
    /// 콘텐츠 타입 감지
    /// </summary>
    private string DetectContentType(string content, string? contentType)
    {
        if (!string.IsNullOrEmpty(contentType)) return contentType;

        var trimmed = content.Trim();

        if (trimmed.StartsWith("<") && trimmed.EndsWith(">"))
            return "text/html";

        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            return "application/json";

        if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<") && trimmed.Contains("</"))
            return "text/xml";

        if (trimmed.Contains("# ") || trimmed.Contains("## ") || trimmed.Contains("### "))
            return "text/markdown";

        return "text/plain";
    }

    #endregion
}