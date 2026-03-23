# WolfBlockchain System Architecture

## High-Level Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                     CLIENT LAYER                               │
├────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐ │
│  │  Blazor UI       │  │  Web Browser     │  │  Mobile App  │ │
│  │  (WebAssembly)   │  │  (JavaScript)    │  │  (REST API)  │ │
│  └────────┬─────────┘  └────────┬─────────┘  └──────┬───────┘ │
└───────────┼──────────────────────┼──────────────────┼──────────┘
            │                      │                  │
            └──────────────────────┼──────────────────┘
                    SignalR / HTTP │
                                   ↓
┌────────────────────────────────────────────────────────────────┐
│              API GATEWAY / LOAD BALANCER (Ingress)             │
│  - TLS Termination (Let's Encrypt)                             │
│  - Rate Limiting                                               │
│  - Request Routing                                             │
└───────────────────────┬──────────────────────────────────────┘
                        │
┌───────────────────────┴──────────────────────────────────────┐
│           KUBERNETES CLUSTER (5 pods)                          │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐    │
│  │ API Pods (3 replicas, Deployment)                  │    │
│  │  ┌─────────────────────────────────────────────┐   │    │
│  │  │  .NET 10 ASP.NET Core (v2.0.0)             │   │    │
│  │  │  ┌─────────────────────────────────────┐   │   │    │
│  │  │  │ Middleware (7 layers)               │   │   │    │
│  │  │  │  - Exception handling               │   │   │    │
│  │  │  │  - Request/response logging         │   │   │    │
│  │  │  │  - Rate limiting                    │   │   │    │
│  │  │  │  - IP allowlisting                  │   │   │    │
│  │  │  │  - Security headers                 │   │   │    │
│  │  │  │  - CORS                             │   │   │    │
│  │  │  │  - Authentication/Authorization     │   │   │    │
│  │  │  └─────────────────────────────────────┘   │   │    │
│  │  │  ┌─────────────────────────────────────┐   │   │    │
│  │  │  │ Controllers (5 main)                │   │   │    │
│  │  │  │  - BlockchainController            │   │   │    │
│  │  │  │  - TokenController                 │   │   │    │
│  │  │  │  - SmartContractController         │   │   │    │
│  │  │  │  - AdminDashboardController        │   │   │    │
│  │  │  │  - Others (security, analytics)    │   │   │    │
│  │  │  └─────────────────────────────────────┘   │   │    │
│  │  │  ┌─────────────────────────────────────┐   │   │    │
│  │  │  │ Services (12+ services)             │   │   │    │
│  │  │  │  - JwtTokenService                  │   │   │    │
│  │  │  │  - CacheService (in-memory)         │   │   │    │
│  │  │  │  - AdminDashboardCacheService       │   │   │    │
│  │  │  │  - RealtimeUpdateService (SignalR)  │   │   │    │
│  │  │  │  - ValidationService                │   │   │    │
│  │  │  │  - And more...                      │   │   │    │
│  │  │  └─────────────────────────────────────┘   │   │    │
│  │  │  ┌─────────────────────────────────────┐   │   │    │
│  │  │  │ Hubs (SignalR)                      │   │   │    │
│  │  │  │  - BlockchainHub (/blockchain-hub)  │   │   │    │
│  │  │  └─────────────────────────────────────┘   │   │    │
│  │  └─────────────────────────────────────────────┘   │    │
│  │  Resources: 384Mi request → 768Mi limit          │    │
│  │            200m CPU request → 500m limit          │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ Database Pod (1, StatefulSet)                       │    │
│  │  - SQL Server 2022                                  │    │
│  │  - PersistentVolume (100GB)                         │    │
│  │  - Backup automation                                │    │
│  │  Resources: 512Mi request → 1Gi limit              │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ Monitoring Pod (1, Deployment)                      │    │
│  │  - Prometheus (metrics collection)                  │    │
│  │  - Grafana-ready (dashboard templates)              │    │
│  │  - AlertManager                                     │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  HPA: 3-10 replicas (70% CPU, 80% memory triggers)          │
└─────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│              DATA LAYER (External to K8s)                       │
├────────────────────────────────────────────────────────────────┤
│  - Persistent SQL Server Database                             │
│  - Regular backups (daily)                                    │
│  - Read replicas (optional for scale)                         │
│  - Point-in-time recovery capability                          │
└────────────────────────────────────────────────────────────────┘
```

---

## Component Details

### Frontend (Blazor WebAssembly)
- **Framework**: Blazor WebAssembly
- **Renders**: In browser (not on server)
- **Features**:
  - Admin dashboard with tabs
  - Token management UI
  - Smart contract deployment
  - AI training job monitor
  - Real-time updates (SignalR)
- **Bundle Size**: ~5-8MB (gzipped)

### Backend API (.NET 10)
- **Framework**: ASP.NET Core 10
- **Port**: 5000 (HTTP)
- **Endpoints**: 20+ REST API routes
- **Real-time**: SignalR hub at `/blockchain-hub`
- **Auth**: JWT bearer tokens

### Middleware Stack (Request Flow)
```
Request
  ↓
1. GlobalExceptionHandlerMiddleware  (catch all errors)
  ↓
2. RequestSizeLimitingMiddleware     (prevent DoS)
  ↓
3. RequestResponseLoggingMiddleware  (log all requests)
  ↓
4. RateLimitingMiddleware            (prevent abuse)
  ↓
5. AdminIpAllowlistMiddleware        (restrict by IP)
  ↓
6. PerformanceMonitoringMiddleware   (track metrics)
  ↓
7. SecurityHeadersMiddleware         (CSP, XSS, etc)
  ↓
8. Authentication Middleware         (JWT validation)
  ↓
9. Authorization Middleware          (role checks)
  ↓
10. Router/Controller
  ↓
Response (reversed order)
```

### Data Access Layer (EF Core)
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Pattern**: Repository pattern
- **Features**:
  - LINQ queries
  - Migrations
  - Change tracking
  - Lazy loading
- **Tables**: Users, Tokens, Transactions, Blocks, etc.

### Cache Layer
- **In-Memory**: MemoryCache (.NET)
- **TTLs**:
  - Summary dashboard: 5 minutes
  - User/token lists: 10 minutes (per page)
  - Recent events: 2 minutes
- **Hit Rate**: Target >70% for admin endpoints
- **Optional**: Redis for distributed cache (future)

### Monitoring Stack
- **Prometheus**: Metrics collection
  - CPU/memory per pod
  - Request latency per endpoint
  - Error rates
  - Custom metrics
- **Grafana**: Dashboard visualization (optional)
- **Alerts**: CPU >80%, memory >85%, errors >1%

---

## Data Flow Diagrams

### User Login Flow
```
User Browser
    ↓
/login page
    ↓
POST /api/auth/login (email, password)
    ↓
API validates credentials → queries Users table
    ↓
API generates JWT token
    ↓
Response with token + redirect
    ↓
Browser stores token (localStorage)
    ↓
Subsequent requests include: Authorization: Bearer {token}
    ↓
Middleware validates JWT
    ↓
Request authorized → proceed
```

### Token Creation Flow
```
Admin opens TokenManagement component
    ↓
User fills form (name, symbol, supply)
    ↓
Input validation (BlazorInputValidator)
    ↓
If valid: POST /api/token/create
    ↓
API validates again (server-side)
    ↓
API creates record in Tokens table
    ↓
RealtimeUpdateService broadcasts to connected clients
    ↓
SignalR sends "TokenCreated" event
    ↓
All connected dashboards update in real-time
    ↓
Success toast notification shown
```

### Admin Dashboard Load
```
User navigates to /admin
    ↓
Blazor component renders
    ↓
Requests: GET /api/admindashboard/summary
    ↓
API checks cache (AdminDashboardCacheService)
    ↓
If cached → return immediately (~5ms)
If not cached → query database → cache for 5min
    ↓
Response returned (5-50ms depending on cache)
    ↓
Component renders real-time statistics
    ↓
SignalR connects to /blockchain-hub
    ↓
Live updates flow to dashboard
```

---

## Scalability Strategy

### Horizontal Scaling
- **Current**: 3 API replicas
- **Auto-scaling**: HPA 3-10 replicas
  - Trigger: 70% CPU or 80% memory
  - Scale-up: Immediate
  - Scale-down: 5 minutes stabilization

### Vertical Scaling
- **Current limits**: 768Mi memory, 500m CPU per pod
- **Can increase** if needed (K8s cluster resources available)

### Database Scaling
- **Current**: Single SQL Server instance
- **Future**:
  - Read replicas for analytics
  - Sharding by user/tenant
  - Connection pooling optimization

### Cache Scaling
- **Current**: In-memory per pod (not shared)
- **Future**: Redis for distributed cache
  - Shared across all pods
  - Reduced database load
  - Faster responses

---

## Security Architecture

### Network Security
```
Internet
   ↓ (encrypted TLS)
Ingress (LoadBalancer)
   ↓ (mTLS possible)
K8s NetworkPolicy (restricts traffic)
   ↓
API Pods
   ↓
Database (connection string only, no public access)
```

### Authentication
- **Method**: JWT bearer tokens
- **Issuer**: API at startup
- **Validator**: Middleware checks every request
- **Expiry**: Configurable (default: 24 hours)
- **Renewal**: Refresh token endpoint (optional)

### Authorization
- **Method**: Claims-based
- **Admin role**: Single admin mode
- **Per-endpoint**: [Authorize] attributes
- **Fallback policy**: Require authenticated user

### Input Validation
- **Layers**:
  1. Client-side (Blazor validation)
  2. Server-side (BlazorInputValidator)
  3. Business logic (service layer)
- **Coverage**: All user inputs sanitized

### Data Protection
- **Connection String**: Environment variable (not in code)
- **Secrets**: K8s Secrets (future: HashiCorp Vault)
- **Logs**: Sanitized (no passwords, no tokens)
- **Database**: Backup encryption (optional)

---

## Performance Architecture

### Response Time Targets
```
/health, /metrics                  <10ms
/api/admindashboard/* (cached)     <50ms (p50), <200ms (p95)
/api/admindashboard/* (uncached)   <500ms (p50), <1000ms (p95)
SignalR messages                   <100ms latency
Blazor UI interaction              <500ms (perceived)
```

### Optimization Techniques
1. **Caching** (API responses, 70-80% hit rate)
2. **Async/await** (non-blocking I/O)
3. **Connection pooling** (database)
4. **Compression** (gzip responses)
5. **Batching** (reduce queries)
6. **Indexing** (database indexes)
7. **CDN** (static assets, future)

---

## Deployment Architecture

### Environments
```
Development
  ├─ Local machine
  ├─ SQL LocalDB or Docker Compose DB
  └─ Visual Studio debugging

Staging
  ├─ K8s cluster (same as prod)
  ├─ Separate namespace
  ├─ Real SSL cert
  └─ Mirror of production

Production
  ├─ K8s cluster (redundancy ready)
  ├─ Auto-scaling enabled
  ├─ Monitoring active
  ├─ Backup automated
  └─ High availability
```

---

## Disaster Recovery

### Backup Strategy
- **Database**: Daily snapshots (SQL Server)
- **Backups stored**: Separate storage location
- **Retention**: 30 days
- **RTO** (Recovery Time Objective): 1 hour
- **RPO** (Recovery Point Objective): 24 hours

### Disaster Scenarios
```
Scenario 1: Pod crash
  Solution: Kubernetes automatically restarts (ReplicaSet)
  Time: <30 seconds

Scenario 2: Database failure
  Solution: Restore from backup
  Time: 1-2 hours

Scenario 3: Cluster failure
  Solution: Deploy to new cluster (manual for now)
  Time: 4-6 hours
  Future: Multi-region for automatic failover

Scenario 4: DDoS attack
  Solution: Rate limiting + ingress throttling
  Built-in: RateLimitingMiddleware
```

---

## Monitoring & Observability

### Metrics Collected
- **Application**:
  - Request count per endpoint
  - Response time distribution
  - Error rate by status code
  - Cache hit rate
  - JWT validation failures
- **Infrastructure**:
  - Pod CPU/memory usage
  - Network I/O
  - Disk usage
  - Database connections
  - Deployment rollout status

### Logging
- **Format**: Structured JSON (Serilog)
- **Levels**: Debug, Info, Warning, Error, Fatal
- **Retention**: 7 days (configurable)
- **Searchable**: By timestamp, level, component, user

### Alerting
- **Alert**: CPU >80% for 5 minutes
- **Alert**: Memory >85% for 5 minutes
- **Alert**: Error rate >1% for 10 minutes
- **Alert**: Response time p95 >1000ms for 10 minutes

---

## Future Enhancements

### Phase 2
- [ ] Redis distributed cache
- [ ] Read replicas for analytics
- [ ] Webhook system for events
- [ ] Advanced AI model serving
- [ ] Mobile app (iOS/Android)

### Phase 3
- [ ] Multi-region deployment
- [ ] Advanced analytics dashboard
- [ ] Audit trail interface
- [ ] Advanced RBAC (role-based access control)
- [ ] OpenID Connect integration

### Phase 4
- [ ] Sharding strategy implementation
- [ ] Blockchain node deployment
- [ ] Custom DNS / multi-domain
- [ ] DDoS mitigation (WAF)
- [ ] API versioning (v1, v2, v3)

---

**This architecture supports MVP launch and is designed to scale to enterprise demands.**
