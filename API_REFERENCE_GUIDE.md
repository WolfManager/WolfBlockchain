# WolfBlockchain API Reference Guide

## Base URL
```
Development:  http://localhost:5000
Staging:      https://staging.wolf-blockchain.local
Production:   https://api.wolf-blockchain.com
```

## Authentication

All endpoints require JWT bearer token (except `/health`, `/metrics`, `/swagger`).

### Getting a Token

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "secure_password"
}

Response:
{
  "token": "eyJhbGc...",
  "expiresIn": 86400,
  "refreshToken": "refresh_token_..."
}
```

### Using Token in Requests

```http
GET /api/admindashboard/summary
Authorization: Bearer eyJhbGc...
```

---

## Public Endpoints (No Auth Required)

### Health Check
```http
GET /health
```

**Response**: `200 OK`
```json
{
  "status": "Healthy",
  "timestamp": "2026-03-22T12:00:00Z"
}
```

**Use**: Kubernetes liveness probe, monitoring

---

### Metrics (Prometheus)
```http
GET /metrics
```

**Response**: `200 OK`
```
# HELP request_duration_seconds Request duration in seconds
# TYPE request_duration_seconds histogram
request_duration_seconds_bucket{le="0.1"} 120
request_duration_seconds_bucket{le="0.5"} 145
...
```

**Use**: Prometheus scraping, Grafana dashboards

---

## Admin Dashboard API (JWT Required)

### Get Dashboard Summary

```http
GET /api/admindashboard/summary
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "totalUsers": 150,
  "totalTokens": 8,
  "totalValidators": 12,
  "totalStaked": 120000000,
  "activeAITrainingJobs": 0,
  "deployedSmartContracts": 5,
  "lastUpdatedAt": "2026-03-22T12:00:00Z"
}
```

**Cache**: 5 minutes
**Performance**: ~5ms (cached), ~50ms (first call)

---

### List Users (Paginated)

```http
GET /api/admindashboard/users?page=1&pageSize=10
Authorization: Bearer {token}
```

**Query Parameters**:
- `page` (int, required): Page number (1-based)
- `pageSize` (int, required): Items per page (1-100)

**Response**: `200 OK`
```json
{
  "users": [
    {
      "userId": "USR001",
      "username": "alex_crypto",
      "address": "WOLF8a2c4...",
      "role": "Validator",
      "balance": 50000,
      "status": "Active",
      "createdAt": "2024-01-15"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 10
}
```

**Cache**: 10 minutes (per page)
**Performance**: ~10ms (cached)

---

### List Tokens (Paginated)

```http
GET /api/admindashboard/tokens?page=1&pageSize=10
Authorization: Bearer {token}
```

**Query Parameters**:
- `page` (int, required): Page number (1-based)
- `pageSize` (int, required): Items per page (1-100)

**Response**: `200 OK`
```json
{
  "tokens": [
    {
      "tokenId": "TOKEN001",
      "name": "Wolf Coin",
      "symbol": "WOLF",
      "tokenType": "Native",
      "totalSupply": 1000000000,
      "currentSupply": 450000000,
      "status": "Active",
      "creatorAddress": "WOLFADMIN",
      "createdAt": "2024-01-10"
    }
  ],
  "totalCount": 8,
  "page": 1,
  "pageSize": 10
}
```

**Cache**: 10 minutes (per page)

---

### Recent Events/Activity

```http
GET /api/admindashboard/recent-events?limit=10
Authorization: Bearer {token}
```

**Query Parameters**:
- `limit` (int, optional): Max events to return (default: 10, max: 100)

**Response**: `200 OK`
```json
[
  {
    "eventType": "BlockAdded",
    "blockNumber": 1234,
    "timestamp": "2026-03-22T11:55:00Z",
    "message": "New block 0x1234... with 15 transactions"
  },
  {
    "eventType": "Transaction",
    "hash": "0x123abc",
    "amount": 100,
    "timestamp": "2026-03-22T11:53:00Z",
    "message": "Transaction 100 WOLF from 0xaaaa... to 0xbbbb..."
  }
]
```

**Cache**: 2 minutes

---

## Real-Time Updates (SignalR)

### Connect to Hub

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.wolf-blockchain.com/blockchain-hub")
  .withAutomaticReconnect()
  .build();

connection.start();
```

### Subscribe to Updates

```javascript
connection.send("SubscribeToUpdates");
```

### Listen for Events

```javascript
// Block added event
connection.on("BlockAdded", (event) => {
  console.log(`New block: ${event.blockHash}`);
  console.log(`Transactions: ${event.transactionCount}`);
});

// Transaction confirmed
connection.on("TransactionConfirmed", (event) => {
  console.log(`Transaction confirmed: ${event.message}`);
});

// Network stats updated
connection.on("NetworkStatsUpdated", (stats) => {
  console.log(`Blocks: ${stats.totalBlocks}`);
  console.log(`Transactions: ${stats.totalTransactions}`);
});
```

### Unsubscribe

```javascript
connection.send("UnsubscribeFromUpdates");
```

---

## Error Responses

### 400 Bad Request
```json
{
  "error": "Invalid pagination parameters"
}
```

### 401 Unauthorized
```json
{
  "error": "Unauthorized"
}
```

### 403 Forbidden
```json
{
  "error": "Access denied - insufficient permissions"
}
```

### 404 Not Found
```json
{
  "error": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "error": "An unexpected error occurred"
}
```

---

## Rate Limiting

**Limit**: 100 requests per minute per IP
**Headers in Response**:
- `X-RateLimit-Limit`: 100
- `X-RateLimit-Remaining`: 87
- `X-RateLimit-Reset`: 1234567890

**Exceeded Limit**: `429 Too Many Requests`

---

## Performance Tips

### Caching Strategy
- Summary dashboard: Cached 5 minutes
  - First request: ~50ms
  - Subsequent: ~5ms
  - Clear cache: Not needed (auto-expiry)

- User/token lists: Cached 10 minutes per page
  - Requesting same page again is ~10x faster
  - Different page starts fresh cache

### Pagination Best Practices
```
DON'T:   Request all 1000 users at once
DO:      Request 10-50 per page, paginate

DON'T:   Request every second
DO:      Request when user interacts or every 30+ seconds

DON'T:   Poll /api/admindashboard/*
DO:      Use SignalR for real-time updates
```

### Batch Operations
- Token list with pagination reduces API hits
- SignalR avoids polling overhead
- Caching reduces database queries by 70-80%

---

## Example Requests

### cURL

#### Get Dashboard Summary
```bash
curl -H "Authorization: Bearer TOKEN" \
  https://api.wolf-blockchain.com/api/admindashboard/summary
```

#### List Tokens (First Page)
```bash
curl -H "Authorization: Bearer TOKEN" \
  "https://api.wolf-blockchain.com/api/admindashboard/tokens?page=1&pageSize=10"
```

#### Health Check
```bash
curl https://api.wolf-blockchain.com/health
```

### JavaScript (Fetch API)

```javascript
// With authentication
const response = await fetch('https://api.wolf-blockchain.com/api/admindashboard/summary', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const data = await response.json();
console.log(data);
```

### Python (Requests)

```python
import requests

headers = {
    'Authorization': f'Bearer {token}',
    'Content-Type': 'application/json'
}

response = requests.get(
    'https://api.wolf-blockchain.com/api/admindashboard/summary',
    headers=headers
)

print(response.json())
```

---

## Webhook Events (Future)

**Not yet implemented. Planned for v2.1.0**

```http
POST /api/webhooks/register
Authorization: Bearer {token}

{
  "eventType": "TokenCreated",
  "url": "https://yourapp.com/webhooks/token-created",
  "secret": "webhook_secret_key"
}
```

---

## SDK & Libraries

### JavaScript/TypeScript
```bash
npm install @signalr/signalr @signalr/signalr-json-protocol
```

### Python
```bash
pip install websockets
```

### .NET/C#
```bash
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

---

## Support & Documentation

- **Swagger UI**: https://api.wolf-blockchain.com/swagger
- **OpenAPI Spec**: https://api.wolf-blockchain.com/swagger/v1/swagger.json
- **Documentation**: https://docs.wolf-blockchain.com
- **Status Page**: https://status.wolf-blockchain.com

---

**API Versioning**: Currently v1. Future versions will be /api/v2/, /api/v3/, etc.
**Last Updated**: 2026-03-22
**API Version**: v2.0.0
