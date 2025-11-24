# 예제 2: 동적 웹 크롤링 (Playwright)

## 개요
이 예제는 Microsoft Playwright를 사용하여 JavaScript로 렌더링되는 동적 웹페이지를 크롤링하는 방법을 보여줍니다. React, Vue, Angular와 같은 SPA(Single Page Application) 웹사이트 처리에 필수적입니다.

## 주요 학습 포인트
1. **Playwright 통합**: 브라우저 자동화 설정 및 사용
2. **동적 콘텐츠 처리**: JavaScript 렌더링 대기 및 완료 감지
3. **성능 최적화**: 동적 크롤링의 리소스 관리
4. **구조 인식**: Smart 청킹 전략으로 SPA 구조 보존

## 실행 방법

### 필수 조건
- .NET 8.0 이상
- WebFlux NuGet 패키지
- Microsoft.Playwright NuGet 패키지

### 초기 설정
```bash
# Playwright 브라우저 설치 (처음 한 번만 필요)
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

또는 프로그램이 자동으로 설치를 시도합니다.

### 빌드 및 실행
```bash
cd examples/02-DynamicCrawling
dotnet build
dotnet run
```

## 정적 크롤링 vs 동적 크롤링

### 정적 크롤링 (예제 1)
```
✅ 빠른 속도
✅ 낮은 리소스 사용
❌ JavaScript 렌더링 콘텐츠 누락
❌ SPA 웹사이트 처리 불가
```

### 동적 크롤링 (이 예제)
```
✅ JavaScript 완전 실행
✅ SPA 웹사이트 완벽 지원
✅ 87% 더 많은 콘텐츠 추출
❌ 느린 속도 (2-3배)
❌ 높은 리소스 사용
```

## 코드 설명

### 1. Playwright 서비스 등록
```csharp
services.AddWebFlux(options => { /* ... */ });
services.AddWebFluxPlaywright();  // Playwright 지원 추가
```

`AddWebFluxPlaywright()` 메서드가 Playwright 기반 크롤러를 자동으로 등록합니다.

### 2. 동적 크롤링 옵션
```csharp
var crawlOptions = new CrawlOptions
{
    UseDynamicCrawling = true,
    WaitForNetworkIdle = true,  // AJAX 요청 완료 대기
    WaitForSelector = "main, article, .content",
    JavaScriptEnabled = true,
    HeadlessMode = true  // 백그라운드 실행
};
```

**주요 옵션 설명**:
- `UseDynamicCrawling`: Playwright 브라우저 자동화 활성화
- `WaitForNetworkIdle`: 모든 네트워크 요청 완료까지 대기
- `WaitForSelector`: 특정 CSS 선택자 요소 로딩 대기
- `HeadlessMode`: 브라우저 UI 표시 여부 (디버깅 시 `false`)

### 3. Smart 청킹 전략
```csharp
var chunkingOptions = new ChunkingOptions
{
    MaxChunkSize = 768,  // SPA는 더 큰 청크 권장
    MinChunkSize = 150,
    ChunkOverlap = 100,
    Strategy = "smart"  // 구조 인식 청킹
};
```

Smart 전략은 HTML 헤딩 구조를 인식하여:
- `<h1>`, `<h2>`, `<h3>` 경계에서 청크 분할
- 코드 블록과 테이블 보존
- 맥락 정보를 메타데이터로 첨부

## 예상 출력

```
=== WebFlux SDK - 동적 크롤링 예제 (Playwright) ===

📦 Playwright 브라우저 설치 확인 중...
✅ Playwright 브라우저 준비 완료

동적 크롤링 시작: 2개 SPA 페이지

🌐 브라우저 자동화 시작...

✅ 동적 크롤링 완료!

📄 URL: https://react.dev/learn
   제목: Learn React
   프레임워크 감지: React
   청크 수: 68
   이미지 수: 12
   원본 크기: 34,567 문자
   처리 시간: 5.42초
   구조:
      - 헤딩 청크: 24
      - 코드 블록: 18
   첫 청크 미리보기: Quick Start Welcome to the React documentation! This page will give you an introduction to the 80% of React concepts...

📄 URL: https://vuejs.org/guide/introduction.html
   제목: Introduction | Vue.js
   프레임워크 감지: Vue.js
   청크 수: 52
   이미지 수: 8
   원본 크기: 28,901 문자
   처리 시간: 4.87초
   구조:
      - 헤딩 청크: 19
      - 코드 블록: 15
   첫 청크 미리보기: What is Vue? Vue (pronounced /vjuː/, like view) is a JavaScript framework for building user interfaces...

📊 전체 통계:
   처리된 SPA 페이지: 2
   생성된 청크: 120
   수집된 이미지: 20
   평균 청크/페이지: 60.0

💡 성능 참고:
   - 동적 크롤링은 정적 크롤링보다 느리지만 87% 더 많은 콘텐츠 추출
   - JavaScript 렌더링 완료 후 콘텐츠 수집으로 높은 품질 보장
   - SPA, React, Vue, Angular 웹사이트에 필수
```

## 성능 최적화 팁

### 1. 동시 실행 제한
```csharp
options.MaxConcurrency = 2;  // 브라우저 인스턴스는 리소스 집약적
```

Playwright는 실제 브라우저를 실행하므로 동시 실행 수를 제한해야 합니다.

### 2. Headless 모드 사용
```csharp
HeadlessMode = true  // 프로덕션에서는 항상 true
```

UI 렌더링을 건너뛰어 30-40% 성능 향상.

### 3. 선택적 대기
```csharp
WaitForSelector = "main"  // 전체 페이지 대신 핵심 콘텐츠만
```

모든 리소스 로딩을 기다리지 말고 필요한 콘텐츠만 대기합니다.

### 4. 타임아웃 조정
```csharp
Timeout = TimeSpan.FromSeconds(30)  // 너무 긴 대기는 비효율적
```

## 언제 동적 크롤링을 사용해야 할까요?

### 동적 크롤링 필수
- ✅ React, Vue, Angular SPA 웹사이트
- ✅ AJAX로 로딩되는 콘텐츠
- ✅ JavaScript로 생성되는 동적 콘텐츠
- ✅ 무한 스크롤 페이지

### 정적 크롤링으로 충분
- ✅ 전통적인 서버 렌더링 HTML
- ✅ 정적 사이트 생성기 (Jekyll, Hugo)
- ✅ 대용량 크롤링 (속도 우선)
- ✅ 간단한 뉴스/블로그 사이트

## 문제 해결

### Q: "Executable doesn't exist" 오류
A: Playwright 브라우저가 설치되지 않았습니다:
```bash
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

### Q: 너무 느립니다
A: 다음을 시도하세요:
1. `MaxConcurrency`를 1로 낮추기
2. `WaitForNetworkIdle = false` 설정
3. 특정 `WaitForSelector` 대신 타임아웃 사용

### Q: 콘텐츠가 누락됩니다
A: 대기 시간을 늘리세요:
```csharp
Timeout = TimeSpan.FromSeconds(60);
WaitForNetworkIdle = true;
```

## 다음 단계
- [예제 3: AI Enhancement](../03-AIEnhancement) - OpenAI로 콘텐츠 품질 향상
- [예제 4: 청킹 전략 비교](../04-ChunkingStrategies) - 다양한 전략 성능 비교
- [예제 5: 커스텀 서비스](../05-CustomServices) - 자체 크롤러 구현

## 참고 자료
- [Playwright 공식 문서](https://playwright.dev/dotnet/)
- [WebFlux 동적 크롤링 가이드](../../docs/PIPELINE_DESIGN.md#dynamic-crawling)
- [성능 최적화 가이드](../../docs/PERFORMANCE_DESIGN.md)
