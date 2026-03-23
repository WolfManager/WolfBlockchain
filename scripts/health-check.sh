#!/bin/bash
# Health check script for WolfBlockchain deployment
# Monitors API health for specified duration
# Usage: ./health-check.sh <api_url> <duration_minutes>

API_URL=${1:-"http://localhost"}
DURATION=${2:-5}
INTERVAL=30

echo "🏥 Starting Health Check Monitoring"
echo "===================================="
echo "API URL: $API_URL"
echo "Duration: $DURATION minutes"
echo "Check Interval: $INTERVAL seconds"
echo ""

START_TIME=$(date +%s)
END_TIME=$((START_TIME + DURATION * 60))
CHECK_COUNT=0
FAILED_COUNT=0
ERROR_COUNT=0

# Define thresholds
LATENCY_WARNING=100  # ms
LATENCY_CRITICAL=500  # ms

while [ $(date +%s) -lt $END_TIME ]; do
    CHECK_COUNT=$((CHECK_COUNT + 1))
    TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
    
    # Check health endpoint
    HEALTH_RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/health" 2>/dev/null)
    HTTP_CODE=$(echo "$HEALTH_RESPONSE" | tail -n1)
    BODY=$(echo "$HEALTH_RESPONSE" | head -n-1)
    
    # Measure response time
    START_MS=$(date +%s%N)
    curl -s "$API_URL/health" > /dev/null 2>&1
    END_MS=$(date +%s%N)
    LATENCY=$((($END_MS - $START_MS) / 1000000))
    
    # Evaluate status
    if [ "$HTTP_CODE" != "200" ]; then
        echo "[$TIMESTAMP] ❌ ERROR: Health check returned HTTP $HTTP_CODE"
        FAILED_COUNT=$((FAILED_COUNT + 1))
        ERROR_COUNT=$((ERROR_COUNT + 1))
    elif [ $LATENCY -gt $LATENCY_CRITICAL ]; then
        echo "[$TIMESTAMP] ❌ CRITICAL: High latency ${LATENCY}ms (threshold: ${LATENCY_CRITICAL}ms)"
        ERROR_COUNT=$((ERROR_COUNT + 1))
    elif [ $LATENCY -gt $LATENCY_WARNING ]; then
        echo "[$TIMESTAMP] ⚠️  WARNING: Elevated latency ${LATENCY}ms (threshold: ${LATENCY_WARNING}ms)"
    else
        echo "[$TIMESTAMP] ✅ OK: Health check passed (${LATENCY}ms)"
    fi
    
    # Wait before next check
    if [ $(date +%s) -lt $END_TIME ]; then
        sleep $INTERVAL
    fi
done

echo ""
echo "===================================="
echo "🏥 Health Check Summary"
echo "===================================="
echo "Total Checks: $CHECK_COUNT"
echo "Failed Checks: $FAILED_COUNT"
echo "Critical Errors: $ERROR_COUNT"

if [ $ERROR_COUNT -eq 0 ]; then
    echo "✅ All health checks passed!"
    exit 0
elif [ $FAILED_COUNT -le 1 ]; then
    echo "⚠️  Some issues detected but within tolerance"
    exit 0
else
    echo "❌ Health check monitoring detected critical issues"
    exit 1
fi
