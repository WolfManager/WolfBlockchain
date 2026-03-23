# 🐺 WOLF BLOCKCHAIN - PLAN ASTĂZI (3-4 ORE)

## 📅 Data: 26 Ianuarie 2024
## 🎯 WEEK 5 - INFRASTRUCTURE (Docker, CI/CD)

---

## ⏰ TIMELINE ASTĂZI

```
09:00 - 09:45  → Task 1: Dockerfile Creation (45 min)
09:45 - 10:30  → Task 2: Docker Compose (45 min)
10:30 - 10:40  → PAUZĂ SCURTĂ (10 min)
10:40 - 12:00  → Task 3: GitHub Actions CI/CD (80 min)
12:00 - 12:30  → Task 4: Local Testing & Validation (30 min)
12:30 - 13:00  → Task 5: Documentation & Cleanup (30 min)

TOTAL: 3h 30min (flexible până la 4h)
```

---

## 📋 TASK 1: DOCKERFILE (45 min)

### Obiectiv:
Creează un **production-grade Dockerfile** pentru WolfBlockchain.API

### Subtasks:
1. ✅ **Create Dockerfile** (15 min)
   - Multi-stage build (build + runtime)
   - Base: `mcr.microsoft.com/dotnet/sdk:10.0`
   - Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`
   - Copy source files
   - Restore & Build
   - Publish to /app/publish

2. ✅ **Add Health Check** (5 min)
   - HEALTHCHECK command
   - Check /health endpoint

3. ✅ **Optimize Image Size** (10 min)
   - Remove unnecessary files
   - Use slim runtime image
   - Multi-stage cleanup

4. ✅ **Create .dockerignore** (5 min)
   - Exclude bin/, obj/, logs/
   - Exclude node_modules, .git
   - Exclude test projects

5. ✅ **Test Build Locally** (10 min)
   ```bash
   docker build -t wolfblockchain:latest .
   docker images
   ```

### Expected Output:
```
✅ Dockerfile created in root
✅ .dockerignore created
✅ Docker image builds successfully
✅ Image size: ~500-700MB (optimized)
```

---

## 📋 TASK 2: DOCKER COMPOSE (45 min)

### Obiectiv:
Creează **docker-compose.yml** pentru dev environment (API + Database)

### Subtasks:
1. ✅ **Create docker-compose.yml** (15 min)
   - API service definition
   - SQL Server service definition
   - Network configuration
   - Volume mounts

2. ✅ **Configure Services** (15 min)
   - API:
     - Build from ./src/WolfBlockchain.API
     - Ports: 5000:5000, 5443:5443
     - Environment variables
     - Depends on: db
   
   - SQL Server:
     - Image: mcr.microsoft.com/mssql/server:2022-latest
     - Ports: 1433:1433
     - Environment: SA_PASSWORD, ACCEPT_EULA
     - Volume: sqldata:/var/opt/mssql

3. ✅ **Create docker-compose.dev.yml** (Optional - 10 min)
   - Dev-specific overrides
   - Hot reload support
   - Debug ports

4. ✅ **Test Docker Compose** (5 min)
   ```bash
   docker-compose up -d
   docker-compose ps
   curl http://localhost:5000/health
   ```

### Expected Output:
```
✅ docker-compose.yml created
✅ docker-compose.dev.yml created (optional)
✅ Services start successfully
✅ API accessible at http://localhost:5000
✅ Database connection working
```

---

## 📋 TASK 3: GITHUB ACTIONS CI/CD (80 min)

### Obiectiv:
Creează **automated CI/CD pipeline** cu GitHub Actions

### Subtasks:
1. ✅ **Create Workflow Directory** (5 min)
   ```bash
   mkdir -p .github/workflows
   ```

2. ✅ **Create ci-cd.yml Workflow** (30 min)
   - Trigger: push to main/develop
   - Jobs:
     - build (compile & test)
     - docker (build & push image)
     - deploy (optional - for later)

3. ✅ **Build Job Configuration** (15 min)
   - Checkout code
   - Setup .NET 10
   - Restore dependencies
   - Build solution
   - Run tests (60+ tests)
   - Upload test results

4. ✅ **Docker Job Configuration** (15 min)
   - Login to Docker Hub
   - Build Docker image
   - Tag image (latest + version)
   - Push to registry
   - Only on main branch

5. ✅ **Configure Secrets** (10 min)
   - DOCKER_USERNAME
   - DOCKER_PASSWORD
   - JWT_SECRET
   - Add to GitHub repo settings

6. ✅ **Test Workflow** (5 min)
   - Push to GitHub
   - Verify workflow triggers
   - Check build status

### Expected Output:
```
✅ .github/workflows/ci-cd.yml created
✅ Workflow triggers on push
✅ Build job passes
✅ Tests run successfully (60+ tests)
✅ Docker image builds & pushes
✅ Badge shows passing status
```

---

## 📋 TASK 4: LOCAL TESTING & VALIDATION (30 min)

### Obiectiv:
Validează că **totul funcționează corect**

### Subtasks:
1. ✅ **Test Docker Build** (10 min)
   ```bash
   docker build -t wolfblockchain:latest .
   docker run -d -p 5000:5000 wolfblockchain:latest
   ```

2. ✅ **Test Endpoints** (10 min)
   - GET /health → 200 OK
   - GET /swagger → UI loads
   - GET /api/monitoring/health-detailed → JSON response
   - POST /api/security/register → User created

3. ✅ **Test Docker Compose** (5 min)
   ```bash
   docker-compose down
   docker-compose up -d
   docker-compose logs api
   ```

4. ✅ **Verify Database Connection** (5 min)
   - Check logs for migration success
   - Verify SQL Server container running
   - Test API database operations

### Expected Output:
```
✅ Docker container runs successfully
✅ All endpoints respond correctly
✅ Database migrations run
✅ API connects to SQL Server
✅ No errors in logs
```

---

## 📋 TASK 5: DOCUMENTATION & CLEANUP (30 min)

### Obiectiv:
Documentează deployment și curăță workspace

### Subtasks:
1. ✅ **Create DEPLOYMENT.md** (15 min)
   - Docker build instructions
   - Docker Compose usage
   - Environment variables
   - Troubleshooting guide

2. ✅ **Update PROGRESS_TRACKER.md** (10 min)
   - Mark WEEK 5 as complete
   - Add Docker & CI/CD details
   - Update statistics

3. ✅ **Cleanup** (5 min)
   - Remove unused files
   - Commit all changes
   - Push to GitHub

### Expected Output:
```
✅ DEPLOYMENT.md created
✅ PROGRESS_TRACKER.md updated
✅ All changes committed
✅ GitHub Actions triggered
✅ WEEK 5 COMPLETE ✅
```

---

## 📊 SUCCESS CRITERIA (END OF DAY)

### Must Have:
- [✅] Dockerfile builds successfully
- [✅] Docker image size < 1GB
- [✅] docker-compose.yml deploys all services
- [✅] API accessible at http://localhost:5000
- [✅] All 60+ tests pass
- [✅] GitHub Actions workflow runs
- [✅] Docker image pushed to registry

### Nice to Have:
- [✅] docker-compose.dev.yml for development
- [✅] Automated version tagging
- [✅] Health check in Dockerfile
- [✅] DEPLOYMENT.md documentation

---

## 🎯 KEY FILES TO CREATE TODAY

```
ROOT/
├── Dockerfile ⭐
├── .dockerignore ⭐
├── docker-compose.yml ⭐
├── docker-compose.dev.yml (optional)
├── .github/
│   └── workflows/
│       └── ci-cd.yml ⭐
├── DEPLOYMENT.md ⭐
└── PROGRESS_TRACKER.md (update)
```

---

## 🚀 QUICK COMMANDS REFERENCE

### Docker Commands:
```bash
# Build image
docker build -t wolfblockchain:latest .

# Run container
docker run -d -p 5000:5000 --name wolf-api wolfblockchain:latest

# Check logs
docker logs wolf-api

# Stop & remove
docker stop wolf-api
docker rm wolf-api

# Cleanup
docker system prune -a
```

### Docker Compose Commands:
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f api

# Rebuild
docker-compose up -d --build

# Check status
docker-compose ps
```

### Git Commands:
```bash
# Commit Docker files
git add Dockerfile docker-compose.yml .github/workflows/
git commit -m "WEEK 5: Docker & CI/CD Infrastructure"
git push origin main

# GitHub Actions will trigger automatically!
```

---

## 📝 ENVIRONMENT VARIABLES NEEDED

### Docker Compose:
```yaml
API:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings__DefaultConnection=Server=db;Database=WolfBlockchainDb;User=sa;Password=YourStrong@Password123
  - Jwt__Secret=YOUR_32_CHARACTER_SECRET_KEY_HERE_PRODUCTION

SQL Server:
  - ACCEPT_EULA=Y
  - SA_PASSWORD=YourStrong@Password123
  - MSSQL_PID=Developer
```

### GitHub Secrets:
```
DOCKER_USERNAME: your-dockerhub-username
DOCKER_PASSWORD: your-dockerhub-token
JWT_SECRET: 32+ character secret
```

---

## ⚠️ TROUBLESHOOTING NOTES

### Docker Build Issues:
- Ensure .NET 10 SDK available
- Check internet connection for NuGet restore
- Verify paths in Dockerfile

### Docker Compose Issues:
- Port 5000/1433 not in use
- SQL Server needs 2GB RAM minimum
- Wait 30s for SQL Server to start

### GitHub Actions Issues:
- Check secrets configured
- Verify workflow syntax (YAML)
- Review logs in Actions tab

---

## 🎯 END OF DAY CHECKLIST

- [ ] Dockerfile created & tested
- [ ] .dockerignore created
- [ ] docker-compose.yml working
- [ ] GitHub Actions workflow created
- [ ] All tests pass in Docker
- [ ] Image pushed to registry
- [ ] Documentation updated
- [ ] WEEK 5 marked complete
- [ ] Ready for WEEK 6 (Documentation)

---

## 🚀 READY TO START?

**First Command:**
```bash
# Verify current state
cd /path/to/WolfBlockchain
dotnet build
dotnet test

# Expected: BUILD SUCCESSFUL, 60+ tests passing ✅
```

**Then we start with Task 1: Dockerfile!**

---

**PLAN CREATED** ✅
**READY FOR 3-4 HOURS OF WORK!** 💪🐺
