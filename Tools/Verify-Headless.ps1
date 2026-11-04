<#
.SYNOPSIS
  Headless verification on a mirror of the project (the real project may be open in the Unity editor).
  Mirrors Assets, Packages, ProjectSettings to D:\UnityVerify\AcousticChamber26, then runs EditMode tests
  (and optionally an -executeMethod step). Prints a summary and exits non-zero on failure.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools\Verify-Headless.ps1
  powershell -ExecutionPolicy Bypass -File Tools\Verify-Headless.ps1 -ExecuteMethod GPL.Editor.Batch.RunAllSteps
#>
param(
    [string]$Mirror = "D:\UnityVerify\AcousticChamber26",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe",
    [string]$ExecuteMethod = "",
    [switch]$NoTests
)

$ErrorActionPreference = "Stop"
$project = Split-Path -Parent $PSScriptRoot
$out = Join-Path $Mirror "_verify"
New-Item -ItemType Directory -Force $out | Out-Null

foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $project $d) (Join-Path $Mirror $d) /MIR /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy $d failed ($LASTEXITCODE)" }
}

$fail = $false

if ($ExecuteMethod -ne "") {
    $log = Join-Path $out "execute.log"
    & $Unity -batchmode -nographics -projectPath $Mirror -executeMethod $ExecuteMethod -quit -logFile $log | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "executeMethod FAILED ($LASTEXITCODE), see $log"; $fail = $true }
    else { Write-Host "executeMethod OK: $ExecuteMethod" }
}

if (-not $NoTests) {
    $results = Join-Path $out "editmode-results.xml"
    $log = Join-Path $out "editmode.log"
    if (Test-Path $results) { Remove-Item $results }
    & $Unity -batchmode -nographics -projectPath $Mirror -runTests -testPlatform EditMode -testResults $results -logFile $log | Out-Null
    $code = $LASTEXITCODE
    if (-not (Test-Path $results)) {
        Write-Host "No test results (exit $code). Compile errors?"
        Select-String -Path $log -Pattern "error CS" | Select-Object -First 30 | ForEach-Object { Write-Host $_.Line }
        $fail = $true
    } else {
        [xml]$x = Get-Content $results
        $run = $x.'test-run'
        Write-Host ("Tests: total {0}, passed {1}, failed {2}, skipped {3}" -f $run.total, $run.passed, $run.failed, $run.skipped)
        $x.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
            Write-Host ("FAILED " + $_.fullname)
            Write-Host ("  " + ($_.failure.message.'#cdata-section' -replace "`n", "`n  "))
        }
        if ([int]$run.failed -gt 0) { $fail = $true }
    }
}

if ($fail) { exit 1 } else { exit 0 }
