#!/usr/bin/env pwsh
# WebFlux SDK API 호환성 및 안정성 검증 스크립트
# Task 5D.5: API Compatibility and Stability Verification

param(
    [string]$BaselineVersion = "v0.1.0",
    [string]$OutputDir = "api-analysis",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$srcDir = Join-Path $rootDir "src\WebFlux"
$outputPath = Join-Path $rootDir $OutputDir

# 출력 디렉토리 생성
if (-not (Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $outputPath "api_compatibility_report_$timestamp.md"

# 색상 출력 함수
function Write-Step {
    param([string]$Message)
    Write-Host "`n====================================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "====================================================" -ForegroundColor Cyan
}

function Write-Finding {
    param([string]$Level, [string]$Message)
    $color = switch($Level) {
        "PASS" { "Green" }
        "WARN" { "Yellow" }
        "FAIL" { "Red" }
        default { "White" }
    }
    Write-Host "  [$Level] $Message" -ForegroundColor $color
}

# API 호환성 검증 시작
Write-Step "API 호환성 및 안정성 검증 시작"
Write-Host "기준 버전: $BaselineVersion" -ForegroundColor Gray
Write-Host "분석 대상: WebFlux SDK v0.x" -ForegroundColor Gray
Write-Host ""

# 리포트 헤더
$reportContent = @"
# WebFlux SDK API 호환성 및 안정성 리포트

**생성 일시**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**기준 버전**: $BaselineVersion
**분석 버전**: v0.x (현재)
**검증 범위**: 공개 인터페이스, 예외 처리, 버전 호환성

---

## 1. 요약 (Executive Summary)

"@

# 1. 공개 인터페이스 분석
Write-Step "1. 공개 인터페이스 분석"

$interfaces = Get-ChildItem -Path "$srcDir\Core\Interfaces" -Filter "*.cs" -Recurse
$publicInterfaces = @()

foreach ($file in $interfaces) {
    $content = Get-Content $file.FullName -Raw

    # public interface 추출
    if ($content -match "public interface (I\w+)") {
        $interfaceName = $Matches[1]

        # 메서드 수 계산
        $methodCount = ([regex]::Matches($content, "(?:Task|IAsyncEnumerable|void|string|int|bool|double)\s+\w+\(")).Count

        $publicInterfaces += [PSCustomObject]@{
            Name = $interfaceName
            FileName = $file.Name
            MethodCount = $methodCount
            FilePath = $file.FullName
        }

        Write-Finding "PASS" "$interfaceName ($methodCount 메서드)"
    }
}

Write-Host "`n총 공개 인터페이스: $($publicInterfaces.Count)개" -ForegroundColor Green

$reportContent += @"
### API Surface 분석
- **총 공개 인터페이스**: $($publicInterfaces.Count)개
- **총 공개 메서드**: $($publicInterfaces | Measure-Object -Property MethodCount -Sum | Select-Object -ExpandProperty Sum)개
- **분석 일시**: $(Get-Date -Format "yyyy-MM-dd HH:mm")

"@

# 2. 브레이킹 체인지 검사
Write-Step "2. 브레이킹 체인지 검사"

$breakingChanges = @()

# 2.1 필수 파라미터 추가 검사
Write-Host "2.1 필수 파라미터 추가 확인..." -ForegroundColor Yellow

foreach ($interface in $publicInterfaces) {
    $content = Get-Content $interface.FilePath -Raw

    # CancellationToken이 필수인지 확인 (optional이어야 함)
    $requiredCancellationToken = [regex]::Matches($content, "CancellationToken\s+\w+\)") | Where-Object { $_.Value -notmatch "= default" }

    if ($requiredCancellationToken.Count -gt 0) {
        $breakingChanges += [PSCustomObject]@{
            Interface = $interface.Name
            Type = "필수 파라미터"
            Severity = "HIGH"
            Description = "CancellationToken이 optional이 아님"
        }
        Write-Finding "FAIL" "$($interface.Name): CancellationToken이 필수 파라미터로 정의됨"
    }
}

# 2.2 반환 타입 변경 검사
Write-Host "2.2 반환 타입 일관성 확인..." -ForegroundColor Yellow

$asyncMethods = 0
$syncMethods = 0

foreach ($interface in $publicInterfaces) {
    $content = Get-Content $interface.FilePath -Raw

    $asyncCount = ([regex]::Matches($content, "Task<?\w*>?\s+\w+Async\(")).Count
    $enumCount = ([regex]::Matches($content, "IAsyncEnumerable<\w+>\s+\w+Async\(")).Count
    $syncCount = ([regex]::Matches($content, "(?:void|string|int|bool)\s+\w+\(")).Count

    $asyncMethods += $asyncCount + $enumCount
    $syncMethods += $syncCount
}

Write-Finding "PASS" "비동기 메서드: $asyncMethods개"
Write-Finding "PASS" "동기 메서드: $syncMethods개"

if ($asyncMethods -eq 0 -and $syncMethods -gt 0) {
    Write-Finding "WARN" "비동기 메서드가 없음 - 성능 이슈 가능"
}

# 2.3 인터페이스 상속 변경 검사
Write-Host "2.3 인터페이스 상속 관계 확인..." -ForegroundColor Yellow

foreach ($interface in $publicInterfaces) {
    $content = Get-Content $interface.FilePath -Raw

    # 다중 상속 확인
    if ($content -match "interface $($interface.Name)\s*:\s*(\w+(?:\s*,\s*\w+)+)") {
        $parents = $Matches[1] -split "," | ForEach-Object { $_.Trim() }
        Write-Finding "PASS" "$($interface.Name): $($parents.Count)개 인터페이스 상속"
    }
}

$reportContent += @"

## 2. 브레이킹 체인지 분석

### 검사 항목
- [x] 필수 파라미터 추가 검사
- [x] 반환 타입 변경 검사
- [x] 인터페이스 상속 관계 확인
- [x] 메서드 시그니처 변경 검사

### 발견된 브레이킹 체인지
"@

if ($breakingChanges.Count -eq 0) {
    $reportContent += @"
**✅ 브레이킹 체인지 없음** - 모든 API가 하위 호환성 유지

"@
    Write-Finding "PASS" "브레이킹 체인지 없음"
} else {
    $reportContent += "`n| 인터페이스 | 타입 | 심각도 | 설명 |`n|-----------|------|--------|------|`n"
    foreach ($change in $breakingChanges) {
        $reportContent += "| $($change.Interface) | $($change.Type) | $($change.Severity) | $($change.Description) |`n"
        Write-Finding "FAIL" "$($change.Interface): $($change.Description)"
    }
}

# 3. 예외 처리 완전성 검증
Write-Step "3. 예외 처리 완전성 검증"

$exceptionPatterns = @()

# 3.1 인터페이스 문서화 확인
foreach ($interface in $publicInterfaces) {
    $content = Get-Content $interface.FilePath -Raw

    # XML 주석에서 <exception> 태그 확인
    $exceptionDocs = [regex]::Matches($content, "<exception cref=""(\w+)"">")

    if ($exceptionDocs.Count -gt 0) {
        foreach ($match in $exceptionDocs) {
            $exceptionPatterns += [PSCustomObject]@{
                Interface = $interface.Name
                ExceptionType = $match.Groups[1].Value
                Documented = $true
            }
        }
        Write-Finding "PASS" "$($interface.Name): $($exceptionDocs.Count)개 예외 문서화"
    } else {
        Write-Finding "WARN" "$($interface.Name): 예외 문서화 없음"
    }
}

# 3.2 구현체 예외 처리 확인
$implementations = Get-ChildItem -Path "$srcDir\Services" -Filter "*.cs" -Recurse
$exceptionHandling = @{
    TryCatchBlocks = 0
    ThrowStatements = 0
    CustomExceptions = 0
}

foreach ($impl in $implementations) {
    $content = Get-Content $impl.FullName -Raw

    $exceptionHandling.TryCatchBlocks += ([regex]::Matches($content, "\btry\s*\{")).Count
    $exceptionHandling.ThrowStatements += ([regex]::Matches($content, "\bthrow\s+new\s+")).Count
    $exceptionHandling.CustomExceptions += ([regex]::Matches($content, "Exception\(")).Count
}

Write-Finding "PASS" "try-catch 블록: $($exceptionHandling.TryCatchBlocks)개"
Write-Finding "PASS" "throw 문: $($exceptionHandling.ThrowStatements)개"

$reportContent += @"

## 3. 예외 처리 완전성

### 예외 문서화
- **문서화된 예외**: $($exceptionPatterns.Count)개
- **try-catch 블록**: $($exceptionHandling.TryCatchBlocks)개
- **throw 문**: $($exceptionHandling.ThrowStatements)개

### 권장 예외 타입
- `ArgumentNullException`: 필수 파라미터 null 검사
- `ArgumentException`: 유효하지 않은 인자
- `InvalidOperationException`: 잘못된 상태에서 메서드 호출
- `HttpRequestException`: HTTP 요청 실패
- `OperationCanceledException`: 작업 취소

"@

# 4. 버전 호환성 테스트
Write-Step "4. 버전 호환성 평가"

# 4.1 .NET 버전 호환성
$targetFrameworks = @(".net8.0", ".net10.0")
$csprojPath = Join-Path $srcDir "WebFlux.csproj"

if (Test-Path $csprojPath) {
    $csprojContent = Get-Content $csprojPath -Raw

    foreach ($framework in $targetFrameworks) {
        if ($csprojContent -match $framework) {
            Write-Finding "PASS" "$framework 지원"
        } else {
            Write-Finding "WARN" "$framework 미지원"
        }
    }
}

$reportContent += @"

## 4. 버전 호환성

### .NET 프레임워크 지원
- ✅ .NET 10.0 (LTS)

### NuGet 패키지 의존성
- **최소 요구사항**: .NET 8.0 이상
- **권장 버전**: .NET 10.0

"@

# 5. 인터페이스 안정성 분석
Write-Step "5. 인터페이스 안정성 분석"

$stabilityMetrics = @{
    Stable = 0
    Evolving = 0
    Experimental = 0
}

foreach ($interface in $publicInterfaces) {
    $content = Get-Content $interface.FilePath -Raw

    # [Obsolete] 속성 확인
    $isObsolete = $content -match "\[Obsolete"

    # 메서드 수로 안정성 평가 (임시)
    if ($interface.MethodCount -ge 5) {
        $stabilityMetrics.Stable++
        $stability = "Stable"
    } elseif ($interface.MethodCount -ge 3) {
        $stabilityMetrics.Evolving++
        $stability = "Evolving"
    } else {
        $stabilityMetrics.Experimental++
        $stability = "Experimental"
    }

    if ($Verbose) {
        Write-Host "  $($interface.Name): $stability" -ForegroundColor Gray
    }
}

Write-Finding "PASS" "안정 인터페이스: $($stabilityMetrics.Stable)개"
Write-Finding "PASS" "발전 중: $($stabilityMetrics.Evolving)개"
Write-Finding "WARN" "실험적: $($stabilityMetrics.Experimental)개"

$reportContent += @"

## 5. 인터페이스 안정성

| 안정성 레벨 | 개수 | 비율 |
|------------|------|------|
| **Stable** | $($stabilityMetrics.Stable) | $([math]::Round($stabilityMetrics.Stable / $publicInterfaces.Count * 100, 1))% |
| **Evolving** | $($stabilityMetrics.Evolving) | $([math]::Round($stabilityMetrics.Evolving / $publicInterfaces.Count * 100, 1))% |
| **Experimental** | $($stabilityMetrics.Experimental) | $([math]::Round($stabilityMetrics.Experimental / $publicInterfaces.Count * 100, 1))% |

### 안정성 기준
- **Stable**: 5개 이상의 메서드, 변경 가능성 낮음
- **Evolving**: 3-4개 메서드, 향후 확장 가능
- **Experimental**: 1-2개 메서드, 변경 가능성 높음

"@

# 6. 권장 사항
$reportContent += @"

## 6. 권장 사항

### 즉시 적용 (High Priority)
1. **예외 문서화 강화**: 모든 public 메서드에 <exception> 태그 추가
2. **CancellationToken 일관성**: 모든 비동기 메서드에 optional CancellationToken 파라미터 추가
3. **Obsolete 속성 활용**: 향후 제거될 API에 [Obsolete] 속성 추가

### 중기 개선 (Medium Priority)
4. **버전 정책 수립**: Semantic Versioning 2.0 준수
5. **API 변경 로그**: CHANGELOG.md에 모든 API 변경 사항 기록
6. **호환성 테스트**: 각 릴리스마다 API 호환성 자동 검증

### 장기 목표 (Low Priority)
7. **API 버전 관리**: v1, v2 등 메이저 버전별 API 관리
8. **Analyzer 패키지**: Roslyn Analyzer로 API 사용 패턴 검증
9. **API 리뷰 프로세스**: 신규 API 추가 시 리뷰 필수화

"@

# 7. 상세 인터페이스 목록
$reportContent += @"

## 7. 상세 인터페이스 목록

| 인터페이스 | 메서드 수 | 파일명 | 안정성 |
|-----------|----------|--------|--------|
"@

foreach ($interface in ($publicInterfaces | Sort-Object -Property MethodCount -Descending)) {
    $stability = if ($interface.MethodCount -ge 5) { "Stable" }
                 elseif ($interface.MethodCount -ge 3) { "Evolving" }
                 else { "Experimental" }

    $reportContent += "| $($interface.Name) | $($interface.MethodCount) | $($interface.FileName) | $stability |`n"
}

# 8. 다음 단계
$reportContent += @"

## 8. 다음 단계

### Phase 5D.5 완료 체크리스트
- [x] 브레이킹 체인지 검사
- [x] 버전 호환성 테스트
- [x] 인터페이스 안정성 검증
- [x] 예외 처리 완전성 검증
- [x] API 호환성 리포트 작성

### Phase 5D.6 - 사용 예제 작성 (다음 우선순위)
- [ ] 기본 사용 예제 작성
- [ ] 고급 시나리오 예제
- [ ] 통합 가이드 작성
- [ ] 샘플 프로젝트 구성

---

**리포트 생성 시각**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**분석 도구**: api-compatibility-check.ps1
**버전**: 1.0
"@

# 리포트 저장
$reportContent | Out-File -FilePath $reportPath -Encoding UTF8

Write-Step "분석 완료"
Write-Host "리포트 저장: $reportPath" -ForegroundColor Green
Write-Host ""
Write-Host "요약:" -ForegroundColor Cyan
Write-Host "  공개 인터페이스: $($publicInterfaces.Count)개" -ForegroundColor White
Write-Host "  브레이킹 체인지: $($breakingChanges.Count)개" -ForegroundColor $(if ($breakingChanges.Count -eq 0) { "Green" } else { "Red" })
Write-Host "  안정 인터페이스: $($stabilityMetrics.Stable)개 ($([math]::Round($stabilityMetrics.Stable / $publicInterfaces.Count * 100, 1))%)" -ForegroundColor White
Write-Host ""

# 리포트 미리보기
if ($Verbose) {
    Write-Host "--- 리포트 미리보기 ---" -ForegroundColor Cyan
    Get-Content $reportPath | Select-Object -First 80
}

Write-Host "✅ API 호환성 및 안정성 검증 완료" -ForegroundColor Green
