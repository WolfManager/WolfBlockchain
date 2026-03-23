# Observability & Monitoring Strategy

## Overview
Comprehensive observability for production system covering logs, metrics, and alerts.

---

## 1. STRUCTURED LOGGING

### Current Implementation
Using **Serilog** with:
- Console output (development)
- File output (rolling daily, 30-day retention)
- Security audit trail (separate 90-day log)
- Structured JSON format

### Log Levels
```
Fatal   - System is unusable (e.g., cannot connect to database)
Error   - Error conditions (e.g., validation failure)
Warning - Warning conditions (e.g., deprecated API call)
Info    - Informational (normal operation milestones)
Debug   - Detailed debugging information
Verbose - Very detailed logging (disabled in production)
```

### Log Examples

**Normal API Request**:
```json
{
  "Timestamp": "2026-03-22T14:30:45.1234567Z",
  "Level": "Information",
  "MessageTemplate": "HTTP request processed in {ElapsedMilliseconds}ms",
  "Properties": {
    "ElapsedMilliseconds": 45,
    "Path": "/api/admindashboard/summary",
    "Method": "GET",
    "StatusCode": 200,
    "UserId": "admin-001"
  }
}
```

**API Error**:
```json
{
  "Timestamp": "2026-03-22T14:30:46.5234567Z",
  "Level": "Error",
  "MessageTemplate": "Database connection failed",
  "Exception": "System.Data.SqlClient.SqlException: Connection timeout",
  "Properties": {
    "Path": "/api/token/create",
    "Method": "POST",
    "UserId": "admin-001",
    "Server": "prod-db-server"
  }
}
```

**Security Audit**:
```json
{
  "Timestamp": "2026-03-22T14:30:50.1234567Z",
  "Level": "Information",
  "MessageTemplate": "Unauthorized access attempt",
  "AuditType": "Security",
  "Properties": {
    "IpAddress": "192.168.1.100",
    "Endpoint": "/api/admindashboard/users",
    "Reason": "Invalid JWT token",
    "UserId": "unknown"
  }
}
```

---

## 2. METRICS (Prometheus)

### What We Measure

#### Application Metrics
```
# HTTP Request Metrics
http_request_duration_seconds  (histogram, per endpoint)
http_requests_total            (counter, per method/status)
http_request_size_bytes        (histogram)
http_response_size_bytes       (histogram)

# Application Metrics
cache_hits_total               (counter)
cache_misses_total             (counter)
cache_evictions_total          (counter)
jwt_validations_total          (counter, per result)
jwt_validation_failures_total  (counter, per reason)

# Database Metrics
db_query_duration_seconds      (histogram, per operation)
db_connections_active          (gauge)
db_connection_pool_size        (gauge)
db_errors_total                (counter, per error type)

# Business Metrics
tokens_created_total           (counter)
contracts_deployed_total       (counter)
ai_jobs_started_total          (counter)
ai_jobs_completed_total        (counter)
ai_jobs_failed_total           (counter)
```

### Scrape Configuration (Prometheus)

```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'wolf-blockchain-api'
    static_configs:
      - targets: ['localhost:9090']
    metrics_path: '/metrics'
    scrape_interval: 15s
    scrape_timeout: 10s
```

### Query Examples

```promql
# API latency (p95)
histogram_quantile(0.95, http_request_duration_seconds)

# Request rate per endpoint
rate(http_requests_total[1m]) by (path)

# Cache hit rate
cache_hits_total / (cache_hits_total + cache_misses_total)

# Error rate
rate(http_requests_total{status=~"5.."}[5m])

# JWT validation failure rate
rate(jwt_validation_failures_total[5m])

# Database connection pool usage
db_connections_active / db_connection_pool_size
```

---

## 3. ALERTING RULES

### Critical Alerts (Page on-call)

**API Latency Critical**
```yaml
- alert: ApiLatencyHigh
  expr: histogram_quantile(0.95, http_request_duration_seconds) > 1
  for: 5m
  annotations:
    summary: "API latency exceeded 1 second"
    description: "P95 latency: {{ $value }}s"
    severity: critical
    action: "Check /metrics endpoint, review slow queries"
```

**Database Connection Failed**
```yaml
- alert: DatabaseConnectionFailed
  expr: increase(db_errors_total{error="connection_timeout"}[5m]) > 0
  for: 1m
  annotations:
    summary: "Database connection failed"
    description: "Cannot connect to database"
    severity: critical
    action: "Check database server status, network connectivity"
```

**Smart Contract Deployment Failed**
```yaml
- alert: SmartContractDeploymentFailed
  expr: rate(contracts_deployed_total{status="failed"}[5m]) > 0
  for: 2m
  annotations:
    summary: "Smart contract deployment failure detected"
    description: "Rate: {{ $value }} failures/min"
    severity: critical
    action: "Check contract validation, blockchain connectivity"
```

**API Error Rate High**
```yaml
- alert: ApiErrorRateHigh
  expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.01
  for: 2m
  annotations:
    summary: "API error rate > 1%"
    description: "Error rate: {{ $value | humanizePercentage }}"
    severity: critical
    action: "Check logs, review recent deployments"
```

### Warning Alerts (Slack notification)

**High Memory Usage**
```yaml
- alert: PodMemoryUsageHigh
  expr: container_memory_working_set_bytes{pod="wolf-blockchain-api"} > 600000000
  for: 5m
  annotations:
    summary: "Pod memory usage > 600Mi"
    severity: warning
    action: "Monitor memory, consider vertical scaling"
```

**Cache Hit Rate Low**
```yaml
- alert: CacheHitRateLow
  expr: (cache_hits_total / (cache_hits_total + cache_misses_total)) < 0.6
  for: 15m
  annotations:
    summary: "Cache hit rate < 60%"
    severity: warning
    action: "Check cache configuration, adjust TTL"
```

**JWT Validation Failures Elevated**
```yaml
- alert: JwtValidationFailuresElevated
  expr: rate(jwt_validation_failures_total[5m]) > 0.001
  for: 10m
  annotations:
    summary: "JWT validation failure rate elevated"
    severity: warning
    action: "Check token validity, review client logs"
```

---

## 4. DASHBOARDS (Grafana)

### System Health Dashboard
```
┌─────────────────────────────────────────────┐
│ WolfBlockchain System Health                │
├─────────────────────────────────────────────┤
│                                             │
│ API Status: ✅ HEALTHY                     │
│ Database Status: ✅ HEALTHY                │
│ Cache Hit Rate: 78%                        │
│ Error Rate: 0.2%                           │
│                                             │
│ ┌──────────────────────────────────────┐   │
│ │ API Latency (P95)                    │   │
│ │ 45ms ───────────────────┐            │   │
│ └──────────────────────────────────────┘   │
│                                             │
│ ┌──────────────────────────────────────┐   │
│ │ Request Rate                         │   │
│ │ 450 req/sec                          │   │
│ └──────────────────────────────────────┘   │
│                                             │
│ ┌──────────────────────────────────────┐   │
│ │ Database Queries/sec                 │   │
│ │ 120 queries/sec                      │   │
│ └──────────────────────────────────────┘   │
│                                             │
└─────────────────────────────────────────────┘
```

### Troubleshooting Dashboard
```
┌─────────────────────────────────────────────┐
│ WolfBlockchain Troubleshooting              │
├─────────────────────────────────────────────┤
│                                             │
│ Recent Errors (Last Hour)                  │
│ ├─ DatabaseTimeoutException (2)            │
│ ├─ ValidationException (5)                 │
│ └─ DeploymentFailedException (1)           │
│                                             │
│ Slow Endpoints (p95 > 100ms)                │
│ ├─ /api/tokens (125ms)                    │
│ └─ /api/contracts (110ms)                 │
│                                             │
│ Pod Restarts (Last 24h)                    │
│ ├─ wolf-blockchain-api-1: 0               │
│ ├─ wolf-blockchain-api-2: 0               │
│ └─ wolf-blockchain-api-3: 0               │
│                                             │
└─────────────────────────────────────────────┘
```

---

## 5. LOGGING IN PRACTICE

### Logging Best Practices

**DO:**
```csharp
// ✅ Good: Structured logging with context
Log.Information(
    "Token created successfully. TokenId: {TokenId}, Creator: {CreatorId}, Supply: {Supply}",
    token.Id, userId, token.TotalSupply);

Log.Error(
    "Failed to deploy contract. Error: {ErrorMessage}, ContractName: {ContractName}",
    ex.Message, contractName);

Log.Warning(
    "Cache miss for endpoint {Endpoint}. Cache TTL may need adjustment.",
    "/api/admindashboard/summary");
```

**DON'T:**
```csharp
// ❌ Bad: Hardcoded values, no context
Log.Information("Token created");

// ❌ Bad: Logging secrets
Log.Information($"JWT: {jwtToken}");  // NEVER log tokens!

// ❌ Bad: String concatenation
Log.Information("User " + username + " logged in");  // Use placeholders instead
```

### Sensitive Data Masking

All logs automatically mask:
- JWT tokens
- Passwords
- API keys
- Connection strings
- Personal identifiable information (PII)

Configured in logging middleware.

---

## 6. INCIDENT RESPONSE

### Detection → Alert → Action → Resolution

**Scenario 1: API Latency Spike**
```
1. [14:35] Prometheus alert: Api Latency High (P95 > 1s)
   ↓
2. [14:35] Grafana dashboard shows: /api/tokens endpoint slow
   ↓
3. [14:36] Check logs: "Database query timeout on tokens list"
   ↓
4. [14:37] Check database: High CPU (95%), connection pool saturated
   ↓
5. [14:38] Action: Scale database connections or restart pods
   ↓
6. [14:40] Resolution: P95 latency back to 45ms, alert cleared
```

**Scenario 2: Smart Contract Deployment Failure**
```
1. [14:42] Prometheus alert: Smart Contract Deployment Failed
   ↓
2. [14:42] Slack notification: "1 contract deployment failed"
   ↓
3. [14:43] Check logs: "Contract validation failed: syntax error in code"
   ↓
4. [14:44] Notify user: Invalid smart contract, resubmit
   ↓
5. [14:45] User resubmits valid contract
   ↓
6. [14:46] Resolution: Contract deployed successfully
```

**Scenario 3: AI Training Job Timeout**
```
1. [14:48] Log: "AI training job timeout after 300 seconds"
   ↓
2. [14:48] Alert: AI job failure rate elevated
   ↓
3. [14:49] Check metrics: Training taking longer than expected
   ↓
4. [14:50] Action options:
   - Increase timeout threshold
   - Scale training resources
   - Check dataset size
   ↓
5. [14:55] Resolution: Training completes successfully with adjusted settings
```

---

## 7. MONITORING CHECKLIST

### Daily (Automated)
- [ ] Alert summary: Critical vs Warning alerts
- [ ] API health: Uptime, error rate, latency
- [ ] Database: Query performance, connection health
- [ ] Cache: Hit rate, eviction rate

### Weekly (Team Review)
- [ ] Trend analysis: Performance trends
- [ ] Capacity planning: Growth projections
- [ ] Log analysis: Error patterns, optimization opportunities

### Monthly (Deep Dive)
- [ ] Alert tuning: False positives/negatives
- [ ] Performance review: SLA compliance
- [ ] Cost analysis: Resource utilization
- [ ] Security review: Access patterns, auth failures

---

## 8. GRAFANA SETUP

### Create Dashboard JSON
```json
{
  "dashboard": {
    "title": "WolfBlockchain System Health",
    "panels": [
      {
        "title": "API Latency (P95)",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, http_request_duration_seconds)"
          }
        ]
      },
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "rate(http_requests_total[1m])"
          }
        ]
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "rate(http_requests_total{status=~\"5..\"}[1m])"
          }
        ]
      },
      {
        "title": "Cache Hit Rate",
        "targets": [
          {
            "expr": "cache_hits_total / (cache_hits_total + cache_misses_total)"
          }
        ]
      }
    ]
  }
}
```

---

## Summary

This observability strategy ensures:
- ✅ Real-time visibility into system health
- ✅ Quick detection of issues
- ✅ Fast incident response
- ✅ Data-driven optimization
- ✅ Compliance & audit trail

**Everything is logged, measured, and monitored for production success.**
