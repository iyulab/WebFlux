# WebFlux 샘플 프로젝트

이 디렉터리는 WebFlux SDK의 사용 예제와 샘플 애플리케이션을 포함합니다.

## 🚫 CI/CD 제외 정책

이 `samples` 디렉터리는 다음과 같은 이유로 CI/CD 파이프라인에서 제외됩니다:

- **학습 목적**: 개발자 학습과 테스트를 위한 코드
- **실험적 코드**: 프로덕션 품질 표준을 적용하지 않는 실험적 구현
- **빌드 최적화**: 메인 프로젝트의 빌드 시간 단축
- **배포 분리**: 라이브러리 배포와 무관한 독립적 관리

## 구조

```
samples/
├── WebFluxSamples.sln          # 샘플 전용 솔루션
├── Directory.Build.props       # 샘플 프로젝트 공통 설정
├── README.md                   # 이 문서
├── BasicUsage/                 # 기본 사용법 예제
├── AdvancedScenarios/          # 고급 시나리오 예제
├── PerformanceBenchmarks/      # 성능 벤치마크
└── IntegrationExamples/        # 외부 서비스 연동 예제
```

## 사용법

### 1. 샘플 프로젝트 빌드
```bash
# 샘플 전용 솔루션으로 빌드
cd samples
dotnet build WebFluxSamples.sln
```

### 2. 특정 샘플 실행
```bash
# 기본 사용법 예제
cd BasicUsage
dotnet run

# 성능 벤치마크
cd PerformanceBenchmarks
dotnet run -c Release
```

## 메인 프로젝트와의 분리

- **독립적 솔루션**: `WebFluxSamples.sln`은 메인 프로젝트와 분리
- **참조만 유지**: WebFlux 라이브러리를 ProjectReference로만 참조
- **자체 빌드 설정**: 샘플에 최적화된 별도 빌드 구성
- **CI/CD 제외**: GitHub Actions 및 빌드 파이프라인에서 제외

## 개발 가이드라인

### 코드 품질
- 샘플 코드는 명확성과 학습 편의성을 우선시
- 프로덕션 코드 수준의 엄격한 품질 기준 적용 안함
- 주석과 설명을 풍부하게 포함

### 네이밍 규칙
- 프로젝트명: `WebFluxSample.{Purpose}`
- 네임스페이스: `WebFluxSample.{Category}`
- 클래스명: 명확하고 설명적인 이름 사용

### 디렉터리 구조
- 카테고리별 하위 디렉터리 생성
- 각 샘플은 독립적으로 실행 가능
- README.md로 각 샘플의 목적과 사용법 설명

## 주의사항

⚠️ **이 디렉터리의 코드는 프로덕션 사용을 위한 것이 아닙니다**
- 학습과 테스트 목적으로만 사용
- 보안, 성능 최적화가 완전하지 않을 수 있음
- 정식 구현은 메인 프로젝트의 API 문서 참조