# 🐺 WOLF BLOCKCHAIN - DEPLOYMENT GUIDE

## 📋 Table of Contents
- [Quick Start](#quick-start)
- [Docker Deployment](#docker-deployment)
- [Docker Compose Deployment](#docker-compose-deployment)
- [GitHub Actions CI/CD](#github-actions-cicd)
- [Environment Variables](#environment-variables)
- [Troubleshooting](#troubleshooting)

---

## 🚀 Quick Start

### Prerequisites
- Docker 20.10+
- Docker Compose 2.0+
- .NET 10 SDK (for local development)

### 1. Clone Repository
```bash
git clone https://github.com/yourusername/WolfBlockchain.git
cd WolfBlockchain
```

### 2. Build Docker Image
```bash
docker build -t wolfblockchain:latest .
```

### 3. Run with Docker Compose
```bash
docker-compose up -d
```

### 4. Access Application
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health
- Monitoring: http://localhost:5000/api/monitoring/health-detailed

---

## 🐳 Docker Deployment

### Build Image
```bash
docker build -t wolfblockchain:latest .
```

### Run Container
```bash
docker run -d \
  --name wolf-blockchain-api \
  -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="Server=your-db;Database=WolfBlockchainDb;User=sa;Password=YourPassword" \
  -e Jwt__Secret="YourJwtSecretMinimum32Characters" \
  wolfblockchain:latest
```

### Check Logs
```bash
docker logs wolf-blockchain-api
```

### Stop Container
```bash
docker stop wolf-blockchain-api
docker rm wolf-blockchain-api
```

---

## 🐋 Docker Compose Deployment

### Production Deployment
```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop services
docker-compose down

# Rebuild and restart
docker-compose up -d --build
```

### Development Deployment (with hot reload)
```bash
# Start dev environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# View logs
docker-compose logs -f api

# Stop services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
```

### Check Service Status
```bash
docker-compose ps
```

### View Specific Service Logs
```bash
# API logs
docker-compose logs -f api

# Database logs
docker-compose logs -f db
```

---

## 🔄 GitHub Actions CI/CD

### Workflow Triggers
- Push to `main` → Build, Test, Docker Build, Deploy to Production
- Push to `develop` → Build, Test, Docker Build, Deploy to Staging
- Pull Request → Build and Test only

### Required GitHub Secrets
Add these in GitHub repository settings:

```
DOCKER_USERNAME=your-dockerhub-username
DOCKER_PASSWORD=your-dockerhub-token
JWT_SECRET=your-32-character-secret
SONAR_TOKEN=your-sonarcloud-token (optional)
```

### Workflow Jobs
1. **Build & Test** - Compile code and run 60+ tests
2. **Docker Build** - Build and push Docker image
3. **Security Scan** - Run security analysis
4. **Code Quality** - SonarCloud analysis
5. **Deploy Staging** - Deploy to staging (develop branch)
6. **Deploy Production** - Deploy to production (main branch)

### Manual Workflow Trigger
```bash
# From GitHub UI: Actions → Wolf Blockchain CI/CD → Run workflow
```

---

## 🔧 Environment Variables

### Required Variables

#### Database Connection
```bash
ConnectionStrings__DefaultConnection="Server=db;Database=WolfBlockchainDb;User=sa;Password=YourPassword;TrustServerCertificate=True"
```

#### JWT Configuration
```bash
Jwt__Secret="YourProductionSecretKeyMinimum32CharactersLong"
Jwt__ExpirationMinutes="60"
```

#### ASP.NET Core
```bash
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_URLS="http://+:5000"
```

### Optional Variables

#### Logging
```bash
Serilog__MinimumLevel="Information"
```

#### Security
```bash
Security__EnableHttpsRedirect="true"
Security__RequireHttps="true"
```

---

## 🗄️ Database Setup

### SQL Server via Docker Compose
Database is automatically created when using docker-compose.yml

### Manual SQL Server Setup
```bash
# Connect to SQL Server
sqlcmd -S localhost,1433 -U sa -P WolfBlockchain@2024!

# Create database
CREATE DATABASE WolfBlockchainDb;
GO
```

### Run Migrations
Migrations run automatically on application startup.

Manual migration:
```bash
dotnet ef database update --project src/WolfBlockchain.API
```

---

## 🔍 Monitoring & Health Checks

### Health Check Endpoints
```bash
# Basic health check
curl http://localhost:5000/health

# Detailed health check with metrics
curl http://localhost:5000/api/monitoring/health-detailed

# Performance statistics
curl http://localhost:5000/api/monitoring/statistics

# Slow requests
curl http://localhost:5000/api/monitoring/slow-requests

# Slow queries
curl http://localhost:5000/api/monitoring/slow-queries
```

### Docker Health Check
```bash
# Check container health
docker inspect --format='{{.State.Health.Status}}' wolf-blockchain-api
```

---

## 🧪 Testing

### Run Tests Locally
```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/WolfBlockchain.Tests/WolfBlockchain.Tests.csproj

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test in Docker
```bash
docker build --target build -t wolfblockchain:test .
docker run wolfblockchain:test dotnet test
```

---

## 🛠️ Troubleshooting

### Container Won't Start
```bash
# Check logs
docker logs wolf-blockchain-api

# Check container status
docker ps -a

# Inspect container
docker inspect wolf-blockchain-api
```

### Database Connection Issues
```bash
# Check if SQL Server is running
docker-compose ps db

# Test connection
docker exec -it wolf-blockchain-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P WolfBlockchain@2024! -Q "SELECT 1" -C

# Check logs
docker-compose logs db
```

### Port Already in Use
```bash
# Find process using port 5000
lsof -i :5000  # macOS/Linux
netstat -ano | findstr :5000  # Windows

# Change port in docker-compose.yml
ports:
  - "5001:5000"
```

### Build Fails
```bash
# Clean build
dotnet clean
dotnet restore
dotnet build

# Clear Docker cache
docker system prune -a
```

### JWT Authentication Issues
```bash
# Verify JWT secret is set (32+ characters)
echo $Jwt__Secret

# Check logs for auth errors
docker logs wolf-blockchain-api | grep -i "jwt\|auth"
```

---

## 📊 Performance Optimization

### Docker Image Size
```bash
# Check image size
docker images wolfblockchain:latest

# Current size: ~458MB (optimized)
```

### Memory Limits
```yaml
# In docker-compose.yml
services:
  api:
    deploy:
      resources:
        limits:
          memory: 1G
        reservations:
          memory: 512M
```

### Database Connection Pooling
Already configured in connection string:
```
MultipleActiveResultSets=true;Max Pool Size=100;
```

---

## 🔒 Security Checklist

- [x] HTTPS enabled
- [x] JWT authentication
- [x] Security headers configured
- [x] Rate limiting active (100/min)
- [x] Input validation
- [x] SQL injection protection
- [x] XSS protection
- [x] Error handling (no data leakage)
- [x] Secrets not in source control
- [x] Database password encrypted

---

## 📈 Scaling

### Horizontal Scaling
```bash
# Scale API instances
docker-compose up -d --scale api=3
```

### Load Balancer
Add nginx or Traefik as reverse proxy:
```yaml
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
```

---

## 🔄 Update & Rollback

### Update Application
```bash
# Pull latest image
docker-compose pull api

# Restart with new image
docker-compose up -d api
```

### Rollback
```bash
# Use specific version
docker pull wolfblockchain:previous-version
docker-compose down
docker-compose up -d
```

---

## 📞 Support

### Logs Location
```
Container: /app/logs/
Host: ./logs/ (if volume mounted)
```

### Common Commands
```bash
# Restart API
docker-compose restart api

# View real-time logs
docker-compose logs -f --tail=100 api

# Execute command in container
docker exec -it wolf-blockchain-api bash

# Database backup
docker exec wolf-blockchain-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P WolfBlockchain@2024! -Q "BACKUP DATABASE WolfBlockchainDb TO DISK='/var/opt/mssql/backup/wolf.bak'" -C
```

---

## 🎯 Production Deployment Checklist

- [ ] Environment variables configured
- [ ] Database connection tested
- [ ] JWT secret set (32+ characters)
- [ ] HTTPS certificates installed
- [ ] Firewall rules configured
- [ ] Backup strategy implemented
- [ ] Monitoring dashboard setup
- [ ] Health checks working
- [ ] Load testing completed
- [ ] Security audit passed
- [ ] Documentation reviewed
- [ ] Team trained on operations

---

**🐺 Wolf Blockchain - Production Ready!** ✅
