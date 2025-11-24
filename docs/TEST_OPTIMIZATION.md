# 테스트 최적화 가이드

WebFlux 프로젝트의 테스트 실행 시간을 최적화하고, 로컬 개발과 CI/CD 환경에서 효율적인 테스트 전략을 제공합니다.

## 테스트 카테고리 분류

### 1. Unit Tests (기본, 빠름)
- **실행 시간**: <30초
- **목적**: 개별 컴포넌트 기능 검증
- **카테고리 속성**: 없음 (기본 테스트)
- **실행 범위**: 모든 환경 (로컬, CI)

**예시**:
```csharp
public class AutoChunkingStrategyTests
{
    [Fact]
    public void Name_ShouldBeAuto()
    {
        // 빠른 단위 테스트
    }
}
```

### 2. Performance Tests (중간 속도)
- **실행 시간**: 5-10분
- **목적**: 메모리 효율성, 대용량 문서 처리 검증
- **카테고리 속성**: `[Trait("Category", "Performance")]`
- **실행 범위**: 로컬 (선택적), CI (스케줄 실행)

**예시**:
```csharp
[Trait("Category", "Performance")]
[Trait("Category", "LargeDocument")]
public class LargeDocumentStabilityTests
{
    [Fact]
    public async Task MemoryOptimizedStrategy_ShouldHandleLargeDocument()
    {
        // 1MB+ 문서 처리 테스트
    }
}
```

### 3. LongRunning Tests (매우 느림)
- **실행 시간**: 10분 ~ 24시간
- **목적**: 장시간 안정성, 메모리 누수 감지
- **카테고리 속성**: `[Trait("Category", "LongRunning")]`
- **실행 범위**: 로컬 (수동), CI (수동 실행)

**예시**:
```csharp
[Trait("Category", "Performance")]
[Trait("Category", "LongRunning")]
public class LongRunningStabilityTests
{
    [Fact(Skip = "24시간 테스트 - 수동 실행 필요")]
    public async Task ContinuousProcessing_ShouldRemainStable_For24Hours()
    {
        // 24시간 연속 처리 테스트
    }

    [Fact]
    public async Task HighLoadProcessing_ShouldRemainStable_For10Minutes()
    {
        // 10분 고부하 테스트 (Skip 없음)
    }
}
```

## 로컬 테스트 실행

### 기본 실행 (빠른 테스트만)
```powershell
# PowerShell
./scripts/full-test.ps1

# 또는 dotnet CLI
dotnet test --filter "Category!=Performance&Category!=LongRunning"
```
**실행 시간**: ~30초
**포함**: Unit tests only

### Performance 테스트 포함
```powershell
./scripts/full-test.ps1 -IncludePerformance
```
**실행 시간**: ~5-10분
**포함**: Unit tests + Performance tests

### LongRunning 테스트 포함
```powershell
./scripts/full-test.ps1 -IncludeLongRunning
```
**실행 시간**: ~10-30분
**포함**: Unit tests + LongRunning tests (24시간 테스트는 Skip)

### 모든 테스트 실행
```powershell
./scripts/full-test.ps1 -IncludePerformance -IncludeLongRunning
```
**실행 시간**: ~15-40분
**포함**: 모든 테스트 (Skip된 것 제외)

### 빠른 테스트만 (명시적)
```powershell
./scripts/full-test.ps1 -FastOnly
```
**실행 시간**: ~30초
**포함**: Unit tests only

## GitHub Actions CI/CD 전략

### 1. 기본 CI (nuget-publish.yml)
```yaml
- name: 🧪 Run Fast Tests (excluding Performance and LongRunning)
  run: dotnet test --filter "Category!=Performance&Category!=LongRunning"
```
- **트리거**: 모든 push, PR
- **실행 시간**: ~30초
- **목적**: 빠른 피드백 루프

### 2. Performance Tests (performance-tests.yml)
```yaml
- name: ⚡ Run Performance Tests (excluding LongRunning)
  run: dotnet test --filter "Category=Performance&Category!=LongRunning"
```
- **트리거**:
  - 스케줄 (매일 새벽 3시 UTC)
  - main 브랜치 푸시
  - 수동 실행
- **실행 시간**: ~5-10분
- **목적**: 메모리 및 성능 검증

### 3. LongRunning Tests (performance-tests.yml)
```yaml
- name: 🕐 Run LongRunning Tests
  run: dotnet test --filter "Category=LongRunning"
```
- **트리거**: 수동 실행만 (`workflow_dispatch`)
- **실행 시간**: ~10-30분
- **목적**: 장시간 안정성 검증

## 테스트 필터링 전략

### xUnit Trait 사용
```csharp
// 테스트 클래스 레벨
[Trait("Category", "Performance")]
public class PerformanceTests { }

// 테스트 메서드 레벨
[Fact]
[Trait("Category", "Performance")]
[Trait("Category", "LongRunning")]
public async Task SlowTest() { }
```

### dotnet test 필터 구문
```bash
# 단일 카테고리 제외
dotnet test --filter "Category!=Performance"

# 다중 카테고리 제외 (AND)
dotnet test --filter "Category!=Performance&Category!=LongRunning"

# 특정 카테고리만 실행
dotnet test --filter "Category=Performance"

# 복합 조건 (Performance이지만 LongRunning은 제외)
dotnet test --filter "Category=Performance&Category!=LongRunning"
```

## 실행 시간 최적화 결과

### Before (최적화 전)
```
모든 테스트 실행: 228.7초 (CI에서 과도한 실행 시간)
- Unit tests: ~30초
- Performance tests: ~180초
- LongRunning tests: ~18초 (일부만 실행)
```

### After (최적화 후)
```
CI 기본 실행: ~30초 (87% 단축)
- Unit tests only: ~30초

성능 테스트 (스케줄): ~5-10분
- Performance tests (매일 자동)

장시간 테스트 (수동): ~10-30분
- LongRunning tests (필요시 수동 실행)
```

## 테스트 작성 가이드라인

### 1. 적절한 카테고리 부여
```csharp
// ✅ 빠른 단위 테스트 - 카테고리 없음
public class QuickTests
{
    [Fact]
    public void FastTest() { }
}

// ✅ 성능 테스트 - Performance 카테고리
[Trait("Category", "Performance")]
public class PerformanceTests
{
    [Fact]
    public async Task MediumSpeedTest() { }
}

// ✅ 장시간 테스트 - LongRunning 카테고리
[Trait("Category", "Performance")]
[Trait("Category", "LongRunning")]
public class LongRunningTests
{
    [Fact]
    public async Task SlowTest() { }
}
```

### 2. Skip 속성 사용
```csharp
// 24시간 테스트 - Skip으로 기본 실행 방지
[Fact(Skip = "24시간 테스트 - 수동 실행 필요")]
public async Task ContinuousProcessing_24Hours()
{
    // 매우 긴 테스트
}

// 10분 테스트 - Skip 없이 LongRunning 카테고리만
[Fact]
[Trait("Category", "LongRunning")]
public async Task HighLoadProcessing_10Minutes()
{
    // 중간 길이 테스트 (필터로 제어)
}
```

### 3. 테스트 격리 및 정리
```csharp
[Fact]
public async Task MemoryTest()
{
    // Arrange: 초기 메모리 정리
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var initialMemory = GC.GetTotalMemory(false);

    // Act: 테스트 실행

    // Assert: 메모리 검증
    GC.Collect();
    var finalMemory = GC.GetTotalMemory(false);

    // Cleanup은 자동 (xUnit이 처리)
}
```

## 문제 해결

### Q1: CI에서 테스트가 너무 오래 걸림
**A**: 기본 CI는 빠른 테스트만 실행하도록 설정되어 있습니다.
```bash
# nuget-publish.yml에서 자동으로 적용
--filter "Category!=Performance&Category!=LongRunning"
```

### Q2: 로컬에서 Performance 테스트를 실행하고 싶음
**A**: `-IncludePerformance` 플래그 사용
```powershell
./scripts/full-test.ps1 -IncludePerformance
```

### Q3: 24시간 테스트를 실행하고 싶음
**A**: Skip 속성을 제거하고 수동 실행
```csharp
// Skip 제거
[Fact] // (Skip = "..." 제거)
public async Task ContinuousProcessing_24Hours() { }
```
```powershell
# 특정 테스트만 실행
dotnet test --filter "FullyQualifiedName~ContinuousProcessing_24Hours"
```

### Q4: GitHub Actions에서 Performance 테스트 수동 실행
**A**: Performance Tests 워크플로우 수동 트리거
1. GitHub Actions 탭으로 이동
2. "Performance and LongRunning Tests" 워크플로우 선택
3. "Run workflow" 클릭

## 모범 사례

### ✅ DO
- Unit 테스트는 카테고리 없이 작성 (기본 실행)
- Performance 테스트는 `[Trait("Category", "Performance")]` 추가
- LongRunning 테스트는 두 카테고리 모두 추가
- 24시간 이상 테스트는 `Skip` 속성 추가
- 로컬 개발 시 `-FastOnly` 또는 기본 실행 사용

### ❌ DON'T
- 모든 테스트에 카테고리 추가 (불필요)
- CI에서 LongRunning 테스트 자동 실행
- Performance 테스트 없이 메모리 최적화 주장
- Skip된 테스트를 CI에서 실행

## 참고 자료
- [xUnit Trait Documentation](https://xunit.net/docs/comparisons#attributes)
- [dotnet test Filtering](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests)
- [GitHub Actions Manual Triggers](https://docs.github.com/en/actions/using-workflows/manually-running-a-workflow)
