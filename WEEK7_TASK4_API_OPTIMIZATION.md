# ⚡ TASK 4: API OPTIMIZATION & TUNING
## Week 7 - Continue Performance Push

---

## 🎯 TASK 4 OBJECTIVES

**Goal**: Optimize API response handling and resource utilization
**Time**: 1-1.5 hours
**Expected Impact**: -20% API latency, +10% throughput

---

## 📋 CHECKLIST

### **1. Async/Await Best Practices** (20 min)
- [ ] Audit existing Controllers for sync-over-async
- [ ] Convert all DB queries to async
- [ ] Remove unnecessary Task.Wait/Result
- [ ] Add ConfigureAwait(false) to library code
- [ ] Verify no blocking calls in async context

### **2. Connection Pooling Optimization** (15 min)
- [ ] Check connection string pool settings
- [ ] Increase Min Pool Size to 5-10
- [ ] Set Max Pool Size to 100
- [ ] Enable Connection Pooling = true
- [ ] Add Application Name
- [ ] Test pool effectiveness

### **3. Request/Response Optimization** (15 min)
- [ ] Add response pagination for large lists
- [ ] Implement sparse field selection
- [ ] Add request batching endpoint
- [ ] Optimize JSON serialization
- [ ] Add response streaming for large payloads

### **4. Performance Dashboard** (20 min)
- [ ] Create performance monitoring endpoint
- [ ] Track response times
- [ ] Track database metrics
- [ ] Track cache performance
- [ ] Add to Prometheus
- [ ] Test dashboard

### **5. Benchmarking Setup** (10 min)
- [ ] Create baseline measurements
- [ ] Run performance tests
- [ ] Compare before/after
- [ ] Document improvements
- [ ] Generate report

---

## 🔧 SPECIFIC OPTIMIZATIONS

### **Connection String Optimization**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": 
      "Server=localhost;Database=WolfBlockchainDb;
       Trusted_Connection=true;
       MultipleActiveResultSets=true;
       Min Pool Size=5;
       Max Pool Size=100;
       Pooling=true;
       Application Name=WolfBlockchain;
       Connection Timeout=30;"
  }
}
```

### **Database Query Patterns**
```csharp
// ❌ AVOID: Sync over async
var users = _context.Users.ToList();

// ✅ GOOD: Async
var users = await _context.Users.ToListAsync();

// ❌ AVOID: N+1 queries
foreach (var user in users)
{
    var tokens = _context.Tokens.Where(t => t.UserId == user.Id).ToList();
}

// ✅ GOOD: Single query
var users = await _context.Users
    .Include(u => u.Tokens)
    .ToListAsync();

// ✅ GOOD: Projection (smaller payload)
var users = await _context.Users
    .Select(u => new { u.Id, u.Username, u.Email })
    .ToListAsync();

// ✅ GOOD: AsNoTracking for read-only
var users = await _context.Users
    .AsNoTracking()
    .ToListAsync();
```

### **Response Optimization**
```csharp
// ✅ Add pagination
public async Task<PagedResult<UserDto>> GetUsers(int page = 1, int pageSize = 20)
{
    var query = _context.Users.AsNoTracking();
    var total = await query.CountAsync();
    
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(u => new UserDto { Id = u.Id, Username = u.Username })
        .ToListAsync();
    
    return new PagedResult<UserDto>(items, page, pageSize, total);
}

// ✅ Sparse field selection
public async Task<IActionResult> GetUsers([FromQuery] string[] fields)
{
    // Return only requested fields
    var allowedFields = new[] { "id", "username", "email" };
    var selectedFields = fields.Where(f => allowedFields.Contains(f)).ToArray();
    // Build dynamic select...
}

// ✅ Batch operations
[HttpPost("batch")]
public async Task<IActionResult> BatchGetUsers([FromBody] BatchRequest request)
{
    var userIds = request.Ids.Take(100).ToList(); // Max 100 per batch
    var users = await _context.Users
        .Where(u => userIds.Contains(u.Id))
        .ToListAsync();
    return Ok(users);
}
```

---

## 🚀 PERFORMANCE MONITORING

### **New Metrics to Track**
```csharp
namespace WolfBlockchain.API.Services;

public interface IPerformanceMonitor
{
    Task RecordRequestAsync(string path, long durationMs, int statusCode);
    Task<PerformanceSnapshot> GetSnapshotAsync();
}

public record PerformanceSnapshot
{
    public int TotalRequests { get; set; }
    public double AvgResponseTime { get; set; }
    public int P50ResponseTime { get; set; }
    public int P95ResponseTime { get; set; }
    public int P99ResponseTime { get; set; }
    public double RequestsPerSecond { get; set; }
    public int ErrorCount { get; set; }
    public double CacheHitRate { get; set; }
}
```

### **Prometheus Metrics**
```
# API Performance
api_request_duration_ms{method="GET", path="/api/users"} = 45
api_request_count{method="GET", status="200"} = 15000
cache_hit_ratio = 0.72
database_query_duration_ms{query="GetUserById"} = 25

# System Health
process_memory_bytes = 350000000
thread_pool_queue_length = 5
connection_pool_available = 95
```

---

## 📊 EXPECTED RESULTS

### **Before Task 4**
```
Response Time: ~75-100ms (from Tasks 2-3)
Throughput: ~2500 req/sec
Memory: ~350MB
CPU: ~200m
```

### **After Task 4**
```
Response Time: ~50-70ms (-30%)
Throughput: ~2700+ req/sec (+8%)
Memory: ~320MB (-8%)
CPU: ~180m (-10%)
```

---

## 🔍 VALIDATION

```
✅ No sync-over-async calls
✅ All DB queries async
✅ Connection pooling active
✅ N+1 queries eliminated
✅ Pagination implemented
✅ Cache integration working
✅ Performance metrics collected
✅ Prometheus updated
✅ All tests passing
✅ Load test stable
```

---

## 📝 DELIVERABLES

- [ ] Updated connection strings
- [ ] Async/await audit complete
- [ ] Request/response optimization
- [ ] Performance monitoring endpoint
- [ ] Performance dashboard working
- [ ] Benchmarks documented
- [ ] Performance report generated
- [ ] Tests passing (60+)

---

## ⏱️ TIME ESTIMATE

```
Async/Await Audit:        15 min
Connection Pooling:       10 min
Request Optimization:     15 min
Monitoring Setup:         15 min
Testing & Validation:     15 min
Documentation:            10 min
─────────────────────
Total:                    80 min (~1.5 hours)
```

---

## 🎯 SUCCESS CRITERIA

✅ Response time < 70ms (P95)
✅ Throughput > 2600 req/sec
✅ Cache hit rate > 60%
✅ Error rate < 0.05%
✅ Memory < 400MB/pod
✅ All tests green
✅ Load tests stable
✅ Documentation complete

---

## 🚀 READY?

Start Task 4 when ready!

After Task 4: Move to Task 5 - Load Testing & Final Report

---

**DURATION**: ~1.5 hours
**DIFFICULTY**: Medium
**IMPACT**: High (3 hours → 2 hours for complex dashboards)
