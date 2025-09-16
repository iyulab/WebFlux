using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.RegularExpressions;
using System.Text;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 문단 기반 청킹 전략
/// 의미적 경계(문단)를 존중하여 자연스러운 청크 생성
/// </summary>
public class ParagraphChunkingStrategy : BaseChunkingStrategy
{
    public ParagraphChunkingStrategy(IEventPublisher eventPublisher) : base(eventPublisher)
    {
    }

    /// <summary>
    /// 문단 기반 청킹 수행
    /// </summary>
    /// <param name="text">분할할 텍스트</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>청크 목록</returns>
    protected override Task<IEnumerable<string>> CreateChunksAsync(
        string text,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(Enumerable.Empty<string>());

        // 문단 분리
        var paragraphs = SplitIntoParagraphs(text);

        // 문단들을 청크로 그룹화
        var chunks = GroupParagraphsIntoChunks(paragraphs);

        return Task.FromResult(chunks.AsEnumerable());
    }

    /// <summary>
    /// 텍스트를 문단으로 분리
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>문단 목록</returns>
    private List<ParagraphInfo> SplitIntoParagraphs(string text)
    {
        var paragraphs = new List<ParagraphInfo>();

        // 다양한 문단 구분자로 분리
        var paragraphSeparators = new[] { "\n\n", "\r\n\r\n", "\n\r\n\r" };
        var sections = text.Split(paragraphSeparators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var section in sections)
        {
            var cleanSection = section.Trim();
            if (string.IsNullOrEmpty(cleanSection)) continue;

            var paragraphType = DetermineParagraphType(cleanSection);
            var priority = GetParagraphPriority(paragraphType);

            paragraphs.Add(new ParagraphInfo
            {
                Content = cleanSection,
                Type = paragraphType,
                Priority = priority,
                WordCount = CountWords(cleanSection),
                CharacterCount = cleanSection.Length
            });
        }

        // 단일 문단이 너무 긴 경우 문장으로 세분화
        var refinedParagraphs = new List<ParagraphInfo>();
        foreach (var paragraph in paragraphs)
        {
            if (paragraph.CharacterCount > _configuration.MaxChunkSize && _configuration.MaxChunkSize > 0)
            {
                var subParagraphs = SplitLongParagraph(paragraph);
                refinedParagraphs.AddRange(subParagraphs);
            }
            else
            {
                refinedParagraphs.Add(paragraph);
            }
        }

        return refinedParagraphs;
    }

    /// <summary>
    /// 문단 유형 결정
    /// </summary>
    /// <param name="content">문단 내용</param>
    /// <returns>문단 유형</returns>
    private ParagraphType DetermineParagraphType(string content)
    {
        var trimmedContent = content.Trim();

        // 제목 패턴
        if (trimmedContent.StartsWith("#") ||
            (trimmedContent.Length < 100 && trimmedContent.EndsWith(":")) ||
            Regex.IsMatch(trimmedContent, @"^[A-Z\s]{10,50}$"))
        {
            return ParagraphType.Heading;
        }

        // 목록 항목
        if (Regex.IsMatch(trimmedContent, @"^(\s*)([•\-\*\+]|\d+\.)\s+"))
        {
            return ParagraphType.ListItem;
        }

        // 인용문
        if (trimmedContent.StartsWith(">") ||
            (trimmedContent.StartsWith("\"") && trimmedContent.EndsWith("\"")))
        {
            return ParagraphType.Quote;
        }

        // 코드 블록
        if (trimmedContent.StartsWith("```") ||
            trimmedContent.Split('\n').All(line => line.StartsWith("    ") || line.Trim().Length == 0))
        {
            return ParagraphType.Code;
        }

        // 테이블
        if (trimmedContent.Contains("|") && trimmedContent.Split('\n').Length > 1)
        {
            return ParagraphType.Table;
        }

        // 일반 문단
        return ParagraphType.Normal;
    }

    /// <summary>
    /// 문단 우선순위 결정
    /// </summary>
    /// <param name="type">문단 유형</param>
    /// <returns>우선순위 (낮을수록 중요)</returns>
    private int GetParagraphPriority(ParagraphType type)
    {
        return type switch
        {
            ParagraphType.Heading => 1,
            ParagraphType.Normal => 2,
            ParagraphType.Quote => 3,
            ParagraphType.ListItem => 4,
            ParagraphType.Table => 5,
            ParagraphType.Code => 6,
            _ => 9
        };
    }

    /// <summary>
    /// 긴 문단을 문장으로 분할
    /// </summary>
    /// <param name="paragraph">긴 문단</param>
    /// <returns>분할된 문단들</returns>
    private List<ParagraphInfo> SplitLongParagraph(ParagraphInfo paragraph)
    {
        var sentences = SplitIntoSentences(paragraph.Content);
        var subParagraphs = new List<ParagraphInfo>();
        var currentContent = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var potentialContent = currentContent.Length == 0
                ? sentence
                : currentContent + " " + sentence;

            if (potentialContent.Length <= _configuration.MaxChunkSize || currentContent.Length == 0)
            {
                if (currentContent.Length > 0)
                    currentContent.Append(" ");
                currentContent.Append(sentence);
            }
            else
            {
                // 현재 누적된 내용을 문단으로 추가
                if (currentContent.Length > 0)
                {
                    subParagraphs.Add(new ParagraphInfo
                    {
                        Content = currentContent.ToString(),
                        Type = paragraph.Type,
                        Priority = paragraph.Priority,
                        WordCount = CountWords(currentContent.ToString()),
                        CharacterCount = currentContent.Length
                    });
                }

                // 새 문단 시작
                currentContent.Clear();
                currentContent.Append(sentence);
            }
        }

        // 마지막 누적 내용 추가
        if (currentContent.Length > 0)
        {
            subParagraphs.Add(new ParagraphInfo
            {
                Content = currentContent.ToString(),
                Type = paragraph.Type,
                Priority = paragraph.Priority,
                WordCount = CountWords(currentContent.ToString()),
                CharacterCount = currentContent.Length
            });
        }

        return subParagraphs;
    }

    /// <summary>
    /// 텍스트를 문장으로 분리
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>문장 목록</returns>
    private List<string> SplitIntoSentences(string text)
    {
        // 문장 종결 부호로 분리 (복잡한 케이스 고려)
        var sentencePattern = @"(?<=[.!?])\s+(?=[A-Z\u4e00-\u9fff\uac00-\ud7af])";
        var sentences = Regex.Split(text, sentencePattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        return sentences;
    }

    /// <summary>
    /// 문단들을 청크로 그룹화
    /// </summary>
    /// <param name="paragraphs">문단 목록</param>
    /// <returns>청크 목록</returns>
    private List<string> GroupParagraphsIntoChunks(List<ParagraphInfo> paragraphs)
    {
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        var currentChunkSize = 0;
        var currentChunkParagraphs = new List<ParagraphInfo>();

        foreach (var paragraph in paragraphs)
        {
            var paragraphSize = paragraph.CharacterCount;
            var potentialSize = currentChunkSize + paragraphSize +
                               (currentChunkSize > 0 ? 2 : 0); // 문단 구분자 공간

            // 청크 크기 제한 확인
            var exceedsMaxSize = _configuration.MaxChunkSize > 0 && potentialSize > _configuration.MaxChunkSize;
            var exceedsMinSize = currentChunkSize >= _configuration.MinChunkSize;

            if (exceedsMaxSize && exceedsMinSize)
            {
                // 현재 청크를 완성하고 새 청크 시작
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                }

                // 새 청크 초기화
                currentChunk.Clear();
                currentChunk.Append(paragraph.Content);
                currentChunkSize = paragraphSize;
                currentChunkParagraphs.Clear();
                currentChunkParagraphs.Add(paragraph);
            }
            else
            {
                // 현재 청크에 문단 추가
                if (currentChunk.Length > 0)
                {
                    currentChunk.AppendLine().AppendLine(); // 문단 구분자
                }
                currentChunk.Append(paragraph.Content);
                currentChunkSize += paragraphSize + (currentChunkParagraphs.Count > 0 ? 2 : 0);
                currentChunkParagraphs.Add(paragraph);
            }
        }

        // 마지막 청크 추가
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    /// <summary>
    /// 문단 기반 전략 특화 후처리
    /// </summary>
    /// <param name="rawChunks">원시 청크</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리된 청크</returns>
    protected override Task<IEnumerable<string>> PostprocessChunksAsync(
        IEnumerable<string> rawChunks,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        var chunks = rawChunks.ToList();

        // 기본 후처리
        var processedChunks = base.PostprocessChunksAsync(chunks, extractedContent, cancellationToken).Result.ToList();

        // 문단 기반 전략 특화 처리

        // 1. 청크 간 의미적 연결성 분석
        AnalyzeSemanticCoherence(processedChunks, extractedContent);

        // 2. 제목과 내용의 분리 방지
        processedChunks = PreventHeadingContentSeparation(processedChunks);

        return Task.FromResult(processedChunks.AsEnumerable());
    }

    /// <summary>
    /// 의미적 연결성 분석 및 로깅
    /// </summary>
    /// <param name="chunks">청크 목록</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    private void AnalyzeSemanticCoherence(List<string> chunks, ExtractedContent extractedContent)
    {
        var coherenceScores = new List<double>();

        for (int i = 0; i < chunks.Count - 1; i++)
        {
            var score = CalculateCoherenceScore(chunks[i], chunks[i + 1]);
            coherenceScores.Add(score);
        }

        if (coherenceScores.Any())
        {
            var avgCoherence = coherenceScores.Average();

            // 의미적 연결성 분석 결과 이벤트 발행
            _ = Task.Run(async () =>
            {
                await _eventPublisher.PublishAsync(new ChunkingCoherenceAnalysisEvent
                {
                    Url = extractedContent.OriginalUrl,
                    ChunkCount = chunks.Count,
                    AverageCoherence = avgCoherence,
                    MinCoherence = coherenceScores.Min(),
                    MaxCoherence = coherenceScores.Max(),
                    Strategy = GetStrategyName(),
                    Timestamp = DateTimeOffset.UtcNow
                });
            });
        }
    }

    /// <summary>
    /// 두 청크 간 연결성 점수 계산 (단순한 휴리스틱)
    /// </summary>
    /// <param name="chunk1">첫 번째 청크</param>
    /// <param name="chunk2">두 번째 청크</param>
    /// <returns>연결성 점수 (0-1)</returns>
    private double CalculateCoherenceScore(string chunk1, string chunk2)
    {
        // 단순한 키워드 겹침 기반 계산
        var words1 = ExtractKeywords(chunk1);
        var words2 = ExtractKeywords(chunk2);

        if (!words1.Any() || !words2.Any()) return 0.0;

        var commonWords = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
        var totalWords = words1.Union(words2, StringComparer.OrdinalIgnoreCase).Count();

        return totalWords > 0 ? (double)commonWords / totalWords : 0.0;
    }

    /// <summary>
    /// 제목과 내용 분리 방지
    /// </summary>
    /// <param name="chunks">청크 목록</param>
    /// <returns>조정된 청크 목록</returns>
    private List<string> PreventHeadingContentSeparation(List<string> chunks)
    {
        var adjustedChunks = new List<string>();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // 청크가 제목으로 끝나는 경우
            if (lines.Length > 0 && IsHeadingLine(lines.Last()) && i + 1 < chunks.Count)
            {
                // 다음 청크의 첫 부분을 가져와서 병합
                var nextChunk = chunks[i + 1];
                var nextLines = nextChunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (nextLines.Length > 0)
                {
                    var combinedLength = chunk.Length + nextLines[0].Length + 1;

                    // 크기 제한 내에서 병합 가능한 경우
                    if (_configuration.MaxChunkSize <= 0 || combinedLength <= _configuration.MaxChunkSize)
                    {
                        chunk += "\n" + nextLines[0];

                        // 다음 청크에서 첫 줄 제거
                        if (nextLines.Length > 1)
                        {
                            chunks[i + 1] = string.Join("\n", nextLines.Skip(1));
                        }
                        else
                        {
                            chunks.RemoveAt(i + 1);
                        }
                    }
                }
            }

            adjustedChunks.Add(chunk);
        }

        return adjustedChunks;
    }

    /// <summary>
    /// 제목 라인인지 확인
    /// </summary>
    /// <param name="line">확인할 라인</param>
    /// <returns>제목 여부</returns>
    private bool IsHeadingLine(string line)
    {
        var trimmed = line.Trim();

        // 마크다운 제목
        if (trimmed.StartsWith("#")) return true;

        // 콜론으로 끝나는 짧은 라인
        if (trimmed.EndsWith(":") && trimmed.Length < 100) return true;

        // 대문자로만 구성된 짧은 라인
        if (trimmed.Length < 80 && trimmed.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c)))
            return true;

        return false;
    }

    /// <summary>
    /// 전략 이름 반환
    /// </summary>
    /// <returns>전략 이름</returns>
    protected override string GetStrategyName()
    {
        return "Paragraph";
    }
}

/// <summary>
/// 문단 정보 클래스
/// </summary>
public class ParagraphInfo
{
    public string Content { get; set; } = string.Empty;
    public ParagraphType Type { get; set; }
    public int Priority { get; set; }
    public int WordCount { get; set; }
    public int CharacterCount { get; set; }
}

/// <summary>
/// 문단 유형 열거형
/// </summary>
public enum ParagraphType
{
    Normal,
    Heading,
    ListItem,
    Quote,
    Code,
    Table
}

/// <summary>
/// 청킹 연결성 분석 이벤트
/// </summary>
public class ChunkingCoherenceAnalysisEvent : ProcessingEvent
{
    public override string EventType => "ChunkingCoherenceAnalysis";
    public string Url { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public double AverageCoherence { get; set; }
    public double MinCoherence { get; set; }
    public double MaxCoherence { get; set; }
    public string Strategy { get; set; } = string.Empty;
}