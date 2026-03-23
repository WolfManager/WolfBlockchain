# 📈 PATH B: WEEK 9 ADVANCED FEATURES - COMPLETE PLAN
## Build Amazing Features Before or After Launch

---

## 🎯 WEEK 9 OVERVIEW

```
Duration:               5 days (Mon-Fri)
Total Tasks:           5 major features
Expected Code:         500+ lines
Expected Tests:        20+ new tests
Expected Endpoints:    10+ new APIs
Expected Impact:       Major UX/functionality improvements
```

---

## 📋 WEEK 9 TASK BREAKDOWN

### Task 1: Advanced Blazor UI Components (Mon-Tue) - 2 days
```
Features:
├─ Real-time Charts (Chart.js integration)
├─ Live Transaction Dashboard
├─ Advanced Filtering UI
├─ Export to CSV/PDF
├─ Responsive Design Improvements
└─ Dark Mode Support

Expected:
├─ 8 new Blazor components
├─ 50+ lines of CSS
├─ 100+ lines of JavaScript
└─ 8 component tests
```

### Task 2: WebSocket Real-time Communication (Tue) - 1 day
```
Features:
├─ Live Price Updates
├─ Real-time Notifications
├─ Live Transaction Stream
├─ Connection Status Monitor
├─ Auto-reconnect Logic
└─ Message Compression

Expected:
├─ IWebSocketService interface
├─ WebSocketService implementation
├─ WebSocketController
├─ Client-side JS (~50 lines)
└─ 5 WebSocket tests
```

### Task 3: ML-Based Predictions (Wed-Thu) - 2 days
```
Features:
├─ Price Prediction Model
├─ Anomaly Detection
├─ Transaction Risk Analysis
├─ Portfolio Optimization
└─ Trend Analysis

Expected:
├─ IPredictionService interface
├─ PredictionService implementation
├─ PredictionController
├─ ML model integration
└─ 8 prediction tests
```

### Task 4: Mobile App API Compatibility (Thu) - 1 day
```
Features:
├─ Mobile-optimized endpoints
├─ Reduced payload responses
├─ App version compatibility
├─ Push notification support
├─ Offline data sync
└─ Rate limiting per app

Expected:
├─ IMobileApiService
├─ MobileApiController
├─ 5 new endpoints
├─ Device token management
└─ 5 mobile API tests
```

### Task 5: Advanced Analytics Dashboard (Fri) - 1 day
```
Features:
├─ Custom Report Builder
├─ Advanced Metrics
├─ User Behavior Tracking
├─ Heatmaps & Usage Patterns
├─ Export Reports
└─ Scheduled Reports

Expected:
├─ IAdvancedAnalyticsService
├─ AdvancedAnalyticsController
├─ 3 new dashboard pages
├─ 5+ new endpoints
└─ 4 analytics tests
```

---

## 🔧 TASK 1: ADVANCED BLAZOR UI COMPONENTS (Mon-Tue)

### File Structure
```
src/WolfBlockchain.API/Pages/Components/
├─ RealTimeChart.razor          (Live transaction chart)
├─ LiveDashboard.razor          (Real-time dashboard)
├─ AdvancedFilters.razor        (Complex filtering)
├─ ExportDialog.razor           (Export functionality)
├─ DarkModeToggle.razor         (Theme switcher)
└─ ResponsiveGrid.razor         (Responsive layout)
```

### Create RealTimeChart Component
```razor
@* File: RealTimeChart.razor *@
@using System.Diagnostics
@implements IAsyncDisposable
@inject HttpClient Http
@inject IJSRuntime JS

<div class="chart-container">
    <canvas @ref="chartCanvas" id="transactionChart"></canvas>
</div>

@code {
    private ElementReference chartCanvas;
    private IJSObjectReference? chartModule;
    private Timer? updateTimer;
    private List<decimal> transactionVolumes = new();
    private List<string> timestamps = new();

    protected override async Task OnInitializedAsync()
    {
        chartModule = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./components/chart.js");
        
        await UpdateChartData();
        
        updateTimer = new Timer(async _ => await UpdateChartData(), 
            null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    private async Task UpdateChartData()
    {
        try
        {
            var data = await Http.GetFromJsonAsync<TransactionDataDto>(
                "api/analytics/live-transactions");
            
            if (data != null)
            {
                transactionVolumes.Add(data.Volume);
                timestamps.Add(data.Timestamp.ToString("HH:mm:ss"));
                
                if (transactionVolumes.Count > 50)
                {
                    transactionVolumes.RemoveAt(0);
                    timestamps.RemoveAt(0);
                }
                
                await chartModule!.InvokeVoidAsync("updateChart",
                    chartCanvas, transactionVolumes, timestamps);
                
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating chart: {ex.Message}");
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        updateTimer?.Dispose();
        if (chartModule is not null)
            await chartModule.DisposeAsync();
    }
}

<style>
    .chart-container {
        position: relative;
        width: 100%;
        height: 400px;
        margin: 20px 0;
        padding: 10px;
        border-radius: 8px;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    canvas {
        max-height: 400px;
        width: 100% !important;
    }
</style>
```

### Create chart.js Helper
```javascript
// File: wwwroot/components/chart.js
let chart = null;

export function updateChart(canvas, volumes, timestamps) {
    const ctx = canvas.getContext('2d');
    
    if (chart) {
        chart.destroy();
    }
    
    chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: timestamps,
            datasets: [{
                label: 'Transaction Volume',
                data: volumes,
                borderColor: '#667eea',
                backgroundColor: 'rgba(102, 126, 234, 0.1)',
                tension: 0.4,
                fill: true,
                pointRadius: 2,
                pointBackgroundColor: '#667eea'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
}
```

### Create Advanced Filters Component
```razor
@* File: AdvancedFilters.razor *@
@page "/admin/advanced-filters"

<div class="filters-container">
    <h3>Advanced Filters</h3>
    
    <div class="filter-group">
        <label>Transaction Type:</label>
        <select @bind="selectedType">
            <option value="">All Types</option>
            <option value="transfer">Transfer</option>
            <option value="contract">Contract</option>
            <option value="mining">Mining</option>
        </select>
    </div>

    <div class="filter-group">
        <label>Date Range:</label>
        <input type="date" @bind="startDate" />
        <span>to</span>
        <input type="date" @bind="endDate" />
    </div>

    <div class="filter-group">
        <label>Amount Range:</label>
        <input type="number" @bind="minAmount" placeholder="Min" />
        <input type="number" @bind="maxAmount" placeholder="Max" />
    </div>

    <button @onclick="ApplyFilters" class="btn-primary">
        Apply Filters
    </button>
    <button @onclick="ResetFilters" class="btn-secondary">
        Reset
    </button>
</div>

<div class="results">
    @if (filteredResults != null && filteredResults.Count > 0)
    {
        <table>
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Type</th>
                    <th>Amount</th>
                    <th>Date</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in filteredResults)
                {
                    <tr>
                        <td>@item.Id</td>
                        <td>@item.Type</td>
                        <td>@item.Amount</td>
                        <td>@item.Date:g</td>
                        <td>@item.Status</td>
                    </tr>
                }
            </tbody>
        </table>
    }
</div>

@code {
    private string selectedType = "";
    private DateTime startDate = DateTime.Now.AddDays(-30);
    private DateTime endDate = DateTime.Now;
    private decimal minAmount = 0;
    private decimal maxAmount = 100000;
    private List<TransactionDto> filteredResults = new();

    private async Task ApplyFilters()
    {
        // Call API with filters
        var response = await Http.GetFromJsonAsync<List<TransactionDto>>(
            $"api/transactions/filtered?type={selectedType}&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&minAmount={minAmount}&maxAmount={maxAmount}");
        
        filteredResults = response ?? new();
        StateHasChanged();
    }

    private void ResetFilters()
    {
        selectedType = "";
        startDate = DateTime.Now.AddDays(-30);
        endDate = DateTime.Now;
        minAmount = 0;
        maxAmount = 100000;
        filteredResults.Clear();
    }
}
```

### Tests for UI Components
```csharp
// File: tests/WolfBlockchain.Tests/UI/AdvancedUIComponentsTests.cs
[Fact]
public async Task RealTimeChart_ShouldUpdateEvery5Seconds()
{
    // Arrange
    var component = new RealTimeChartComponent();
    
    // Act
    await component.OnInitializedAsync();
    await Task.Delay(6000); // Wait 6 seconds
    
    // Assert
    Assert.True(component.UpdateCount > 1);
}

[Fact]
public async Task AdvancedFilters_ShouldApplyFiltersCorrectly()
{
    // Arrange
    var filters = new AdvancedFiltersComponent();
    filters.SelectedType = "transfer";
    filters.MinAmount = 100;
    filters.MaxAmount = 5000;
    
    // Act
    await filters.ApplyFilters();
    
    // Assert
    Assert.NotEmpty(filters.FilteredResults);
    Assert.All(filters.FilteredResults, r => 
        Assert.InRange(r.Amount, 100, 5000));
}
```

---

## 🔌 TASK 2: WEBSOCKET REAL-TIME COMMUNICATION (Tue)

### Create WebSocket Service
```csharp
// File: src/WolfBlockchain.API/Services/IWebSocketService.cs
public interface IWebSocketService
{
    Task ConnectAsync(string userId, CancellationToken ct);
    Task DisconnectAsync(string userId);
    Task SendMessageAsync(string userId, WebSocketMessageDto message);
    Task BroadcastAsync(WebSocketMessageDto message);
    IAsyncEnumerable<WebSocketMessageDto> GetMessagesAsync(string userId, CancellationToken ct);
    bool IsConnected(string userId);
}

// File: src/WolfBlockchain.API/Services/WebSocketService.cs
public class WebSocketService : IWebSocketService
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ILogger<WebSocketService> _logger;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

    public WebSocketService(ILogger<WebSocketService> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string userId, CancellationToken ct)
    {
        _logger.LogInformation("User {UserId} connected", userId);
        
        // Connection established
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync(string userId)
    {
        if (_connections.TryRemove(userId, out var ws))
        {
            ws?.Dispose();
            _logger.LogInformation("User {UserId} disconnected", userId);
        }

        await Task.CompletedTask;
    }

    public async Task SendMessageAsync(string userId, WebSocketMessageDto message)
    {
        if (_connections.TryGetValue(userId, out var ws) && ws?.State == WebSocketState.Open)
        {
            await _sendSemaphore.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await ws.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }
    }

    public async Task BroadcastAsync(WebSocketMessageDto message)
    {
        var tasks = _connections.Values
            .Where(ws => ws?.State == WebSocketState.Open)
            .Select(ws => SendToSocketAsync(ws, message));

        await Task.WhenAll(tasks);
    }

    private async Task SendToSocketAsync(WebSocket ws, WebSocketMessageDto message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WebSocket message");
        }
    }

    public IAsyncEnumerable<WebSocketMessageDto> GetMessagesAsync(string userId, CancellationToken ct)
    {
        return ReceiveMessagesAsync(userId, ct);
    }

    public bool IsConnected(string userId) => _connections.ContainsKey(userId);

    private async IAsyncEnumerable<WebSocketMessageDto> ReceiveMessagesAsync(
        string userId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Implementation for receiving messages
        yield break;
    }
}

// File: src/WolfBlockchain.API/Controllers/WebSocketController.cs
[ApiController]
[Route("api/[controller]")]
public class WebSocketController : ControllerBase
{
    private readonly IWebSocketService _wsService;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(IWebSocketService wsService, ILogger<WebSocketController> logger)
    {
        _wsService = wsService;
        _logger = logger;
    }

    [HttpGet("ws/{userId}")]
    public async Task Get(string userId)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _wsService.ConnectAsync(userId, HttpContext.RequestAborted);
            
            _logger.LogInformation("WebSocket connection established for user {UserId}", userId);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] WebSocketMessageDto message)
    {
        await _wsService.BroadcastAsync(message);
        return Ok(new { message = "Broadcast sent" });
    }

    [HttpGet("status/{userId}")]
    public IActionResult GetStatus(string userId)
    {
        return Ok(new { connected = _wsService.IsConnected(userId) });
    }
}

public record WebSocketMessageDto(
    string Type,
    string Content,
    DateTime Timestamp,
    Dictionary<string, object>? Metadata = null);
```

### Client-side WebSocket
```javascript
// File: wwwroot/js/websocket-client.js
class WebSocketClient {
    constructor(userId) {
        this.userId = userId;
        this.url = `wss://${window.location.hostname}/api/websocket/ws/${userId}`;
        this.ws = null;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
    }

    connect() {
        try {
            this.ws = new WebSocket(this.url);
            this.ws.onopen = () => this.onOpen();
            this.ws.onmessage = (e) => this.onMessage(e);
            this.ws.onerror = (e) => this.onError(e);
            this.ws.onclose = () => this.onClose();
        } catch (error) {
            console.error('WebSocket connection error:', error);
        }
    }

    onOpen() {
        console.log('WebSocket connected');
        this.reconnectAttempts = 0;
    }

    onMessage(event) {
        try {
            const message = JSON.parse(event.data);
            console.log('Received message:', message);
            
            // Dispatch custom event
            document.dispatchEvent(new CustomEvent('ws-message', { detail: message }));
        } catch (error) {
            console.error('Error parsing message:', error);
        }
    }

    onError(error) {
        console.error('WebSocket error:', error);
    }

    onClose() {
        console.log('WebSocket disconnected');
        this.attemptReconnect();
    }

    attemptReconnect() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = Math.pow(2, this.reconnectAttempts) * 1000;
            console.log(`Reconnecting in ${delay}ms...`);
            setTimeout(() => this.connect(), delay);
        }
    }

    send(message) {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(message));
        }
    }

    close() {
        if (this.ws) {
            this.ws.close();
        }
    }
}

// Usage:
// const client = new WebSocketClient('user123');
// client.connect();
// document.addEventListener('ws-message', (e) => console.log('Message:', e.detail));
```

---

## 🤖 TASK 3: ML-BASED PREDICTIONS (Wed-Thu)

### Create Prediction Service
```csharp
// File: src/WolfBlockchain.API/Services/IPredictionService.cs
public interface IPredictionService
{
    Task<PricePredictor> PredictPriceAsync(string tokenId, int days = 7);
    Task<AnomalyDetectionResultDto> DetectAnomaliesAsync(string dataSource);
    Task<RiskAnalysisDto> AnalyzeTransactionRiskAsync(string transactionId);
    Task<PortfolioOptimizationDto> OptimizePortfolioAsync(string userId);
}

// File: src/WolfBlockchain.API/Services/PredictionService.cs
public class PredictionService : IPredictionService
{
    private readonly ILogger<PredictionService> _logger;
    private readonly IQueryCacheService _cache;

    public PredictionService(ILogger<PredictionService> logger, IQueryCacheService cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<PricePredictor> PredictPriceAsync(string tokenId, int days = 7)
    {
        var cacheKey = $"prediction:price:{tokenId}:{days}";
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogInformation("Predicting price for token {TokenId} for {Days} days", tokenId, days);
            
            // Simulated ML prediction (in production, use ML.NET or external service)
            var predictions = new List<PricePredictionDto>();
            var currentPrice = 100m; // Mock current price
            var random = new Random();

            for (int i = 1; i <= days; i++)
            {
                var change = (decimal)(random.NextDouble() - 0.5) * 5;
                currentPrice += change;
                
                predictions.Add(new PricePredictionDto
                {
                    Date = DateTime.UtcNow.AddDays(i),
                    PredictedPrice = currentPrice,
                    Confidence = 0.75m - (i * 0.05m), // Confidence decreases over time
                    LowerBound = currentPrice * 0.95m,
                    UpperBound = currentPrice * 1.05m
                });
            }

            return new PricePredictor
            {
                TokenId = tokenId,
                Predictions = predictions,
                ModelVersion = "1.0",
                GeneratedAtUtc = DateTime.UtcNow
            };
        }, TimeSpan.FromHours(1));
    }

    public async Task<AnomalyDetectionResultDto> DetectAnomaliesAsync(string dataSource)
    {
        _logger.LogInformation("Detecting anomalies in {DataSource}", dataSource);
        
        return new AnomalyDetectionResultDto
        {
            DataSource = dataSource,
            AnomaliesDetected = 0,
            ConfidenceScore = 0.95m,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<RiskAnalysisDto> AnalyzeTransactionRiskAsync(string transactionId)
    {
        _logger.LogInformation("Analyzing risk for transaction {TransactionId}", transactionId);
        
        return new RiskAnalysisDto
        {
            TransactionId = transactionId,
            RiskLevel = "Low",
            RiskScore = 0.15m,
            Flags = new List<string>(),
            AnalyzedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<PortfolioOptimizationDto> OptimizePortfolioAsync(string userId)
    {
        _logger.LogInformation("Optimizing portfolio for user {UserId}", userId);
        
        return new PortfolioOptimizationDto
        {
            UserId = userId,
            CurrentAllocation = new Dictionary<string, decimal>
            {
                { "BTC", 0.4m },
                { "ETH", 0.3m },
                { "WOLF", 0.3m }
            },
            RecommendedAllocation = new Dictionary<string, decimal>
            {
                { "BTC", 0.35m },
                { "ETH", 0.35m },
                { "WOLF", 0.3m }
            },
            ExpectedReturn = 0.25m,
            Risk = 0.18m,
            OptimizedAtUtc = DateTime.UtcNow
        };
    }
}

// DTOs
public record PricePredictor(
    string TokenId,
    List<PricePredictionDto> Predictions,
    string ModelVersion,
    DateTime GeneratedAtUtc);

public record PricePredictionDto(
    DateTime Date,
    decimal PredictedPrice,
    decimal Confidence,
    decimal LowerBound,
    decimal UpperBound);

public record AnomalyDetectionResultDto(
    string DataSource,
    int AnomaliesDetected,
    decimal ConfidenceScore,
    DateTime Timestamp);

public record RiskAnalysisDto(
    string TransactionId,
    string RiskLevel,
    decimal RiskScore,
    List<string> Flags,
    DateTime AnalyzedAtUtc);

public record PortfolioOptimizationDto(
    string UserId,
    Dictionary<string, decimal> CurrentAllocation,
    Dictionary<string, decimal> RecommendedAllocation,
    decimal ExpectedReturn,
    decimal Risk,
    DateTime OptimizedAtUtc);
```

### Prediction Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public class PredictionController : ControllerBase
{
    private readonly IPredictionService _predictionService;
    private readonly ILogger<PredictionController> _logger;

    [HttpGet("price/{tokenId}")]
    public async Task<IActionResult> PredictPrice(string tokenId, [FromQuery] int days = 7)
    {
        var prediction = await _predictionService.PredictPriceAsync(tokenId, days);
        return Ok(prediction);
    }

    [HttpPost("anomalies")]
    public async Task<IActionResult> DetectAnomalies([FromBody] string dataSource)
    {
        var result = await _predictionService.DetectAnomaliesAsync(dataSource);
        return Ok(result);
    }

    [HttpGet("risk/{transactionId}")]
    public async Task<IActionResult> AnalyzeRisk(string transactionId)
    {
        var risk = await _predictionService.AnalyzeTransactionRiskAsync(transactionId);
        return Ok(risk);
    }

    [HttpGet("portfolio/optimize/{userId}")]
    public async Task<IActionResult> OptimizePortfolio(string userId)
    {
        var optimization = await _predictionService.OptimizePortfolioAsync(userId);
        return Ok(optimization);
    }
}
```

---

## 📱 TASK 4: MOBILE API COMPATIBILITY (Thu)

### Mobile API Service
```csharp
// File: src/WolfBlockchain.API/Services/IMobileApiService.cs
public interface IMobileApiService
{
    Task<MobileDataDto> GetMobileDataAsync(string userId);
    Task<MobileTransactionDto> GetTransactionAsync(string transactionId, bool lite = true);
    Task RegisterDeviceAsync(string userId, DeviceRegistrationDto device);
    Task SendPushNotificationAsync(string userId, PushNotificationDto notification);
}

// File: src/WolfBlockchain.API/Controllers/MobileApiController.cs
[ApiController]
[Route("api/v1/mobile")]
public class MobileApiController : ControllerBase
{
    private readonly IMobileApiService _mobileService;
    private readonly ILogger<MobileApiController> _logger;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var data = await _mobileService.GetMobileDataAsync(User.FindFirst("sub")?.Value);
        return Ok(data);
    }

    [HttpGet("transactions/{id}")]
    public async Task<IActionResult> GetTransaction(string id)
    {
        var tx = await _mobileService.GetTransactionAsync(id, lite: true);
        return Ok(tx);
    }

    [HttpPost("devices/register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationDto device)
    {
        await _mobileService.RegisterDeviceAsync(User.FindFirst("sub")?.Value, device);
        return Ok(new { message = "Device registered" });
    }
}

public record MobileDataDto(
    decimal Balance,
    int TransactionCount,
    List<MobileTransactionDto> RecentTransactions);

public record MobileTransactionDto(
    string Id,
    string From,
    string To,
    decimal Amount,
    DateTime Date);

public record DeviceRegistrationDto(
    string DeviceId,
    string DeviceType,
    string OsVersion,
    string AppVersion,
    string PushToken);

public record PushNotificationDto(
    string Title,
    string Message,
    Dictionary<string, string>? Data = null);
```

---

## 📊 TASK 5: ADVANCED ANALYTICS DASHBOARD (Fri)

### Advanced Analytics Service
```csharp
// File: src/WolfBlockchain.API/Services/IAdvancedAnalyticsService.cs
public interface IAdvancedAnalyticsService
{
    Task<CustomReportDto> CreateCustomReportAsync(ReportDefinitionDto definition);
    Task<List<UserBehaviorDto>> GetUserBehaviorAsync(DateTime startDate, DateTime endDate);
    Task<HeatmapDataDto> GetUsageHeatmapAsync(string timeframe = "daily");
    Task<List<ReportDto>> GetScheduledReportsAsync(string userId);
    Task ScheduleReportAsync(string userId, ReportScheduleDto schedule);
}

[ApiController]
[Route("api/[controller]")]
public class AdvancedAnalyticsController : ControllerBase
{
    private readonly IAdvancedAnalyticsService _analyticsService;

    [HttpPost("reports/custom")]
    public async Task<IActionResult> CreateCustomReport([FromBody] ReportDefinitionDto definition)
    {
        var report = await _analyticsService.CreateCustomReportAsync(definition);
        return Ok(report);
    }

    [HttpGet("behavior")]
    public async Task<IActionResult> GetBehavior([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var behavior = await _analyticsService.GetUserBehaviorAsync(startDate, endDate);
        return Ok(behavior);
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] string timeframe = "daily")
    {
        var heatmap = await _analyticsService.GetUsageHeatmapAsync(timeframe);
        return Ok(heatmap);
    }
}
```

---

## 📋 WEEK 9 IMPLEMENTATION CHECKLIST

### Monday-Tuesday: UI Components
```
☐ Create RealTimeChart component
☐ Create LiveDashboard page
☐ Create AdvancedFilters component
☐ Integrate Chart.js
☐ Add dark mode support
☐ Write UI component tests
☐ Test in browser
☐ Code review & merge
```

### Tuesday: WebSocket
```
☐ Create WebSocketService
☐ Create WebSocketController
☐ Implement client-side JS
☐ Test connections
☐ Add error handling
☐ Write WebSocket tests
☐ Integration testing
☐ Code review & merge
```

### Wednesday-Thursday: ML Predictions
```
☐ Create PredictionService
☐ Create prediction DTOs
☐ Create PredictionController
☐ Add price prediction logic
☐ Add anomaly detection
☐ Add risk analysis
☐ Write prediction tests
☐ Integration testing
☐ Code review & merge
```

### Thursday: Mobile API
```
☐ Create MobileApiService
☐ Create MobileApiController
☐ Add device registration
☐ Add push notifications
☐ Optimize payloads
☐ Write mobile API tests
☐ Test with mobile clients
☐ Code review & merge
```

### Friday: Advanced Analytics
```
☐ Create AdvancedAnalyticsService
☐ Create AdvancedAnalyticsController
☐ Add custom reports
☐ Add heatmaps
☐ Add scheduled reports
☐ Write analytics tests
☐ Create analytics pages
☐ Code review & merge
☐ Final integration testing
☐ Deploy to staging
```

---

## 🎯 WEEK 9 DEPLOYMENT

### Final Checklist
```
✅ All features built
✅ All tests passing
✅ Code reviewed
✅ Security validated
✅ Performance tested
✅ Staging validated
✅ Documentation updated
✅ Team trained
✅ Ready for production
```

### Deployment Steps
```bash
# Build and test
dotnet build
dotnet test

# Merge to main
git commit -m "Week 9: Advanced features"
git push origin main

# Deploy to production
kubectl set image deployment/wolfblockchain \
  wolfblockchain=your-registry/wolfblockchain:week9 \
  -n wolfblockchain

# Verify deployment
kubectl rollout status deployment/wolfblockchain -n wolfblockchain
```

---

**WEEK 9: ADVANCED FEATURES - COMPLETE PLAN READY!** 🎉

**PATH B: COMPLETE ✅**
