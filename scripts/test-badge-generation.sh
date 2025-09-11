#!/bin/bash
# Test script for local coverage badge generation
# Simulates the workflow steps locally

set -e

echo "🧪 Testing Unity Coverage Badge Generation"
echo "=========================================="

# Check if coverage file exists
COVERAGE_FILE="Unity/CodeCoverage/Report/Summary.json"

if [ ! -f "$COVERAGE_FILE" ]; then
    echo "❌ Coverage file not found: $COVERAGE_FILE"
    echo "Please run Unity tests first: python scripts/unity.py"
    exit 1
fi

echo "✅ Coverage file found: $COVERAGE_FILE"

# Calculate coverage
echo "📊 Calculating coverage..."
COVERAGE_OUTPUT=$(python scripts/calculate-coverage.py --github "$COVERAGE_FILE")
echo "$COVERAGE_OUTPUT"

# Extract coverage percentage
LINE_COVERAGE=$(echo "$COVERAGE_OUTPUT" | grep "line_coverage=" | cut -d'=' -f2)

if [ -z "$LINE_COVERAGE" ]; then
    echo "❌ Could not extract coverage percentage"
    exit 1
fi

echo "📈 Extracted coverage: $LINE_COVERAGE%"

# Generate badge
echo "🎨 Generating coverage badge..."
python scripts/generate-badge.py "$LINE_COVERAGE" docs/images/coverage-badge.svg

echo "✅ Badge generated: docs/images/coverage-badge.svg"

# Show badge file info
echo "📁 Badge file info:"
ls -la docs/images/coverage-badge.svg
echo

# Show git status
echo "📝 Git status:"
git status docs/images/coverage-badge.svg || echo "Not in git repository"

echo
echo "🎉 Local badge generation test completed!"
echo "Coverage: $LINE_COVERAGE%"
echo "Badge: docs/images/coverage-badge.svg"
