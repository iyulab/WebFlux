# WebFlux 기본 사용법 샘플

이 샘플은 WebFlux SDK의 기본적인 사용법을 보여줍니다.

## 🎯 학습 목표

- WebFlux 의존성 주입 설정 방법
- 기본적인 문서 처리 방법
- 청킹 전략 사용 방법

## 🚀 실행 방법

```bash
# samples 디렉터리로 이동
cd samples/BasicUsage

# 프로젝트 실행
dotnet run
```

## 📋 주요 구성 요소

### Program.cs
- 메인 애플리케이션 진입점
- 의존성 주입 설정 예제
- WebFlux 서비스 사용 예제

### 의존성 주입 설정
```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// TODO: WebFlux 서비스 등록
// services.AddWebFlux();
```

## 🔧 기술 스택

- .NET 10.0
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- WebFlux SDK

## ⚠️ 참고사항

이 샘플은 학습 목적으로 작성되었으며, 프로덕션 코드의 완전한 예제가 아닙니다. WebFlux SDK의 전체 기능을 활용하려면 공식 문서를 참조하세요.

## 🔗 관련 링크

- [WebFlux 공식 문서](../../README.md)
- [WebFlux API 문서](../../docs/)
- [다른 샘플 예제](../)