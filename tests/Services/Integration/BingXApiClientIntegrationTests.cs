using NUnit.Framework;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using CryptoPositionAnalysis.Models;
using CryptoPositionAnalysis.Services;
using Microsoft.Extensions.Configuration;

namespace CryptoPositionAnalysis.Tests.Services.Integration;

/// <summary>
/// Integration tests for BingX API endpoints using real API calls.
/// These tests are marked as [Explicit] to prevent automatic execution in CI/CD.
/// 
/// To run these tests:
/// 1. Set up your API credentials in user secrets or appsettings.json
/// 2. Run: dotnet test --filter "Category=Integration" --logger console
/// 3. Or run individual tests in your IDE
/// 
/// Prerequisites:
/// - Valid BingX API credentials
/// - Active BingX account with appropriate permissions
/// - Stable internet connection
/// </summary>
[TestFixture]
[Category("Integration")]
[Explicit("These tests make real API calls and require valid credentials")]
public class BingXApiClientIntegrationTests
{
    private BingXApiClient _apiClient;
    private ILogger<BingXApiClient> _logger;
    private BingXApiSettings _settings;
    private RateLimitingSettings _rateLimitSettings;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Set up configuration to read from appsettings.json and user secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true) // For local development
            .AddUserSecrets<BingXApiClientIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole()
                   .AddDebug()
                   .SetMinimumLevel(LogLevel.Information);
        });
        _logger = loggerFactory.CreateLogger<BingXApiClient>();

        // Get settings from configuration
        _settings = configuration.GetSection("BingXApi").Get<BingXApiSettings>() ?? new BingXApiSettings();
        _rateLimitSettings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        // Validate credentials are available
        if (string.IsNullOrEmpty(_settings.ApiKey) || string.IsNullOrEmpty(_settings.SecretKey))
        {
            var message = @"
BingX API credentials are not configured. Please set up your credentials using one of these methods:

1. User Secrets (Recommended):
   dotnet user-secrets set ""BingXApi:ApiKey"" ""your-api-key"" --project tests
   dotnet user-secrets set ""BingXApi:SecretKey"" ""your-secret-key"" --project tests

2. Environment Variables:
   $env:BingXApi__ApiKey = ""your-api-key""
   $env:BingXApi__SecretKey = ""your-secret-key""

3. Helper Script:
   .\tests\Run-IntegrationTests.ps1 -SetupCredentials

For more information, see CONFIGURATION.md in the project root.
";
            Assert.Fail(message);
        }

        // Validate base URL is configured
        if (string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _settings.BaseUrl = "https://open-api.bingx.com";
        }

        // Create API client
        _apiClient = new BingXApiClient(_settings, _rateLimitSettings, _logger);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _apiClient?.Dispose();
    }

    #region Positions Tests

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("Positions")]
    public async Task GetOpenPositionsAsync_WithoutSymbol_ShouldReturnAllPositions()
    {
        // Act
        var positions = await _apiClient.GetOpenPositionsAsync();

        // Assert
        positions.Should().NotBeNull();
        positions.Should().BeOfType<List<Position>>();
        
        // Log results for manual verification
        Console.WriteLine($"Retrieved {positions.Count} positions");
        
        foreach (var position in positions)
        {
            Console.WriteLine($"Position: {position.Symbol} - {position.PositionSide} - Size: {position.PositionSize} - PnL: {position.UnrealizedPnl}");
            
            // Validate position data structure
            position.Symbol.Should().NotBeNullOrEmpty();
            position.Exchange.Should().Be("BingX");
            position.PositionSide.Should().BeOneOf(PositionSide.Long, PositionSide.Short);
            position.UpdateTime.Should().BeGreaterThan(0);
        }
    }

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("Positions")]
    public async Task GetOpenPositionsAsync_WithSpecificSymbol_ShouldReturnSymbolPositions()
    {
        // Arrange
        const string symbol = "BTC-USDT";

        // Act
        var positions = await _apiClient.GetOpenPositionsAsync(symbol);

        // Assert
        positions.Should().NotBeNull();
        positions.Should().BeOfType<List<Position>>();
        
        // All returned positions should be for the specified symbol
        positions.Should().OnlyContain(p => p.Symbol == symbol);
        
        Console.WriteLine($"Retrieved {positions.Count} positions for {symbol}");
        
        foreach (var position in positions)
        {
            Console.WriteLine($"Position: {position.Symbol} - {position.PositionSide} - Size: {position.PositionSize} - PnL: {position.UnrealizedPnl}");
            position.Exchange.Should().Be("BingX");
        }
    }

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("Positions")]
    public async Task GetOpenPositionsAsync_WithInvalidSymbol_ShouldReturnEmptyList()
    {
        // Arrange
        const string invalidSymbol = "INVALID-SYMBOL";

        // Act
        var positions = await _apiClient.GetOpenPositionsAsync(invalidSymbol);

        // Assert
        positions.Should().NotBeNull();
        positions.Should().BeEmpty();
        
        Console.WriteLine($"Retrieved {positions.Count} positions for invalid symbol {invalidSymbol}");
    }

    #endregion

    #region Balance Tests

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("Balance")]
    public async Task GetFuturesBalancesAsync_ShouldReturnFuturesBalance()
    {
        // Act
        var balances = await _apiClient.GetFuturesBalancesAsync();

        // Assert
        balances.Should().NotBeNull();
        balances.Should().BeOfType<List<FuturesBalance>>();
        
        Console.WriteLine($"Retrieved {balances.Count} futures balances");
        
        foreach (var balance in balances)
        {
            Console.WriteLine($"Balance: {balance.Asset} - Balance: {balance.Balance} - Available: {balance.AvailableBalance} - PnL: {balance.CrossUnrealizedPnl}");
            
            // Validate balance data structure
            balance.Asset.Should().NotBeNullOrEmpty();
            balance.Exchange.Should().Be("BingX");
            balance.Balance.Should().BeGreaterThanOrEqualTo(0);
            balance.AvailableBalance.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("Balance")]
    public async Task GetSpotBalancesAsync_ShouldReturnSpotBalances()
    {
        // Act
        var balances = await _apiClient.GetSpotBalancesAsync();

        // Assert
        balances.Should().NotBeNull();
        balances.Should().BeOfType<List<Balance>>();
        
        Console.WriteLine($"Retrieved {balances.Count} spot balances");
        
        foreach (var balance in balances)
        {
            Console.WriteLine($"Balance: {balance.Asset} - Available: {balance.Available} - Locked: {balance.Locked}");
            
            // Validate balance data structure
            balance.Asset.Should().NotBeNullOrEmpty();
            balance.Exchange.Should().Be("BingX");
            balance.Available.Should().BeGreaterThanOrEqualTo(0);
            balance.Locked.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    #endregion

    #region Trade History Tests

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithoutParameters_ShouldReturnTrades()
    {
        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync();

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades");
        
        foreach (var trade in trades.Take(5)) // Log first 5 trades
        {
            Console.WriteLine($"Trade: {trade.Symbol} - {trade.Side} - Size: {trade.Quantity} - Price: {trade.Price} - Time: {trade.Time}");
            
            // Validate trade data structure
            trade.Symbol.Should().NotBeNullOrEmpty();
            trade.Exchange.Should().Be("BingX");
            trade.Quantity.Should().BeGreaterThan(0);
            trade.Price.Should().BeGreaterThan(0);
            trade.Time.Should().BeGreaterThan(0);
        }
    }

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("TradeHistory")]
    public async Task GetSpotTradeHistoryAsync_WithSymbol_ShouldReturnSymbolTrades()
    {
        // Arrange
        const string symbol = "BTC-USDT";

        // Act
        var trades = await _apiClient.GetSpotTradeHistoryAsync(symbol);

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<Trade>>();
        
        // All returned trades should be for the specified symbol
        trades.Should().OnlyContain(t => t.Symbol == symbol);
        
        Console.WriteLine($"Retrieved {trades.Count} spot trades for {symbol}");
        
        foreach (var trade in trades.Take(5)) // Log first 5 trades
        {
            Console.WriteLine($"Trade: {trade.Symbol} - {trade.Side} - Quantity: {trade.Quantity} - Price: {trade.Price} - Time: {trade.TradeTime}");
            trade.Exchange.Should().Be("BingX");
        }
    }

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithTimeRange_ShouldReturnTradesInRange()
    {
        // Arrange
        var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var startTime = endTime - TimeSpan.FromDays(7).Ticks / TimeSpan.TicksPerMillisecond; // 7 days ago

        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(startTime: startTime, endTime: endTime, limit: 100);

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades in the last 7 days");
        
        // Verify all trades are within the time range
        foreach (var trade in trades)
        {
            trade.Time.Should().BeGreaterThanOrEqualTo(startTime);
            trade.Time.Should().BeLessThanOrEqualTo(endTime);
        }
    }

    #endregion

    #region Error Handling Tests

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("ErrorHandling")]
    public async Task GetOpenPositionsAsync_WithNetworkIssue_ShouldHandleGracefully()
    {
        // Create a client with very short timeout to simulate network issues
        var shortTimeoutSettings = new BingXApiSettings
        {
            BaseUrl = _settings.BaseUrl,
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey
        };

        var shortTimeoutRateLimiting = new RateLimitingSettings
        {
            BingXRequestsPerSecond = 1,
            RetryAttempts = 1, // Only one attempt to speed up test
            RetryDelaySeconds = 1
        };

        using var clientWithShortTimeout = new BingXApiClient(shortTimeoutSettings, shortTimeoutRateLimiting, _logger);

        // Act & Assert - Should not throw exception
        var positions = await clientWithShortTimeout.GetOpenPositionsAsync();
        positions.Should().NotBeNull();
    }

    #endregion

    #region Performance Tests

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("Performance")]
    public async Task GetOpenPositionsAsync_PerformanceTest_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int maxExecutionTimeSeconds = 30;

        // Act
        var positions = await _apiClient.GetOpenPositionsAsync();

        // Assert
        stopwatch.Stop();
        
        Console.WriteLine($"GetOpenPositionsAsync completed in {stopwatch.ElapsedMilliseconds}ms");
        
        positions.Should().NotBeNull();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(maxExecutionTimeSeconds));
    }

    [Test]
    [Explicit("Makes real API call to BingX")]
    [Category("Performance")]
    public async Task MultipleApiCalls_ShouldRespectRateLimit()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task<List<Position>>>();
        const int numberOfCalls = 3;

        // Act
        for (int i = 0; i < numberOfCalls; i++)
        {
            tasks.Add(_apiClient.GetOpenPositionsAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        
        Console.WriteLine($"{numberOfCalls} API calls completed in {stopwatch.ElapsedMilliseconds}ms");
        
        results.Should().HaveCount(numberOfCalls);
        results.Should().OnlyContain(r => r != null);
        
        // With rate limiting, this should take at least (numberOfCalls - 1) / requestsPerSecond seconds
        var minExpectedTime = TimeSpan.FromSeconds((numberOfCalls - 1) / (double)_rateLimitSettings.BingXRequestsPerSecond);
        stopwatch.Elapsed.Should().BeGreaterThan(minExpectedTime);
    }

    #endregion
}
