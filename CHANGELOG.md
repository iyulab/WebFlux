# Changelog

All notable changes to this project will be documented in this file.

## [0.7.0] - 2026-08-07

### Changed (Breaking) — 동적 렌더링이 별도 패키지 `WebFlux.Playwright` 로 분리됐다

**동적 렌더링을 쓰는 소비자는 `WebFlux.Playwright` 를 추가해야 한다.** 쓰지 않는 소비자는
아무것도 하지 않아도 되며, 그쪽이 이 변경의 목적이다.

`Microsoft.Playwright` 는 WebFlux 의 무조건 의존이었다. 브라우저 자동화를 전혀 쓰지 않는
소비자도 그것을 통해 플랫폼별 Node 런타임을 배포 산출물에 실었다 — self-contained 단일 RID
publish 에서 **460 MB 규모의, 대상 플랫폼도 아닌 런타임 4종**이 실린 사례가 실측됐다. 정적
크롤링만 쓰는 소비자에게 이것은 전부 낭비이며, 그들이 선택한 적 없는 비용이다.

- `Microsoft.Playwright` 가 `WebFlux` 의 의존성 목록에서 빠졌다.
- `PlaywrightCrawler` 와 `AddWebFluxPlaywright()` 가 `WebFlux.Playwright` 로 이동했다.
  **네임스페이스와 메서드 이름은 그대로**이므로 `using` 과 호출부는 바뀌지 않는다 —
  패키지 참조만 추가하면 된다.
- `CrawlStrategy.Dynamic` 은 계약값이므로 **enum 에 그대로 남는다.** 구현이 없을 뿐이다.

**등록 없이 동적 렌더링을 요청하면** — `CrawlStrategy.Dynamic` 이든 `UseDynamicRendering = true`
든 — 어느 패키지가 빠졌는지와 대안을 함께 알려주는 `InvalidOperationException` 으로 실패한다.
세 경로(크롤러 팩토리 둘, 옵션 하나)가 서로 다르게 실패하던 것을 하나로 통일했다. 이전에는
경로에 따라 널 참조가 되거나 서비스 키를 언급하는 DI 예외가 났고, 어느 쪽도 렌더링이나 패키지를
언급하지 않았다.

### Removed — `SmartCrawler`

정적/동적 자동 감지 크롤러였으나 **어디에도 배선돼 있지 않았다** — DI 등록도, 팩토리 분기도,
대응하는 `CrawlStrategy` 값도 없었고 문서·예제·테스트의 참조도 0건이었다. 손으로 직접 생성하지
않는 한 도달할 수 없는 public 타입이었다. 공개 표면 제거이므로 기록해 둔다.

### Added — `CrawlerKeys`

크롤러 등록 키를 상수로 노출한다. 구현을 제공하지 않는 크롤러를 외부 패키지가 채우는 구조가
되면서 키가 패키지 간 계약이 됐고, 계약을 문자열 리터럴로 양쪽에 흩어 두지 않기 위한 것이다.

## [0.6.0] - 2026-08-06

### Changed (Breaking) — 청킹을 FluxCurator 에 위임

**청크 경계가 바뀐다. 이전 버전으로 만든 인덱스는 재구축해야 한다.**

WebFlux 는 문단·고정크기·시맨틱 청커를 자체 구현으로 갖고 있었다. FluxCurator 가 이미 소유한
청커의 축소 재구현이었고, 축소된 부분이 곧 결함이었다 — 어느 것도 실패로 드러나지 않는다.
청크는 반환되기 때문이다. 요청한 청크가 아닐 뿐이다.

- **`MaxChunkSize` 가 문서대로 토큰 수로 동작한다.** 이전에는 `string.Length` 와 비교했다.
  512 를 선언한 호출자는 512 **문자**를 받았고, 오차가 문자당 토큰 비율이라 **크기가 언어마다
  달랐다** — 같은 선언에서 영어 문서 하나가 5청크로 나왔고 토큰 기준으로는 1청크였다.
  호출자가 보정할 수도 없었다. 보정 계수가 텍스트에 따라 달라지기 때문이다.
- **`ChunkOverlap` 이 동작한다.** 9개 전략 중 어느 것도 이 값을 읽지 않았다. 50 을 설정한
  호출자는 겹침 0 을 받았고, 설정이 무효라는 신호는 어디에도 없었다.
- **`Semantic` 이 임베더를 요구한다.** 이전에는 임베더가 없으면 문단 분할로 폴백하면서 결과를
  계속 "Semantic" 으로 라벨했다 — 그럴듯한 답이지만 요청은 이행되지 않았다. 이제 무엇이
  없는지 밝히며 예외를 던진다.
- **`Language` 기본값이 전달되지 않는다.** `ChunkingOptions.Language` 는 `"ko"` 로 기본
  설정되는데 이는 아무도 선택하지 않은 값이다. 토큰 크기는 언어별로 추정되므로, 그 기본값을
  그대로 넘기면 영어 문서를 한국어 비율로 재고 그것을 호출자의 선택이라 부르게 된다.
  값이 비어 있으면 언어를 감지에 맡긴다.
- **`Smart` 가 모든 헤딩에서 섹션을 연다.** 이전에는 누적 텍스트가 이미 크기를 넘은 뒤에만
  섹션을 열었다 — "여기 이음매가 있다" 와 "충분히 쌓였다" 를 뒤섞은 것이라, 그 크기 아래의
  문서에서는 헤딩이 통째로 무시되고 구조 인식을 표방한 이름 아래 문단 분할로 퇴화했다.
- **`MemoryOptimized` 는 위임 별칭이 됐다.** 스트리밍을 표방했으나 스트리밍하지 않았다 —
  텍스트 전체를 문자열로 받아 잘랐다. 고정 크기 청킹과 구별되는 유일한 동작이 그 조각을
  문자로 잰다는 것이었고, 그것이 결함이다. 100 청크마다 `GC.Collect` 를 호출하던 것도 함께
  사라졌다. 호스트 프로세스가 내려야 할 결정이었다.

**마이그레이션**

- 전략 **이름은 그대로다**(`FixedSize` / `Paragraph` / `Semantic` / `Smart` /
  `MemoryOptimized` / `Auto` / `DomStructure`). 이름으로 전략을 고르는 코드는 수정이 필요 없다.
- 타입으로 직접 생성하던 코드는 바뀐다: `FixedSizeChunkingStrategy` ·
  `ParagraphChunkingStrategy` · `SemanticChunkingStrategy` · `MemoryOptimizedChunkingStrategy`
  는 타입으로서 제거됐다. `FluxCuratorChunkingStrategy.FixedSize(...)` 등 정적 팩토리를 쓰거나
  종전대로 이름으로 해석한다.
- `SmartChunkingStrategy` 와 `AutoChunkingStrategy` 는 `FluxCurator.Core.Core.IChunkerFactory`
  를 필요로 한다. `AddWebFluxChunking()` 을 쓰면 자동으로 등록된다.
- `DomStructure` 는 그대로다. HTML 구조 분할은 텍스트 청킹이 아니며 위임 대상이 없다.

### Changed — 의존성

- `FluxCurator` / `FluxCurator.Core` **0.8.1** 참조 추가. 릴리스 순서상 WebFlux 는
  FluxCurator 다음이다.

## [0.5.4] - 2026-08-02

### Changed
- `Flux.Abstractions` 핀을 `0.24.0`으로 갱신. 계약 패키지가 독립 리포·독립 버전 라인으로 분리됐다.
  이전에는 이 패키지를 소비하는 패키지 안에서 생산돼 의존 그래프에 순환이 있었고, 그 때문에 이 핀이
  여러 릴리스 뒤에 묶여 있었다. API 변경 없음 — 기존 핀(`0.13.12`)과 타입이 동일하다.

## [0.5.0] - 2026-04-14

### Changed (Breaking)

#### 이벤트 시스템 정리
- 모든 `*EventV2` 클래스 제거 (v0.x 단계에서 V2 명명은 부적절)
- 정식 이벤트 클래스를 `Core/Models/Events/` 네임스페이스로 통합
  - `CrawlingEvents.cs`: `CrawlingStartedEvent`, `CrawlingCompletedEvent`, `PageCrawledEvent`, `UrlProcessingStartedEvent`, `UrlProcessedEvent`, `UrlProcessingFailedEvent`
  - `ChunkingEvents.cs`: `ChunkingStartedEvent`, `ChunkingCompletedEvent`, `ChunkGeneratedEvent`
  - `ExtractionEvents.cs`: `ContentExtractionStartedEvent`, `ContentExtractionCompletedEvent`, `ContentExtractionFailedEvent`, `ImageProcessedEvent`
  - `ProcessorEvents.cs`: `ProcessingStartedEvent`, `ProcessingProgressEvent`, `ProcessingCompletedEvent`, `ProcessingFailedEvent`
  - `MonitoringEvents.cs`: `ErrorOccurredEvent`, `PerformanceMetricsEvent`
- Services 계층(`EventPublisher.cs`, `WebContentProcessor.cs`, `BaseContentExtractor.cs`)에 흩어져 있던 이벤트 정의를 모두 Core 계층으로 이동
- `ProcessingEvent.cs`는 base class와 `EventSeverity`만 보존
- 이벤트 속성을 `set` → `init`으로 전환, invariant 필드는 `required` 강제
- `#if NET8_0_OR_GREATER` 가드 제거 (.NET 10 단일 타깃)
- 사용처 호환성: 소비자는 `using WebFlux.Core.Models.Events;` 추가 필요

### Documentation
- README의 `IEventPublisher` 사용 가이드를 정확한 이벤트명/카테고리 표로 보강
- CLAUDE.md 주요 인터페이스에 `IEventPublisher` 항목 추가

## [0.3.0] - 2026-02-07

### Added

#### 옵션 검증 프레임워크
- `IValidatable` 인터페이스 및 `ValidationResult` 모델 추가
- 10개 옵션 클래스에 `Validate()` 메서드 구현 (`ChunkingOptions`, `CrawlOptions`, `ExtractOptions`, `AnalysisOptions`, `ReconstructOptions`, `PipelineOptions`, `MultimodalProcessingOptions`, `TextCompletionOptions`, `ImageToTextOptions`, `HtmlChunkingOptions`)
- 진입점 메서드(`ProcessUrlAsync`, `ProcessWebsiteAsync`, `ExtractContentAsync`, `ExtractBatchAsync`)에서 자동 검증

#### URL 정규화 및 패턴 필터링
- `UrlNormalizer` 유틸리티 추가 (scheme/host 소문자화, www 제거, 기본 포트 제거, 후행 슬래시 정리, fragment 제거)
- `BaseCrawler`의 URL 중복 체크가 정규화된 URL 기반으로 개선
- `IncludeUrlPatterns` / `ExcludeUrlPatterns` 패턴 필터링 구현

#### 인터페이스 분리 (ISP)
- `IContentExtractService` 인터페이스 추가 (추출 전용 소비자용)
- `IContentChunkService` 인터페이스 추가 (청킹 전용 소비자용)
- `IWebContentProcessor`가 두 인터페이스를 상속하는 파사드로 변환
- DI 등록 시 집중 인터페이스 자동 등록

#### 이벤트 시스템 통합
- `Core/Models/Events/` 디렉토리에 이벤트 통합 (`CrawlingEvents`, `ExtractionEvents`, `ChunkingEvents`, `ProcessorEvents`, `MonitoringEvents`)

### Changed
- `ContentExtractorFactory`가 콘텐츠 타입 기반 키드 서비스 선택 지원
- `WebContentProcessor`에 선택적 `IResilienceService` 연동 (재시도 2회, Exponential Backoff)
- `ProcessUrlsBatchAsync` 병렬 처리 구현 (기존 stub 대체)
- `ProcessHtmlAsync` 실제 구현 (기존 stub 대체)

### Deprecated
- `CrawlOptions.IncludePatterns` / `ExcludePatterns` → `IncludeUrlPatterns` / `ExcludeUrlPatterns` 사용 권장
- `EventPublisher.cs` 내 레거시 이벤트 클래스들 (`CrawlStartedEvent`, `CrawlCompletedEvent`, `CrawlErrorEvent`, `CrawlWarningEvent`)

---

## [0.1.9] - 2026-01-19

### Changed
- Updated Microsoft.SourceLink.GitHub to 10.0.102 (from 8.0.0)

### Maintenance
- Cleaned up internal development documentation files
- Updated tutorial documentation for .NET 10 requirements

---

## [0.1.8] - 2025-12-XX

### Changed
- Centralized build properties with Directory.Build.props
- Centralized package version management with Directory.Packages.props
- Updated all NuGet packages to latest versions

### Maintenance
- Improved project structure with central package management

---

## [0.1.7] - 2025-11-23

### Added

#### Integration Interfaces (P0)
- `IEnrichedChunk` interface for FluxIndex compatibility
- `ISourceMetadata` interface for source document metadata
- `SourceMetadata` model with factory methods for conversion
- `WebContentChunk` now implements `IEnrichedChunk` for seamless integration

#### Web Document Metadata (P0/P1)
- `WebDocumentMetadata` model with comprehensive web standards support:
  - SEO metadata (title, description, keywords, robots, canonical)
  - Open Graph protocol (og:title, og:description, og:image, og:type, og:site_name)
  - Twitter Card data
  - Schema.org JSON-LD structured data
  - Language detection (HTML lang, HTTP headers, content analysis)
  - Site context (breadcrumbs, related pages, navigation)
- `IWebDocumentMetadataExtractor` interface
- `WebDocumentMetadataExtractor` service with full implementation

#### DOM Structure-based Chunking (P1)
- `DomStructureChunkingStrategy` preserving HTML semantic boundaries
- `HtmlChunkingOptions` for fine-grained chunking control
- Support for heading hierarchy preservation
- Special handling for code blocks, tables, and lists
- Small chunk merging for optimal chunk sizes

#### Batch Crawling Progress (P1)
- `CrawlProgress` model with detailed statistics
- `ICrawlProgressReporter` interface
- `CrawlProgressReporter` with async streaming support
- `CrawlProgressTracker` for real-time progress tracking
- Detailed error tracking and statistics

### Changed
- `WebContentChunk`: Added `HeadingPath`, `SectionTitle`, `ContextDependency`, `Source` properties
- `WebContent`: Added `OriginalHtml` property for DOM-based chunking
- Updated to AngleSharp 1.2.0 for HTML DOM parsing

### Dependencies
- Added: AngleSharp 1.2.0

---

## [0.1.6] - 2025-11-XX

### Changed
- Updated target framework to .NET 10.0
- Updated all dependencies to latest versions

---

## [0.1.5] - Previous Release

Initial public release with core functionality.
