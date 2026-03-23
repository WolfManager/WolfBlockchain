#!/bin/bash
# Load Testing Script for WolfBlockchain API
# Simulates concurrent users accessing dashboard endpoints
# Usage: ./load-test.sh [concurrent-users] [duration-seconds]

CONCURRENT_USERS=${1:-10}
DURATION=${2:-60}
API_URL="http://localhost/api"
RESULTS_DIR="./load-test-results"

echo "🚀 Starting Load Test"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "URL: $API_URL"
echo "Concurrent Users: $CONCURRENT_USERS"
echo "Duration: $DURATION seconds"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

mkdir -p "$RESULTS_DIR"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RESULTS_FILE="$RESULTS_DIR/load_test_${TIMESTAMP}.log"

# Initialize counters
TOTAL_REQUESTS=0
SUCCESSFUL_REQUESTS=0
FAILED_REQUESTS=0
TOTAL_TIME=0
MIN_TIME=9999
MAX_TIME=0

# Function to test single endpoint
test_endpoint() {
    local endpoint=$1
    local method=${2:-GET}
    local data=$3
    
    START_TIME=$(date +%s%N)
    
    if [ -n "$data" ]; then
        RESPONSE=$(curl -s -w "\n%{http_code}" -X "$method" \
            -H "Content-Type: application/json" \
            -d "$data" \
            "$API_URL$endpoint" 2>/dev/null)
    else
        RESPONSE=$(curl -s -w "\n%{http_code}" -X "$method" \
            "$API_URL$endpoint" 2>/dev/null)
    fi
    
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    END_TIME=$(date +%s%N)
    ELAPSED=$((($END_TIME - $START_TIME) / 1000000)) # Convert to ms
    
    TOTAL_REQUESTS=$((TOTAL_REQUESTS + 1))
    TOTAL_TIME=$((TOTAL_TIME + ELAPSED))
    
    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "401" ]; then
        SUCCESSFUL_REQUESTS=$((SUCCESSFUL_REQUESTS + 1))
        STATUS="✅"
    else
        FAILED_REQUESTS=$((FAILED_REQUESTS + 1))
        STATUS="❌"
    fi
    
    if [ $ELAPSED -lt $MIN_TIME ]; then
        MIN_TIME=$ELAPSED
    fi
    if [ $ELAPSED -gt $MAX_TIME ]; then
        MAX_TIME=$ELAPSED
    fi
    
    echo "$STATUS $endpoint - HTTP $HTTP_CODE - ${ELAPSED}ms" | tee -a "$RESULTS_FILE"
}

# Test endpoints in parallel
for i in $(seq 1 $CONCURRENT_USERS); do
    (
        START=$(date +%s)
        while [ $(($(date +%s) - $START)) -lt $DURATION ]; do
            test_endpoint "/admindashboard/summary"
            test_endpoint "/admindashboard/users?page=1"
            test_endpoint "/admindashboard/tokens?page=1"
            test_endpoint "/admindashboard/recent-events?limit=10"
            sleep 0.5
        done
    ) &
done

# Wait for all background jobs
wait

# Calculate statistics
AVG_TIME=$((TOTAL_TIME / TOTAL_REQUESTS))
SUCCESS_RATE=$(((SUCCESSFUL_REQUESTS * 100) / TOTAL_REQUESTS))

# Print results
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 Load Test Results"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Total Requests: $TOTAL_REQUESTS"
echo "Successful: $SUCCESSFUL_REQUESTS ✅"
echo "Failed: $FAILED_REQUESTS ❌"
echo "Success Rate: $SUCCESS_RATE%"
echo ""
echo "Response Times:"
echo "  Min: ${MIN_TIME}ms"
echo "  Avg: ${AVG_TIME}ms"
echo "  Max: ${MAX_TIME}ms"
echo ""
echo "Results saved to: $RESULTS_FILE"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Determine pass/fail
if [ $SUCCESS_RATE -ge 95 ] && [ $AVG_TIME -lt 100 ]; then
    echo "✅ Load test PASSED"
    exit 0
else
    echo "❌ Load test FAILED"
    exit 1
fi
