# 예제 1: 기본 웹 크롤링

## 개요
이 예제는 WebFlux SDK의 가장 기본적인 사용 방법을 보여줍니다. 정적 HTML 페이지를 크롤링하고 청킹하는 전체 흐름을 학습할 수 있습니다.

## 주요 학습 포인트
1. **서비스 등록**: WebFlux 서비스를 DI 컨테이너에 등록하는 방법
2. **크롤링 옵션**: 크롤링 동작을 제어하는 다양한 옵션 설정
3. **청킹 전략**: 문단 기반 청킹 전략 사용
4. **결과 처리**: 크롤링 및 청킹 결과 분석

## 실행 방법

### 필수 조건
- .NET 8.0 이상
- WebFlux NuGet 패키지

### 빌드 및 실행
```bash
# 프로젝트 디렉토리로 이동
cd examples/01-BasicCrawling

# 빌드
dotnet build

# 실행
dotnet run
```

## 코드 설명

### 1. 서비스 등록
```csharp
var services = new ServiceCollection();

services.AddWebFlux(options =>
{
    options.MaxConcurrency = 3;
    options.UserAgent = "WebFlux-Example/1.0";
    options.RequestDelay = TimeSpan.FromMilliseconds(500);
    options.DefaultChunkSize = 512;
    options.ChunkOverlap = 50;
});
```

`AddWebFlux` 확장 메서드를 사용하여 모든 필수 서비스를 자동으로 등록합니다.

### 2. 크롤링 옵션 구성
```csharp
var crawlOptions = new CrawlOptions
{
    MaxDepth = 0,  // 주어진 URL만 크롤링
    FollowExternalLinks = false,
    RespectRobotsTxt = true,
    Timeout = TimeSpan.FromSeconds(30)
};
```

- `MaxDepth = 0`: 링크를 따라가지 않고 주어진 URL만 크롤링
- `RespectRobotsTxt = true`: robots.txt 규칙 준수
- `Timeout`: 각 페이지당 최대 대기 시간

### 3. 청킹 옵션 구성
```csharp
var chunkingOptions = new ChunkingOptions
{
    MaxChunkSize = 512,
    MinChunkSize = 100,
    ChunkOverlap = 64,
    Strategy = "paragraph"
};
```

- `Strategy = "paragraph"`: 문단 경계를 인식하여 자연스러운 청크 생성
- `ChunkOverlap = 64`: 청크 간 64자 겹침으로 맥락 보존

### 4. 실행 및 결과 확인
```csharp
var results = await processor.ProcessUrlsAsync(
    urls,
    crawlOptions,
    chunkingOptions
);
```

비동기적으로 모든 URL을 처리하고 결과를 반환합니다.

## 예상 출력

```
=== WebFlux SDK - 기본 크롤링 예제 ===

크롤링 시작: 2개 페이지

✅ 크롤링 완료!

📄 URL: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12
   제목: What's new in C# 12
   청크 수: 45
   원본 크기: 23,456 문자
   처리 시간: 2.34초
   첫 청크 미리보기: C# 12 introduces several new features...

📄 URL: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9
   제목: What's new in .NET 9
   청크 수: 52
   원본 크기: 28,901 문자
   처리 시간: 2.67초
   첫 청크 미리보기: .NET 9 brings performance improvements...

📊 전체 통계:
   처리된 페이지: 2
   생성된 청크: 97
   평균 청크/페이지: 48.5
```

## 다음 단계
- [예제 2: 동적 크롤링](../02-DynamicCrawling) - Playwright를 사용한 JavaScript 렌더링 페이지 처리
- [예제 3: AI Enhancement](../03-AIEnhancement) - OpenAI 통합으로 고급 텍스트 처리
- [예제 4: 청킹 전략 비교](../04-ChunkingStrategies) - 다양한 청킹 전략 성능 비교

## 문제 해결

### Q: "User agent must be set" 오류가 발생합니다
A: `UserAgent` 옵션이 설정되지 않았습니다. 옵션에 `UserAgent` 값을 지정하세요.

### Q: robots.txt 제한으로 크롤링이 차단됩니다
A: `RespectRobotsTxt = false`로 설정하거나 (테스트 목적으로만), 허용된 User-Agent로 변경하세요.

### Q: 청크가 너무 작거나 큽니다
A: `MaxChunkSize`, `MinChunkSize` 값을 조정하거나 다른 청킹 전략을 사용하세요.

## 참고 자료
- [WebFlux 공식 문서](../../docs/REFERENCE_GUIDE.md)
- [청킹 전략 가이드](../../docs/CHUNKING_STRATEGIES.md)
- [API 레퍼런스](../../docs/INTERFACES.md)
