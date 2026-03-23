#!/bin/bash
# Smoke tests for WolfBlockchain API
# Usage: ./smoke-tests.sh <api_url> [retries] [host_header]

API_URL=${1:-"http://localhost"}
RETRIES=${2:-3}
HOST_HEADER=${3:-""}
ATTEMPT=1

CURL_ARGS=(-s)
if [ -n "$HOST_HEADER" ]; then
    CURL_ARGS+=(-H "Host: $HOST_HEADER")
fi

echo "🧪 Running Smoke Tests for WolfBlockchain"
echo "=========================================="
echo "API URL: $API_URL"
if [ -n "$HOST_HEADER" ]; then
    echo "Host Header: $HOST_HEADER"
fi
echo ""

# Test 1: Health Check
echo "Test 1: Health Check..."
while [ $ATTEMPT -le $RETRIES ]; do
    RESPONSE=$(curl "${CURL_ARGS[@]}" -o /dev/null -w "%{http_code}" "$API_URL/health")
    if [ "$RESPONSE" = "200" ]; then
        echo "✅ Health check passed (HTTP $RESPONSE)"
        break
    else
        echo "⚠️  Attempt $ATTEMPT: Health check returned HTTP $RESPONSE"
        if [ $ATTEMPT -eq $RETRIES ]; then
            echo "❌ Health check failed after $RETRIES attempts"
            exit 1
        fi
        ATTEMPT=$((ATTEMPT + 1))
        sleep 5
    fi
done

# Test 2: Metrics Endpoint
echo ""
echo "Test 2: Metrics Endpoint..."
RESPONSE=$(curl "${CURL_ARGS[@]}" -o /dev/null -w "%{http_code}" "$API_URL/metrics")
if [ "$RESPONSE" = "200" ]; then
    echo "✅ Metrics endpoint working (HTTP $RESPONSE)"
else
    echo "❌ Metrics endpoint failed (HTTP $RESPONSE)"
    exit 1
fi

# Test 3: Swagger Documentation
echo ""
echo "Test 3: Swagger Documentation..."
RESPONSE=$(curl "${CURL_ARGS[@]}" -o /dev/null -w "%{http_code}" "$API_URL/swagger")
if [ "$RESPONSE" = "200" ] || [ "$RESPONSE" = "301" ] || [ "$RESPONSE" = "302" ]; then
    echo "✅ Swagger UI accessible (HTTP $RESPONSE)"
else
    echo "⚠️  Swagger UI returned HTTP $RESPONSE (may not be critical)"
fi

# Test 4: Response Time
echo ""
echo "Test 4: Response Time Check..."
START=$(date +%s%N)
curl "${CURL_ARGS[@]}" "$API_URL/health" > /dev/null
END=$(date +%s%N)
ELAPSED=$((($END - $START) / 1000000))

if [ $ELAPSED -lt 500 ]; then
    echo "✅ Response time: ${ELAPSED}ms (excellent)"
elif [ $ELAPSED -lt 1000 ]; then
    echo "⚠️  Response time: ${ELAPSED}ms (acceptable)"
else
    echo "❌ Response time: ${ELAPSED}ms (slow, may indicate issues)"
    exit 1
fi

echo ""
echo "=========================================="
echo "✅ All smoke tests passed!"
echo "=========================================="
exit 0
