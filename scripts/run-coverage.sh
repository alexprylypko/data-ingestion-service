#!/usr/bin/env bash
# run-coverage.sh - runs unit tests and generates an HTML + Cobertura coverage report.
#
# Prerequisites:
#   dotnet SDK 8+
#   reportgenerator global tool - install once with:
#     dotnet tool install -g dotnet-reportgenerator-globaltool
#
# Usage:
#   chmod +x scripts/run-coverage.sh
#   ./scripts/run-coverage.sh
#
# Exit code is non-zero if line coverage on [TransactionApi.Application]* falls below 80 %.

set -euo pipefail

RESULTS_DIR="$(pwd)/coverage-results"
REPORT_DIR="$(pwd)/coverage-report"

echo "Cleaning previous results..."
rm -rf "$RESULTS_DIR" "$REPORT_DIR"

echo "Running tests with coverage collection..."
dotnet test TransactionApi.Tests/TransactionApi.Tests.csproj \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RESULTS_DIR" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura \
     DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include="[TransactionApi.Application]*" \
     DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Threshold=80 \
     DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ThresholdType=line

echo "Generating HTML report..."
reportgenerator \
  -reports:"$RESULTS_DIR/**/coverage.cobertura.xml" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Html;TextSummary"

echo
cat "$REPORT_DIR/Summary.txt"
echo
echo "HTML report -> $REPORT_DIR/index.html"
open "$REPORT_DIR/index.html"
