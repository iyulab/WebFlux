# WebFlux SDK 사용 예제

WebFlux SDK의 다양한 기능을 보여주는 5가지 실용적인 예제 모음입니다. 각 예제는 독립적으로 실행 가능하며, 단계별로 학습할 수 있도록 구성되어 있습니다.

## 📚 예제 목록

### 예제 1: 기본 웹 크롤링
**난이도**: ⭐ 초급
**소요 시간**: 10분
**학습 목표**: WebFlux SDK의 기본 사용법 이해

정적 HTML 페이지를 크롤링하고 청킹하는 가장 기본적인 사용 예제입니다.

```bash
cd 01-BasicCrawling
dotnet run
```

**주요 내용**:
- WebFlux 서비스 등록 및 설정
- 크롤링 옵션 구성
- 문단 기반 청킹 전략 사용
- 결과 분석 및 출력

**다음으로**: [예제 2 - 동적 크롤링](./02-DynamicCrawling)

---

### 예제 2: 동적 웹 크롤링 (Playwright)
**난이도**: ⭐⭐ 중급
**소요 시간**: 15분
**학습 목표**: JavaScript 렌더링 페이지 크롤링

Microsoft Playwright를 사용하여 React, Vue, Angular와 같은 SPA 웹사이트를 크롤링합니다.

```bash
cd 02-DynamicCrawling

# Playwright 브라우저 설치 (처음 한 번만)
pwsh bin/Debug/net10.0/playwright.ps1 install chromium

dotnet run
```

**주요 내용**:
- Playwright 통합 및 설정
- 동적 콘텐츠 대기 메커니즘
- Smart 청킹 전략으로 구조 보존
- 정적 vs 동적 크롤링 성능 비교

**다음으로**: [예제 3 - AI Enhancement](./03-AIEnhancement)

---

### 예제 3: AI Enhancement (OpenAI 통합)
**난이도**: ⭐⭐ 중급
**소요 시간**: 20분
**학습 목표**: AI로 콘텐츠 품질 향상

OpenAI API를 사용하여 크롤링된 콘텐츠를 요약하고, 키워드를 추출하며, 관련 질문을 생성합니다.

```bash
# 환경 변수 설정
export OPENAI_API_KEY="sk-your-api-key"

cd 03-AIEnhancement
dotnet run
```

**주요 내용**:
- OpenAI 서비스 통합
- 자동 요약 및 번역 (영문 → 한국어)
- 키워드 추출 및 관련 질문 생성
- 토큰 사용량 추적 및 비용 분석

**다음으로**: [예제 4 - 청킹 전략 비교](./04-ChunkingStrategies)

---

### 예제 4: 청킹 전략 비교
**난이도**: ⭐⭐⭐ 고급
**소요 시간**: 15분
**학습 목표**: 최적의 청킹 전략 선택

6가지 청킹 전략의 성능, 메모리 효율성, 품질을 비교 분석합니다.

```bash
cd 04-ChunkingStrategies
dotnet run
```

**주요 내용**:
- 6가지 전략 성능 벤치마킹
- 처리 시간, 메모리, 품질 점수 측정
- 시나리오별 최적 전략 추천
- 성능 비교 차트 및 상세 분석

**비교 전략**:
1. **FixedSize** - 고정 크기 (최고 속도)
2. **Paragraph** - 문단 기반 (자연스러움)
3. **Smart** - 구조 인식 (기술 문서)
4. **Semantic** - 의미론적 (최고 품질)
5. **MemoryOptimized** - 메모리 최적화 (대용량)
6. **Auto** - 자동 선택 (다목적)

**다음으로**: [예제 5 - 커스텀 서비스](./05-CustomServices)

---

### 예제 5: 커스텀 서비스 구현
**난이도**: ⭐⭐⭐ 고급
**소요 시간**: 25분
**학습 목표**: 자체 청킹/AI 서비스 구현

WebFlux 인터페이스를 구현하여 프로젝트 특화 커스텀 서비스를 만듭니다.

```bash
cd 05-CustomServices
dotnet run
```

**주요 내용**:
- IChunkingStrategy 커스텀 구현 (문장 기반)
- ITextCompletionService 커스텀 구현 (규칙 기반)
- 의존성 주입 및 서비스 등록
- 실전 사용 사례 (법률, 코드 문서)

**학습 패턴**:
- 전략 패턴 (Strategy Pattern)
- 팩토리 패턴 (Factory Pattern)
- 데코레이터 패턴 (Decorator Pattern)

---

## 🚀 빠른 시작

### 전체 예제 빌드
```bash
# examples 디렉토리로 이동
cd D:\data\WebFlux\examples

# 모든 예제 빌드
for dir in 01-BasicCrawling 02-DynamicCrawling 03-AIEnhancement 04-ChunkingStrategies 05-CustomServices; do
    cd $dir
    dotnet build
    cd ..
done
```

### 순차적 학습 경로
```
1. 기본 크롤링 (10분) → WebFlux 기초 이해
                ↓
2. 동적 크롤링 (15분) → Playwright 통합
                ↓
3. AI Enhancement (20분) → OpenAI 통합
                ↓
4. 청킹 전략 비교 (15분) → 전략 선택 기준
                ↓
5. 커스텀 서비스 (25분) → 고급 커스터마이징

총 학습 시간: 약 85분
```

## 📋 필수 조건

### 공통 요구사항
- .NET 8.0 이상 SDK
- WebFlux NuGet 패키지

### 예제별 추가 요구사항

| 예제 | 추가 요구사항 |
|------|------------|
| 예제 1 | 없음 |
| 예제 2 | Microsoft.Playwright, Chromium 브라우저 |
| 예제 3 | OpenAI API 키 |
| 예제 4 | 없음 |
| 예제 5 | 없음 |

## 🎯 시나리오별 추천 예제

### 빠르게 시작하고 싶다면
→ **예제 1: 기본 크롤링**
가장 간단한 예제로 WebFlux의 핵심 개념을 빠르게 이해할 수 있습니다.

### SPA 웹사이트를 처리해야 한다면
→ **예제 2: 동적 크롤링**
React, Vue, Angular 웹사이트 크롤링에 필수적입니다.

### RAG 품질을 향상시키고 싶다면
→ **예제 3: AI Enhancement**
OpenAI로 요약, 키워드 추출, 관련 질문 생성을 자동화합니다.

### 최적의 청킹 전략을 찾고 싶다면
→ **예제 4: 청킹 전략 비교**
6가지 전략의 성능을 비교하여 프로젝트에 맞는 전략을 선택합니다.

### 도메인 특화 기능이 필요하다면
→ **예제 5: 커스텀 서비스**
법률, 의료, 금융 등 특수 분야에 맞춘 청킹 로직을 구현합니다.

## 🔧 문제 해결

### 빌드 오류
```bash
# NuGet 패키지 복원
dotnet restore

# 클린 빌드
dotnet clean
dotnet build
```

### Playwright 설치 오류 (예제 2)
```bash
# 수동 설치
pwsh bin/Debug/net10.0/playwright.ps1 install chromium

# 또는
playwright install chromium
```

### OpenAI API 오류 (예제 3)
```bash
# API 키 확인
echo $OPENAI_API_KEY

# 키 재설정 (Linux/Mac)
export OPENAI_API_KEY="sk-your-key"

# 키 재설정 (Windows)
setx OPENAI_API_KEY "sk-your-key"
```

## 📖 학습 리소스

### WebFlux 공식 문서
- [아키텍처 가이드](../docs/ARCHITECTURE.md)
- [인터페이스 레퍼런스](../docs/INTERFACES.md)
- [청킹 전략 가이드](../docs/CHUNKING_STRATEGIES.md)
- [성능 최적화](../docs/PERFORMANCE_DESIGN.md)

### 외부 리소스
- [OpenAI API 문서](https://platform.openai.com/docs)
- [Playwright 문서](https://playwright.dev/dotnet/)
- [RAG 시스템 설계](https://www.anthropic.com/index/contextual-retrieval)

## 💡 다음 단계

### 프로젝트 적용
1. 가장 관심있는 예제 실행
2. 프로젝트 요구사항에 맞게 수정
3. 단위 테스트 작성
4. 프로덕션 환경 배포

### 고급 주제
- [멀티모달 처리](../docs/MULTIMODAL_DESIGN.md)
- [Web Intelligence Engine](../TASKS.md#phase-4)
- [메타데이터 통합](../docs/METADATA_INTEGRATION.md)

## 🤝 기여

예제 개선이나 새로운 예제 추가를 환영합니다!

1. Fork the repository
2. Create your feature branch
3. 예제 작성 및 테스트
4. Pull Request 제출

## 📝 라이센스

이 예제들은 WebFlux SDK와 동일한 라이센스를 따릅니다.

---

**Happy Coding with WebFlux SDK! 🚀**
