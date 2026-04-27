# run-coverage.ps1 - Windows PowerShell equivalent of run-coverage.sh
#
# Prerequisites:
#   dotnet SDK 8+
#   reportgenerator global tool - install once with:
#     dotnet tool install -g dotnet-reportgenerator-globaltool
#
# Usage:
#   .\scripts\run-coverage.ps1

$ResultsDir = Join-Path $PSScriptRoot "..\coverage-results"
$ReportDir  = Join-Path $PSScriptRoot "..\coverage-report"

Remove-Item -Recurse -Force $ResultsDir, $ReportDir -ErrorAction SilentlyContinue

dotnet test TransactionApi.Tests\TransactionApi.Tests.csproj `
  --configuration Release `
  --collect:"XPlat Code Coverage" `
  --results-directory $ResultsDir `
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura `
     DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include="[TransactionApi.Application]*" `
     DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Threshold=80 `
     DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ThresholdType=line

reportgenerator `
  -reports:"$ResultsDir\**\coverage.cobertura.xml" `
  -targetdir:$ReportDir `
  -reporttypes:"Html;TextSummary"

Get-Content "$ReportDir\Summary.txt"
Write-Host "`nHTML report -> $ReportDir\index.html"
