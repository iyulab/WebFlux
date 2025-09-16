# WebFlux 프로젝트 컨텍스트

## 🎯 프로젝트 개요
**WebFlux**는 RAG(Retrieval-Augmented Generation) 시스템을 위한 완전한 웹 콘텐츠 처리 SDK입니다. **.NET 9 기반**으로 웹 콘텐츠를 RAG에 최적화된 구조화된 청크로 변환하는 기능을 제공합니다.

## 🏗️ 현재 상태 분석

### 디렉터리 구조
```
D:\data\WebFlux\
├── README.md                 # 프로젝트 문서 (17,591 chars)
├── LICENSE                   # 라이선스 파일 (새 파일)
├── docs/
│   └── research.md          # RAG 기술 연구 문서 (심도 있는 분석)
├── src/
│   └── WebFlux.sln         # 빈 Visual Studio 솔루션
└── claudedocs/             # Claude 관련 문서 (생성됨)
    └── project_context.md  # 이 파일
```

### Git 상태
- **Branch**: main
- **Modified**: README.md (수정됨)
- **Untracked**: LICENSE, docs/, src/

### 아키텍처 원칙
- **인터페이스 제공자**: WebFlux는 인터페이스를 정의하고, 소비 애플리케이션이 구현체를 선택
- **Clean Architecture**: 의존성 역전으로 확장성 보장
- **AI 공급자 중립**: OpenAI, Anthropic, Azure 등 특정 공급자에 종속되지 않음

## 🎛️ 핵심 기능 (설계 단계)

### ✅ WebFlux가 제공할 기능
1. **🕷️ 웹 크롤링**: 구조적 사이트맵 생성 및 지능형 페이지 탐색
2. **📄 콘텐츠 추출**: HTML → 구조화된 텍스트, 메타데이터 보존
3. **🔌 AI 인터페이스**: ITextCompletionService, IImageToTextService 계약 정의
4. **🎛️ 처리 파이프라인**: Crawler → Extractor → Parser → Chunking 오케스트레이션
5. **🧪 Mock 서비스**: 테스트용 MockTextCompletionService, MockImageToTextService

### ❌ WebFlux가 제공하지 않는 것
- AI 서비스 구현 (OpenAI, Anthropic 등)
- 벡터 생성 (임베딩 생성은 소비 앱 책임)
- 데이터 저장 (Pinecone, Qdrant 등)

## 🎯 청킹 전략
7가지 청킹 전략 지원 예정:
1. **Auto** (권장) - 자동 최적 전략 선택
2. **Smart** - 기술 문서, API 문서용
3. **Intelligent** - 블로그, 뉴스, 지식베이스용
4. **MemoryOptimized** - 대규모 사이트, 서버 환경 (84% 메모리 절감)
5. **Semantic** - 일반 웹페이지, 아티클용
6. **Paragraph** - 마크다운 문서, 위키용
7. **FixedSize** - 균일한 처리 필요

## 📊 성능 목표
- **크롤링 속도**: 100페이지/분 (평균 1MB 페이지 기준)
- **메모리 효율**: 페이지 크기 1.5배 이하 메모리 사용
- **품질 보장**: 청크 완성도 81%, 컨텍스트 보존 75%+ 달성

## 🔧 기술 스택
- **.NET 9**
- **Clean Architecture**
- **Dependency Injection**
- **AsyncEnumerable** (스트리밍 최적화)
- **Threading.Channels** (병렬 처리)

## 📚 문서 상태
- **README.md**: 완성도 높은 사용법 가이드 및 아키텍처 설명
- **docs/research.md**: RAG 전처리 기술에 대한 심도 있는 연구 문서
- **코드베이스**: 현재 빈 솔루션 상태 (구현 대기)

## 🚀 다음 단계
1. **프로젝트 구조 설정**: 라이브러리, 테스트, 샘플 프로젝트 생성
2. **인터페이스 정의**: 핵심 서비스 인터페이스 구현
3. **웹 크롤러 구현**: 첫 번째 주요 기능
4. **청킹 전략 구현**: 7가지 전략 구현
5. **테스트 작성**: 단위 테스트 및 통합 테스트

## 📋 개발 우선순위
- 현재 빈 솔루션에서 시작하여 완전한 SDK 구현이 필요
- README.md와 research.md에 상세한 설계가 완료된 상태
- 구현 준비가 완료된 프로젝트