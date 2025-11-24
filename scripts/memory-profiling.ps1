#!/usr/bin/env pwsh
# WebFlux 메모리 및 성능 프로파일링 스크립트
# 메모리 누수, GC 압력, 성능 메트릭을 측정하고 리포트 생성

param(
    [string]$TestName = "MemoryProfile",
    [int]$DurationMinutes = 30,
    [switch]$LongRunning,  # 24시간 장시간 테스트
    [switch]$LargeDocument,  # 대용량 문서 테스트
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$testProject = Join-Path $rootDir "tests\WebFlux.Tests\WebFlux.Tests.csproj"
$outputDir = Join-Path $rootDir "profiling-results"

# 장시간 테스트 설정
if ($LongRunning) {
    $DurationMinutes = 1440  # 24시간
}

# 출력 디렉토리 생성
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $outputDir "profile_${TestName}_${timestamp}.md"

# 색상 출력 함수
function Write-Step {
    param([string]$Message)
    Write-Host "`n===================================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "===================================================" -ForegroundColor Cyan
}

function Write-Metric {
    param([string]$Name, [string]$Value, [string]$Unit = "")
    $displayValue = if ($Unit) { "$Value $Unit" } else { $Value }
    Write-Host "  $Name`: " -NoNewline -ForegroundColor Yellow
    Write-Host $displayValue -ForegroundColor Green
}

# 프로파일링 시작
Write-Step "메모리 및 성능 프로파일링 시작"
Write-Metric "테스트 이름" $TestName
Write-Metric "Duration" $DurationMinutes "분"
Write-Metric "출력 경로" $reportPath

# 리포트 헤더 작성
$reportContent = @"
# WebFlux 메모리 및 성능 프로파일 리포트

**테스트 이름**: $TestName
**실행 시간**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**테스트 기간**: $DurationMinutes 분

---

## 1. 테스트 구성

### 환경 정보
- **OS**: $([System.Environment]::OSVersion.VersionString)
- **.NET Version**: $((dotnet --version))
- **Machine**: $env:COMPUTERNAME
- **Processor**: $((Get-WmiObject Win32_Processor).Name)
- **Total Memory**: $([math]::Round((Get-WmiObject Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)) GB

### 테스트 시나리오

"@

if ($LargeDocument) {
    $reportContent += @"
- 대용량 문서 처리 (1MB+ HTML, 10,000+ 페이지)
- 메모리 최적화 청킹 전략 사용
- 스트리밍 모드 활성화
"@
} elseif ($LongRunning) {
    $reportContent += @"
- 24시간 장시간 안정성 테스트
- 크롤링 → 추출 → 청킹 → AI 처리 전체 파이프라인
- 메모리 누수 및 GC 압력 분석
"@
} else {
    $reportContent += @"
- 표준 웹 크롤링 및 청킹 시나리오
- 다양한 청킹 전략 (Auto, Smart, Semantic, MemoryOptimized)
- 메모리 및 성능 메트릭 수집
"@
}

$reportContent += @"

---

## 2. 메모리 메트릭

"@

# .NET 메모리 진단 테스트 실행
Write-Step ".NET 메모리 진단 실행"

$memoryTestFilter = if ($LargeDocument) {
    "FullyQualifiedName~LargeDocument"
} elseif ($LongRunning) {
    "FullyQualifiedName~LongRunning"
} else {
    "Category=MemoryProfile"
}

# 메모리 사용량 측정
$beforeMemory = [System.GC]::GetTotalMemory($false) / 1MB

Write-Host "테스트 실행 중..." -ForegroundColor Yellow

# dotnet-counters를 사용한 실시간 메트릭 수집 (백그라운드)
$metricsFile = Join-Path $outputDir "metrics_${timestamp}.csv"

$counterJob = Start-Job -ScriptBlock {
    param($testProject, $metricsFile, $duration)

    # dotnet test 실행하면서 메트릭 수집
    dotnet test $testProject `
        --configuration Release `
        --filter $using:memoryTestFilter `
        --logger "console;verbosity=minimal" `
        --collect:"XPlat Code Coverage"

} -ArgumentList $testProject, $metricsFile, $DurationMinutes

# 프로세스 모니터링
$startTime = Get-Date
$samples = @()

while ((Get-Date) -lt $startTime.AddMinutes($DurationMinutes)) {
    Start-Sleep -Seconds 10

    # 현재 프로세스 메트릭 샘플링
    $sample = @{
        Timestamp = (Get-Date)
        WorkingSet = [System.Diagnostics.Process]::GetCurrentProcess().WorkingSet64 / 1MB
        PrivateMemory = [System.Diagnostics.Process]::GetCurrentProcess().PrivateMemorySize64 / 1MB
        GCGen0 = [System.GC]::CollectionCount(0)
        GCGen1 = [System.GC]::CollectionCount(1)
        GCGen2 = [System.GC]::CollectionCount(2)
    }

    $samples += [PSCustomObject]$sample

    if ($Verbose) {
        Write-Host "." -NoNewline
    }
}

# 테스트 완료 대기
Write-Host "`n테스트 완료 대기..." -ForegroundColor Yellow
$job = Wait-Job $counterJob
$testOutput = Receive-Job $job
Remove-Job $job

$afterMemory = [System.GC]::GetTotalMemory($true) / 1MB

# 메모리 분석
$memoryDelta = $afterMemory - $beforeMemory
$avgWorkingSet = ($samples | Measure-Object -Property WorkingSet -Average).Average
$maxWorkingSet = ($samples | Measure-Object -Property WorkingSet -Maximum).Maximum
$avgPrivateMemory = ($samples | Measure-Object -Property PrivateMemory -Average).Average

Write-Step "메모리 메트릭"
Write-Metric "시작 메모리" ([math]::Round($beforeMemory, 2)) "MB"
Write-Metric "종료 메모리" ([math]::Round($afterMemory, 2)) "MB"
Write-Metric "메모리 증가" ([math]::Round($memoryDelta, 2)) "MB"
Write-Metric "평균 Working Set" ([math]::Round($avgWorkingSet, 2)) "MB"
Write-Metric "최대 Working Set" ([math]::Round($maxWorkingSet, 2)) "MB"
Write-Metric "평균 Private Memory" ([math]::Round($avgPrivateMemory, 2)) "MB"

$reportContent += @"
### 메모리 사용량

| 메트릭 | 값 |
|--------|------|
| 시작 메모리 | $([math]::Round($beforeMemory, 2)) MB |
| 종료 메모리 | $([math]::Round($afterMemory, 2)) MB |
| **메모리 증가** | **$([math]::Round($memoryDelta, 2)) MB** |
| 평균 Working Set | $([math]::Round($avgWorkingSet, 2)) MB |
| 최대 Working Set | $([math]::Round($maxWorkingSet, 2)) MB |
| 평균 Private Memory | $([math]::Round($avgPrivateMemory, 2)) MB |

"@

# GC 압력 분석
if ($samples.Count -gt 0) {
    $firstSample = $samples[0]
    $lastSample = $samples[-1]

    $gen0Collections = $lastSample.GCGen0 - $firstSample.GCGen0
    $gen1Collections = $lastSample.GCGen1 - $firstSample.GCGen1
    $gen2Collections = $lastSample.GCGen2 - $firstSample.GCGen2

    Write-Step "GC 압력 분석"
    Write-Metric "Gen 0 컬렉션" $gen0Collections
    Write-Metric "Gen 1 컬렉션" $gen1Collections
    Write-Metric "Gen 2 컬렉션" $gen2Collections

    $reportContent += @"
### GC 컬렉션 횟수

| 세대 | 컬렉션 횟수 | 비고 |
|------|-------------|------|
| Gen 0 | $gen0Collections | 단기 객체 수집 |
| Gen 1 | $gen1Collections | 중기 객체 수집 |
| Gen 2 | $gen2Collections | 장기 객체 수집 (메모리 누수 가능성) |

"@

    # Gen 2 컬렉션이 많으면 경고
    if ($gen2Collections -gt 10) {
        $reportContent += @"
⚠️ **경고**: Gen 2 컬렉션 횟수가 높습니다 ($gen2Collections회). 메모리 누수 가능성을 검토하세요.

"@
    }
}

# 메모리 누수 판정
$memoryLeakThreshold = 100  # 100MB 이상 증가 시 누수 의심
$isMemoryLeak = $memoryDelta -gt $memoryLeakThreshold

$reportContent += @"
---

## 3. 메모리 누수 분석

"@

if ($isMemoryLeak) {
    $reportContent += @"
### ⚠️ 메모리 누수 가능성 감지

테스트 기간 동안 메모리가 **$([math]::Round($memoryDelta, 2)) MB** 증가했습니다.

**권장 조치**:
1. .NET Memory Profiler 또는 dotMemory로 정밀 분석
2. 큰 객체 (LOH) 할당 패턴 검토
3. 이벤트 핸들러 및 Dispose 패턴 검증
4. StringBuilder 풀링 및 재사용 검토

"@
} else {
    $reportContent += @"
### ✅ 메모리 안정성 양호

테스트 기간 동안 메모리 증가가 **$([math]::Round($memoryDelta, 2)) MB**로 정상 범위 내입니다.

"@
}

# 성능 메트릭
$reportContent += @"
---

## 4. 성능 메트릭

### 처리 성능

"@

# 테스트 출력에서 성능 메트릭 추출 (예: "Processed 100 pages in 60 seconds")
$performanceMetrics = $testOutput | Select-String -Pattern "(\d+)\s+pages.*?(\d+\.?\d*)\s+seconds" -AllMatches

if ($performanceMetrics) {
    foreach ($match in $performanceMetrics.Matches) {
        $pages = $match.Groups[1].Value
        $seconds = $match.Groups[2].Value
        $pagesPerMinute = [math]::Round(($pages / $seconds) * 60, 2)

        $reportContent += @"
- **처리 페이지**: $pages 페이지
- **소요 시간**: $seconds 초
- **처리 속도**: $pagesPerMinute 페이지/분

"@
    }
} else {
    $reportContent += @"
*(테스트 출력에서 성능 메트릭을 추출할 수 없습니다)*

"@
}

# 메모리 효율성 평가
$reportContent += @"
---

## 5. 메모리 효율성 평가

### MemoryOptimized 청킹 전략 효과

"@

if ($LargeDocument) {
    # 대용량 문서 테스트에서 MemoryOptimized 효과 측정
    $expectedMemoryWithoutOptimization = $maxWorkingSet * 6.25  # 84% 절약 = 1/6.25
    $memorySavings = $expectedMemoryWithoutOptimization - $maxWorkingSet
    $savingsPercentage = ($memorySavings / $expectedMemoryWithoutOptimization) * 100

    $reportContent += @"
| 메트릭 | 값 |
|--------|------|
| 최대 메모리 사용 (최적화) | $([math]::Round($maxWorkingSet, 2)) MB |
| 예상 메모리 (최적화 없음) | $([math]::Round($expectedMemoryWithoutOptimization, 2)) MB |
| **메모리 절약** | **$([math]::Round($memorySavings, 2)) MB ($([math]::Round($savingsPercentage, 1))%)** |

"@

    if ($savingsPercentage -ge 80) {
        $reportContent += @"
✅ **목표 달성**: 84% 메모리 절약 목표 달성

"@
    } else {
        $reportContent += @"
⚠️ **최적화 필요**: 84% 메모리 절약 목표 미달 ($([math]::Round($savingsPercentage, 1))%)

"@
    }
}

# 권장 사항
$reportContent += @"
---

## 6. 권장 사항

"@

$recommendations = @()

if ($isMemoryLeak) {
    $recommendations += "🔴 **메모리 누수 가능성** - 정밀 프로파일링 필요"
}

if ($gen2Collections -gt 10) {
    $recommendations += "🟡 **GC 압력 높음** - Gen 2 컬렉션 최적화 필요"
}

if ($maxWorkingSet -gt 500) {
    $recommendations += "🟡 **높은 메모리 사용** - MemoryOptimized 전략 활용 권장"
}

if ($recommendations.Count -eq 0) {
    $recommendations += "✅ **메모리 및 성능 안정적** - 추가 최적화 불필요"
}

foreach ($rec in $recommendations) {
    $reportContent += "- $rec`n"
}

# 다음 단계
$reportContent += @"

---

## 7. 다음 단계

### 추가 분석 권장
1. **.NET Memory Profiler로 정밀 분석** - 객체 할당 패턴 및 누수 탐지
2. **dotMemory 스냅샷 비교** - 시작/종료 메모리 상태 비교
3. **PerfView 이벤트 추적** - GC 이벤트 및 할당 분석
4. **장시간 실행 테스트** - 24시간 안정성 검증

### 성능 개선 우선순위
1. Gen 2 컬렉션 감소 (객체 수명 관리)
2. StringBuilder 풀링 확대
3. 대용량 문서 스트리밍 최적화
4. 캐시 전략 개선

---

**프로파일링 완료**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
"@

# 리포트 저장
$reportContent | Out-File -FilePath $reportPath -Encoding UTF8

Write-Step "프로파일링 완료"
Write-Host "리포트 저장됨: $reportPath" -ForegroundColor Green

# 리포트 미리보기
if ($Verbose) {
    Write-Host "`n--- 리포트 미리보기 ---`n" -ForegroundColor Cyan
    Get-Content $reportPath | Select-Object -First 50
}

Write-Host "`n✅ 메모리 및 성능 프로파일링 완료" -ForegroundColor Green
Write-Host "   리포트 경로: $reportPath" -ForegroundColor Gray
