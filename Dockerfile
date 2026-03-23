# ============================================
# 🐺 WOLF BLOCKCHAIN - PRODUCTION DOCKERFILE
# ============================================
# Multi-stage build pentru .NET 10
# Optimized for production deployment

# ============= STAGE 1: BUILD =============
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/WolfBlockchain.API/WolfBlockchain.API.csproj", "src/WolfBlockchain.API/"]
COPY ["src/WolfBlockchain.Core/WolfBlockchain.Core.csproj", "src/WolfBlockchain.Core/"]
COPY ["src/WolfBlockchain.Storage/WolfBlockchain.Storage.csproj", "src/WolfBlockchain.Storage/"]
COPY ["src/WolfBlockchain.Wallet/WolfBlockchain.Wallet.csproj", "src/WolfBlockchain.Wallet/"]

# Restore dependencies (cached layer)
RUN dotnet restore "src/WolfBlockchain.API/WolfBlockchain.API.csproj"

# Copy all source files
COPY src/ src/

# Build the application
WORKDIR /src/src/WolfBlockchain.API
RUN dotnet build "WolfBlockchain.API.csproj" -c Release -o /app/build

# ============= STAGE 2: PUBLISH =============
FROM build AS publish
RUN dotnet publish "WolfBlockchain.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============= STAGE 3: RUNTIME =============
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app from build stage
COPY --from=publish /app/publish .

# Create directories for logs
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Expose ports
EXPOSE 5000
EXPOSE 5443

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5000/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "WolfBlockchain.API.dll"]
