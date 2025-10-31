#!/usr/bin/env pwsh
# WebFlux 전체 테스트 스크립트
# 빌드, 테스트, 커버리지 리포트를 순차적으로 실행

param(
    [switch]$SkipBuild,
    [switch]$Coverage,
    [switch]$Verbose,
    [string]$Filter = "",
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$solutionPath = Join-Path $rootDir "src\WebFlux.sln"
$testProject = Join-Path $rootDir "tests\WebFlux.Tests\WebFlux.Tests.csproj"
$coverageDir = Join-Path $rootDir "coverage"

# 색상 출력 함수
function Write-Step {
    param([string]$Message)
    Write-Host "`n===================================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "===================================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

# 시작 시간 기록
$startTime = Get-Date

Write-Host @"

╔══════════════════════════════════════════════════╗
║                                                  ║
║           WebFlux Full Test Suite                ║
║                                                  ║
╚══════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

# 1. 빌드
if (-not $SkipBuild) {
    Write-Step "Step 1: Building Solution"

    $buildArgs = @(
        "build",
        $solutionPath,
        "--configuration", $Configuration,
        "--no-incremental"
    )

    if ($Verbose) {
        $buildArgs += "--verbosity", "detailed"
    }

    try {
        & dotnet @buildArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
        Write-Success "Build completed successfully"
    }
    catch {
        Write-Error "Build failed: $_"
        exit 1
    }
}
else {
    Write-Warning "Skipping build (--SkipBuild specified)"
}

# 2. 테스트 실행
Write-Step "Step 2: Running Tests"

$testArgs = @(
    "test",
    $testProject,
    "--configuration", $Configuration,
    "--no-build",
    "--logger", "console;verbosity=normal"
)

if ($Filter) {
    $testArgs += "--filter", $Filter
    Write-Host "Filter: $Filter" -ForegroundColor Yellow
}

if ($Coverage) {
    Write-Host "Coverage collection enabled" -ForegroundColor Yellow

    # coverlet.collector 사용
    $testArgs += "--collect", "XPlat Code Coverage"
    $testArgs += "--results-directory", $coverageDir

    # 추가 coverlet 설정
    $testArgs += "--"
    $testArgs += "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover,cobertura,json,lcov"
    $testArgs += "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile=**/*.g.cs,**/*.Designer.cs"
}

try {
    & dotnet @testArgs
    $testExitCode = $LASTEXITCODE

    if ($testExitCode -ne 0) {
        Write-Error "Tests failed with exit code $testExitCode"

        # 테스트 실패 상세 정보 표시
        Write-Host "`nFor more details, check:" -ForegroundColor Yellow
        Write-Host "  - Test output above" -ForegroundColor Yellow
        Write-Host "  - dotnet test --logger 'console;verbosity=detailed'" -ForegroundColor Yellow

        exit $testExitCode
    }

    Write-Success "All tests passed"
}
catch {
    Write-Error "Test execution failed: $_"
    exit 1
}

# 3. 커버리지 리포트 생성 (선택적)
if ($Coverage) {
    Write-Step "Step 3: Generating Coverage Report"

    # ReportGenerator 확인 및 설치
    $reportGeneratorPath = (dotnet tool list -g | Select-String "reportgenerator").ToString()

    if (-not $reportGeneratorPath) {
        Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }

    # 커버리지 파일 찾기
    $coverageFiles = Get-ChildItem -Path $coverageDir -Filter "coverage.cobertura.xml" -Recurse

    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found in $coverageDir"
    }
    else {
        $reportDir = Join-Path $coverageDir "report"

        Write-Host "Generating HTML report to: $reportDir" -ForegroundColor Cyan

        $coverageFilePaths = $coverageFiles | ForEach-Object { $_.FullName }
        $coverageFileArg = $coverageFilePaths -join ";"

        & reportgenerator `
            "-reports:$coverageFileArg" `
            "-targetdir:$reportDir" `
            "-reporttypes:Html;HtmlSummary;Badges;TextSummary" `
            "-historydir:$reportDir\history"

        if ($LASTEXITCODE -eq 0) {
            Write-Success "Coverage report generated"

            # 커버리지 요약 출력
            $summaryFile = Join-Path $reportDir "Summary.txt"
            if (Test-Path $summaryFile) {
                Write-Host "`n" -NoNewline
                Get-Content $summaryFile | Write-Host
            }

            # HTML 리포트 열기
            $htmlReport = Join-Path $reportDir "index.html"
            Write-Host "`nHTML Report: $htmlReport" -ForegroundColor Green
            Write-Host "Open with: start $htmlReport" -ForegroundColor Cyan
        }
        else {
            Write-Warning "Report generation had issues (exit code: $LASTEXITCODE)"
        }
    }
}

# 4. 최종 요약
Write-Step "Test Summary"

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host @"

Configuration: $Configuration
Test Filter:   $(if ($Filter) { $Filter } else { "(none)" })
Coverage:      $(if ($Coverage) { "Enabled" } else { "Disabled" })
Duration:      $($duration.ToString("mm\:ss"))

"@ -ForegroundColor White

Write-Success "Full test suite completed successfully!"

if ($Coverage) {
    Write-Host "`nCoverage report location: $coverageDir\report\index.html" -ForegroundColor Cyan
}

Write-Host "`n"
exit 0
