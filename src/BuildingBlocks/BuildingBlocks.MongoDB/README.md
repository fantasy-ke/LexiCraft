# BuildingBlocks.MongoDB

Enhanced MongoDB support for LexiCraft with built-in resilience patterns and performance monitoring.

## Features

- **Resilience Patterns**: Automatic retry with exponential backoff and jitter
- **Performance Monitoring**: Real-time operation tracking and metrics
- **Connection Pooling**: Optimized connection management
- **Error Handling**: MongoDB-specific error classification and handling
- **Health Checks**: Built-in health monitoring

## Quick Start

### 1. Register Resilience and MongoDB Context

```csharp
var builder = WebApplication.CreateBuilder(args);

// First, register general resilience configuration
builder.AddResilience();

// Then, register MongoDB with resilience and monitoring
builder.AddMongoDbContext<YourDbContext>("MongoOptions");
```

### 2. Configuration

Add to your `appsettings.json`:

```json
{
  "MongoOptions": {
    "ConnectionString": "mongodb://localhost:27017/your-database",
    "MaxConnectionPoolSize": 100,
    "MinConnectionPoolSize": 10,
    "ConnectTimeout": "00:00:30",
    "SocketTimeout": "00:01:00",
    "ServerSelectionTimeout": "00:00:30"
  },
  "Resilience": {
    "RetryCount": 3,
    "BaseDelaySeconds": 1.0,
    "UseExponentialBackoff": true,
    "MaxDelaySeconds": 30.0,
    "JitterFactor": 0.1
  }
}
```

### 3. Use Resilient Repository

```csharp
public class YourRepository : ResilientMongoRepository<YourEntity>
{
    public YourRepository(
        IMongoDatabase database,
        IResilienceService resilienceService,        // 使用接口
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<YourRepository> logger)
        : base(database, resilienceService, performanceMonitor, logger)
    {
    }

    public async Task<List<YourEntity>> GetCustomDataAsync()
    {
        // Automatic retry and performance monitoring
        return await FindAsync(x => x.IsActive);
    }
}
```

## Resilience Configuration

| Option                  | Default | Description                              |
|-------------------------|---------|------------------------------------------|
| `RetryCount`            | 3       | Number of retry attempts                 |
| `BaseDelaySeconds`      | 1.0     | Base delay between retries               |
| `UseExponentialBackoff` | true    | Enable exponential backoff               |
| `MaxDelaySeconds`       | 30.0    | Maximum delay between retries            |
| `JitterFactor`          | 0.1     | Random jitter to prevent thundering herd |

## Performance Monitoring

The performance monitor automatically tracks:

- Operation response times (min, max, average)
- Operations per second
- Slow operations (>200ms)
- Operations by collection and type

### Accessing Metrics

```csharp
public class MetricsController : ControllerBase
{
    private readonly IMongoPerformanceMonitor _monitor;

    public MetricsController(IMongoPerformanceMonitor monitor)
    {
        _monitor = monitor;
    }

    [HttpGet("metrics")]
    public async Task<PerformanceMetrics> GetMetrics()
    {
        return await _monitor.GetMetricsAsync(TimeSpan.FromMinutes(5));
    }
}
```

## Error Handling

The resilience service automatically handles these MongoDB exceptions:

- **Connection Errors**: `MongoConnectionException`, network timeouts
- **Temporary Errors**: Write conflicts, node recovery, primary elections
- **Timeout Errors**: `MongoExecutionTimeoutException`, operation timeouts

Non-retryable errors (authentication, incompatible driver) are not retried.

## Health Checks

```csharp
// Check MongoDB health
var isHealthy = await resilienceService.IsHealthyAsync();
```

## Best Practices

1. **Use the resilient repository base class** for automatic retry and monitoring
2. **Configure appropriate timeouts** based on your application needs
3. **Monitor slow operations** and optimize queries accordingly
4. **Set reasonable retry limits** to avoid cascading failures
5. **Use connection pooling** for better performance under load

## Migration from Legacy Code

If you have existing MongoDB repositories, you can easily migrate:

1. Change base class from `MongoRepository<T>` to `ResilientMongoRepository<T>`
2. Update constructor to include `MongoResilienceService` and `IMongoPerformanceMonitor`
3. Remove manual retry logic - it's now handled automatically
4. Remove manual performance tracking - it's built-in

## Logging

The system automatically logs:

- Retry attempts with delay information
- Slow operations (>200ms as warnings)
- Very slow operations (>1000ms as errors)
- Connection failures and recovery

Configure your logging level to `Information` or higher to see retry attempts.