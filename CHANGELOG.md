# Changelog

All notable changes to this project will be documented in this file.

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
