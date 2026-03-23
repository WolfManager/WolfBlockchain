# 🚀 MAINE - QUICK REFERENCE

## 📅 Data: 26 Ianuarie 2024
## 🎯 Task: WEEK 5 - INFRASTRUCTURE (Docker, CI/CD)

---

## ⚡ QUICK STATUS CHECK

```bash
# 1. Verifica build
cd D:\WolfBlockchain  # Path-ul workspace
dotnet build

# Expected: BUILD SUCCESSFUL ✅

# 2. Ruleaza tests  
dotnet test

# Expected: 60+ tests passing ✅
```

---

## 📋 WEEK 5 CHECKLIST

### Task 1: Dockerfile Creation
- [ ] Create `Dockerfile` in `src/WolfBlockchain.API/`
- [ ] Multi-stage build (build + runtime)
- [ ] .NET 10 base images
- [ ] Expose port 5000 (HTTP) & 5443 (HTTPS)
- [ ] Health check configured

### Task 2: Docker Compose
- [ ] Create `docker-compose.yml` in root
- [ ] API service (WolfBlockchain.API)
- [ ] SQL Server service
- [ ] Network configuration
- [ ] Volume mounts for database

### Task 3: GitHub Actions CI/CD
- [ ] Create `.github/workflows/` directory
- [ ] Create `ci-cd.yml` workflow
- [ ] Build & Test triggers
- [ ] Docker image build & push
- [ ] Environment variables

### Task 4: Testing & Validation
- [ ] Build Docker image locally
- [ ] Run container
- [ ] Test /health endpoint
- [ ] Test /swagger endpoint
- [ ] Verify all tests pass

---

## 📁 FILES TO CREATE MAINE

```
ROOT/
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .github/
│   └── workflows/
│       └── ci-cd.yml
└── docker-compose.dev.yml (optional)
```

---

## 🔧 KEY CONFIGURATIONS

### Dockerfile Key Points:
- Base image: `mcr.microsoft.com/dotnet/sdk:10.0` (build)
- Runtime image: `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime)
- Expose: `EXPOSE 5000`
- Healthcheck: `HEALTHCHECK CMD curl --fail http://localhost:5000/health`

### Docker Compose Key Points:
- API service: build from ./src/WolfBlockchain.API
- SQL Server: image: `mcr.microsoft.com/mssql/server:latest`
- Networks: `wolf-network`
- Ports: `5000:5000` (API), `1433:1433` (SQL Server)

### GitHub Actions Key Points:
- Trigger: `on: [push]` (to main/develop)
- Jobs: build, test, docker-build, docker-push
- Secrets: `DOCKER_USERNAME`, `DOCKER_PASSWORD`, `JWT_SECRET`

---

## 📝 TEMPLATE URLS PENTRU MAINE

### Dockerfile Template:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
RUN dotnet build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "WolfBlockchain.API.dll"]
```

### Docker Compose Template:
```yaml
version: '3.8'

services:
  api:
    build: ./src/WolfBlockchain.API
    ports:
      - "5000:5000"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=WolfBlockchainDb;User SA;Password=YourPasswordHere123!;
    depends_on:
      - db

  db:
    image: mcr.microsoft.com/mssql/server:latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPasswordHere123!
    ports:
      - "1433:1433"
```

### GitHub Actions Template:
```yaml
name: CI/CD Pipeline

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '10.0.x'
      - run: dotnet build
      - run: dotnet test
```

---

## ✅ EXPECTED RESULTS MAINE

### After Dockerfile:
```
✅ Docker image builds: SUCCESS
✅ Image size: ~500MB (optimized)
✅ Container runs: SUCCESS
```

### After Docker Compose:
```
✅ docker-compose up: RUNNING
✅ API accessible: http://localhost:5000
✅ Swagger accessible: http://localhost:5000/swagger
✅ /health returns: 200 OK
```

### After GitHub Actions:
```
✅ Workflow triggers on push: YES
✅ Tests run automatically: YES
✅ Docker image pushed to registry: YES
```

---

## 🎯 TIME ESTIMATES MAINE

| Task | Time |
|------|------|
| Dockerfile | 30-45 min |
| Docker Compose | 30-45 min |
| GitHub Actions | 1-1.5 hours |
| Testing & Fixes | 30-45 min |
| **TOTAL** | **3-4 hours** |

---

## 📊 SUCCESS CRITERIA

✅ Dockerfile builds successfully
✅ Docker image runs locally
✅ docker-compose.yml deploys all services
✅ GitHub Actions workflow runs on push
✅ All 60+ tests pass in Docker
✅ API responds correctly in container
✅ No security vulnerabilities

---

## 🚀 MAINE QUICK START

```bash
# 1. Verify current state
dotnet build
dotnet test

# 2. Create Dockerfile
# - Multi-stage build
# - Health checks
# - Proper base images

# 3. Create docker-compose.yml
# - API + DB services
# - Network config
# - Volume mounts

# 4. Create GitHub Actions
# - Build & test
# - Docker push
# - Deployment

# 5. Test locally
docker build -t wolfblockchain:latest .
docker-compose up
# Test /health endpoint

# 6. Push to GitHub
git add .
git commit -m "WEEK 5: Docker & CI/CD"
git push
# GitHub Actions should trigger automatically!
```

---

## 📞 NOTES FOR MAINE

- Verify JWT_SECRET is set (32+ chars)
- SQL Server needs SA password in docker-compose
- .dockerignore file prevents copying unnecessary files
- GitHub Actions needs repo secrets configured
- Test both HTTP and HTTPS endpoints
- Monitor Docker image size (should be < 1GB)

---

**READY FOR MAINE!** 🎯
**WEEK 5 - INFRASTRUCTURE STARTS HERE!** 🚀
