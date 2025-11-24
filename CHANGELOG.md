# Changelog

All notable changes to this project will be documented in this file.

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
