# WebFlux 샘플 프로젝트

WebFlux SDK 사용 예제 모음입니다.

## 샘플 목록

### BasicUsage
기본 사용법 예제 - WebFlux SDK의 핵심 기능 시연

```bash
cd BasicUsage
dotnet run
```

### SimpleOpenAITest
OpenAI 통합 예제 - 실제 API를 사용한 웹 콘텐츠 처리

```bash
cd SimpleOpenAITest
dotnet run
```

## 빌드

```bash
dotnet build WebFluxSamples.slnx
```

## 주의사항

⚠️ 샘플 코드는 학습 및 테스트 목적으로만 사용하세요
- CI/CD 파이프라인에서 제외됨
- 프로덕션 품질 기준 미적용
- API 키가 필요한 경우 .env.local 파일 설정 필요
