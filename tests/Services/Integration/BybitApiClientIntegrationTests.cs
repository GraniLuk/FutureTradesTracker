using NUnit.Framework;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using FutureTradesTracker.Models;
using FutureTradesTracker.Services;
using Microsoft.Extensions.Configuration;

namespace FutureTradesTracker.Tests.Services.Integration;

/// <summary>
/// Integration tests for Bybit API endpoints using real API calls.
/// These tests are marked as [Explicit] to prevent automatic execution in CI/CD.
/// 
/// To run these tests:
/// 1. Set up your API credentials in user secrets or appsettings.json
/// 2. Run: dotnet test --filter "TestCategory=Integration.Bybit" --logger console
/// 3. Or run individual tests in your IDE
/// 
/// Prerequisites:
/// - Valid Bybit API credentials
/// - Active Bybit account with appropriate permissions
/// - Stable internet connection
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("Bybit")]
[Explicit("These tests make real API calls and require valid credentials")]
public class BybitApiClientIntegrationTests
{
    private BybitApiClient _apiClient;
    private ILogger<BybitApiClient> _logger;
    private BybitApiSettings _settings;
    private RateLimitingSettings _rateLimitSettings;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Set up configuration to read from appsettings.json and user secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<BybitApiClientIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole()
                   .AddDebug()
                   .SetMinimumLevel(LogLevel.Information);
        });
        _logger = loggerFactory.CreateLogger<BybitApiClient>();

        // Get settings from configuration
        _settings = configuration.GetSection("BybitApi").Get<BybitApiSettings>() ?? new BybitApiSettings();
        _rateLimitSettings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        // Validate credentials are available
        if (string.IsNullOrEmpty(_settings.ApiKey) || string.IsNullOrEmpty(_settings.SecretKey))
        {
            Assert.Fail("Bybit API credentials are not configured. Please set up your API key and secret key in user secrets or appsettings.json");
        }

        // Create API client
        _apiClient = new BybitApiClient(_settings, _rateLimitSettings, _logger);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _apiClient?.Dispose();
    }

    #region Positions Tests

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("Positions")]
    public async Task GetOpenPositionsAsync_WithoutSymbol_ShouldReturnAllPositions()
    {
        // Act
        var positions = await _apiClient.GetPositionsAsync();

        // Assert
        positions.Should().NotBeNull();
        positions.Should().BeOfType<List<Position>>();
        
        Console.WriteLine($"Retrieved {positions.Count} positions from Bybit");
        
        foreach (var position in positions)
        {
            Console.WriteLine($"Position: {position.Symbol} - {position.PositionSide} - Size: {position.PositionSize} - PnL: {position.UnrealizedPnl}");
            
            // Validate position data structure
            position.Symbol.Should().NotBeNullOrEmpty();
            position.Exchange.Should().Be("Bybit");
            position.PositionSide.Should().BeOneOf(PositionSide.Long, PositionSide.Short);
            position.UpdateTime.Should().BeGreaterThan(0);
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("Positions")]
    public async Task GetOpenPositionsAsync_WithSpecificSymbol_ShouldReturnSymbolPositions()
    {
        // Arrange
        const string symbol = "BTCUSDT";

        // Act
        var positions = await _apiClient.GetPositionsAsync(symbol);

        // Assert
        positions.Should().NotBeNull();
        positions.Should().BeOfType<List<Position>>();
        
        // All returned positions should be for the specified symbol
        positions.Should().OnlyContain(p => p.Symbol == symbol);
        
        Console.WriteLine($"Retrieved {positions.Count} positions for {symbol} from Bybit");
        
        foreach (var position in positions)
        {
            Console.WriteLine($"Position: {position.Symbol} - {position.PositionSide} - Size: {position.PositionSize} - PnL: {position.UnrealizedPnl}");
            position.Exchange.Should().Be("Bybit");
        }
    }

    #endregion

    #region Balance Tests

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("Balance")]
    public async Task GetFuturesBalancesAsync_ShouldReturnFuturesBalance()
    {
        // Act
        var balances = await _apiClient.GetFuturesBalancesAsync();

        // Assert
        balances.Should().NotBeNull();
        balances.Should().BeOfType<List<FuturesBalance>>();
        
        Console.WriteLine($"Retrieved {balances.Count} futures balances from Bybit");
        
        foreach (var balance in balances)
        {
            Console.WriteLine($"Balance: {balance.Asset} - Balance: {balance.Balance} - Available: {balance.AvailableBalance} - PnL: {balance.CrossUnrealizedPnl}");
            
            // Validate balance data structure
            balance.Asset.Should().NotBeNullOrEmpty();
            balance.Exchange.Should().Be("Bybit");
            balance.Balance.Should().BeGreaterThanOrEqualTo(0);
            balance.AvailableBalance.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("Balance")]
    public async Task GetSpotBalancesAsync_ShouldReturnSpotBalances()
    {
        // Act
        var balances = await _apiClient.GetSpotBalancesAsync();

        // Assert
        balances.Should().NotBeNull();
        balances.Should().BeOfType<List<Balance>>();
        
        Console.WriteLine($"Retrieved {balances.Count} spot balances from Bybit");
        
        foreach (var balance in balances)
        {
            Console.WriteLine($"Balance: {balance.Asset} - Available: {balance.Available} - Locked: {balance.Locked}");
            
            // Validate balance data structure
            balance.Asset.Should().NotBeNullOrEmpty();
            balance.Exchange.Should().Be("Bybit");
            balance.Available.Should().BeGreaterThanOrEqualTo(0);
            balance.Locked.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    #endregion

    #region Futures Trade History Tests

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithoutParameters_ShouldReturnRecentTrades()
    {
        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync();

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades from Bybit");
        
        foreach (var trade in trades.Take(5)) // Show first 5 trades
        {
            Console.WriteLine($"Trade: {trade.Symbol} - {trade.Side} - Qty: {trade.ExecutedQuantity} - Price: {trade.Price} - Time: {DateTimeOffset.FromUnixTimeMilliseconds(trade.Time):yyyy-MM-dd HH:mm:ss}");
            
            // Validate trade data structure
            trade.Symbol.Should().NotBeNullOrEmpty();
            trade.Exchange.Should().Be("Bybit");
            trade.Side.Should().BeOneOf("Buy", "Sell");
            trade.ExecutedQuantity.Should().BeGreaterThan(0);
            trade.Price.Should().BeGreaterThan(0);
            trade.Time.Should().BeGreaterThan(0);
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithSpecificSymbol_ShouldReturnSymbolTrades()
    {
        // Arrange
        const string symbol = "VIRTUALUSDT";

        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(symbol);

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        
        // All returned trades should be for the specified symbol
        trades.Should().OnlyContain(t => t.Symbol == symbol);
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades for {symbol} from Bybit");
        
        foreach (var trade in trades.Take(3)) // Show first 3 trades
        {
            Console.WriteLine($"Trade: {trade.Symbol} - {trade.Side} - Qty: {trade.ExecutedQuantity} - Price: {trade.Price} - Time: {DateTimeOffset.FromUnixTimeMilliseconds(trade.Time):yyyy-MM-dd HH:mm:ss}");
            trade.Exchange.Should().Be("Bybit");
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithTimeRange_ShouldReturnTradesInRange()
    {
        // Arrange
        var endTime = DateTimeOffset.UtcNow;
        var startTime = endTime.AddDays(-30); // Last 30 days
        
        var startTimeMs = startTime.ToUnixTimeMilliseconds();
        var endTimeMs = endTime.ToUnixTimeMilliseconds();

        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(
            symbol: null, 
            startTime: startTimeMs, 
            endTime: endTimeMs
        );

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades in time range {startTime:yyyy-MM-dd} to {endTime:yyyy-MM-dd}");
        
        // All trades should be within the specified time range
        foreach (var trade in trades)
        {
            trade.Time.Should().BeGreaterThanOrEqualTo(startTimeMs);
            trade.Time.Should().BeLessThanOrEqualTo(endTimeMs);
            trade.Exchange.Should().Be("Bybit");
        }
        
        if (trades.Any())
        {
            var earliestTrade = trades.OrderBy(t => t.Time).First();
            var latestTrade = trades.OrderByDescending(t => t.Time).First();
            
            Console.WriteLine($"Earliest trade: {DateTimeOffset.FromUnixTimeMilliseconds(earliestTrade.Time):yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Latest trade: {DateTimeOffset.FromUnixTimeMilliseconds(latestTrade.Time):yyyy-MM-dd HH:mm:ss}");
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithLimitParameter_ShouldRespectLimit()
    {
        // Arrange
        const int limit = 10;

        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(limit: limit);

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        trades.Count.Should().BeLessOrEqualTo(limit);
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades with limit {limit} from Bybit");
        
        foreach (var trade in trades)
        {
            Console.WriteLine($"Trade: {trade.Symbol} - {trade.Side} - Qty: {trade.ExecutedQuantity} - Price: {trade.Price}");
            trade.Exchange.Should().Be("Bybit");
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithAllParameters_ShouldReturnFilteredTrades()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const int limit = 5;
        var endTime = DateTimeOffset.UtcNow;
        var startTime = endTime.AddDays(-7); // Last 7 days
        
        var startTimeMs = startTime.ToUnixTimeMilliseconds();
        var endTimeMs = endTime.ToUnixTimeMilliseconds();

        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(
            symbol: symbol,
            startTime: startTimeMs,
            endTime: endTimeMs,
            limit: limit
        );

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        trades.Count.Should().BeLessOrEqualTo(limit);
        
        // All trades should match the filters
        trades.Should().OnlyContain(t => t.Symbol == symbol);
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades for {symbol} in last 7 days with limit {limit}");
        
        foreach (var trade in trades)
        {
            Console.WriteLine($"Trade: {trade.Symbol} - {trade.Side} - Qty: {trade.ExecutedQuantity} - Price: {trade.Price} - Time: {DateTimeOffset.FromUnixTimeMilliseconds(trade.Time):yyyy-MM-dd HH:mm:ss}");
            
            // Validate all constraints
            trade.Symbol.Should().Be(symbol);
            trade.Time.Should().BeGreaterThanOrEqualTo(startTimeMs);
            trade.Time.Should().BeLessThanOrEqualTo(endTimeMs);
            trade.Exchange.Should().Be("Bybit");
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithMaxLimit_ShouldRespectApiLimit()
    {
        // Arrange - Test that the API respects the maximum limit of 50
        const int requestedLimit = 100; // Request more than API allows
        const int expectedMaxLimit = 50; // API should cap at 50

        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(limit: requestedLimit);

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        trades.Count.Should().BeLessOrEqualTo(expectedMaxLimit);
        
        Console.WriteLine($"Requested {requestedLimit} futures trades, received {trades.Count} (max API limit: {expectedMaxLimit})");
        
        foreach (var trade in trades.Take(3))
        {
            Console.WriteLine($"Trade: {trade.Symbol} - {trade.Side} - Qty: {trade.ExecutedQuantity} - Price: {trade.Price}");
            trade.Exchange.Should().Be("Bybit");
        }
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_WithInvalidSymbol_ShouldReturnEmptyList()
    {
        // Arrange
        const string invalidSymbol = "INVALID_SYMBOL_123";

        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(invalidSymbol);

        // Assert
        trades.Should().NotBeNull();
        trades.Should().BeOfType<List<FuturesTrade>>();
        trades.Should().BeEmpty();
        
        Console.WriteLine($"Retrieved {trades.Count} futures trades for invalid symbol '{invalidSymbol}' - expected empty result");
    }

    [Test]
    [Explicit("Makes real API call to Bybit")]
    [Category("TradeHistory")]
    public async Task GetFuturesTradeHistoryAsync_ValidateTradeProperties_ShouldHaveCorrectFormat()
    {
        // Act
        var trades = await _apiClient.GetFuturesTradeHistoryAsync(limit: 1);

        // Assert
        trades.Should().NotBeNull();
        
        if (trades.Any())
        {
            var trade = trades.First();
            
            Console.WriteLine($"Validating trade properties for: {trade.Symbol}");
            Console.WriteLine($"Order ID: {trade.OrderId}");
            Console.WriteLine($"Side: {trade.Side}");
            Console.WriteLine($"Executed Quantity: {trade.ExecutedQuantity}");
            Console.WriteLine($"Price: {trade.Price}");
            Console.WriteLine($"Cumulative Quote Quantity: {trade.CumulativeQuoteQuantity}");
            Console.WriteLine($"Fee: {trade.Fee}");
            Console.WriteLine($"Fee Asset: {trade.FeeAsset}");
            Console.WriteLine($"Time: {DateTimeOffset.FromUnixTimeMilliseconds(trade.Time):yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Update Time: {DateTimeOffset.FromUnixTimeMilliseconds(trade.UpdateTime):yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Exchange: {trade.Exchange}");
            
            // Validate required properties
            trade.OrderId.Should().BeGreaterThan(0);
            trade.Symbol.Should().NotBeNullOrEmpty();
            trade.Side.Should().BeOneOf("Buy", "Sell");
            trade.ExecutedQuantity.Should().BeGreaterThan(0);
            trade.Price.Should().BeGreaterThan(0);
            trade.CumulativeQuoteQuantity.Should().BeGreaterThan(0);
            trade.Fee.Should().BeGreaterThanOrEqualTo(0);
            trade.FeeAsset.Should().NotBeNullOrEmpty();
            trade.Time.Should().BeGreaterThan(0);
            trade.UpdateTime.Should().BeGreaterThan(0);
            trade.Exchange.Should().Be("Bybit");
        }
        else
        {
            Console.WriteLine("No trades found to validate properties");
        }
    }

    #endregion

    #region Cross-Exchange Comparison Tests

    [Test]
    [Explicit("Makes real API calls to both exchanges")]
    [Category("CrossExchange")]
    public async Task ComparePositionsAcrossExchanges_ShouldShowDifferences()
    {
        // This test demonstrates comparing data across exchanges
        // Arrange
        var bingXSettings = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<BybitApiClientIntegrationTests>(optional: true)
            .Build()
            .GetSection("BingXApi")
            .Get<BingXApiSettings>();

        if (bingXSettings == null || string.IsNullOrEmpty(bingXSettings.ApiKey))
        {
            Assert.Ignore("BingX credentials not configured - skipping cross-exchange comparison");
            return;
        }

        using var bingXClient = new BingXApiClient(bingXSettings, _rateLimitSettings, 
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<BingXApiClient>());

        // Act
        var bingXPositions = await bingXClient.GetOpenPositionsAsync();
        var bybitPositions = await _apiClient.GetPositionsAsync();

        // Assert & Analysis
        Console.WriteLine($"BingX Positions: {bingXPositions.Count}");
        Console.WriteLine($"Bybit Positions: {bybitPositions.Count}");

        var bingXSymbols = bingXPositions.Select(p => p.Symbol).ToHashSet();
        var bybitSymbols = bybitPositions.Select(p => p.Symbol).ToHashSet();

        var commonSymbols = bingXSymbols.Intersect(bybitSymbols).ToList();
        Console.WriteLine($"Common symbols: {string.Join(", ", commonSymbols)}");

        // Compare positions for common symbols
        foreach (var symbol in commonSymbols)
        {
            var bingXPos = bingXPositions.FirstOrDefault(p => p.Symbol == symbol);
            var bybitPos = bybitPositions.FirstOrDefault(p => p.Symbol == symbol);

            if (bingXPos != null && bybitPos != null)
            {
                Console.WriteLine($"Symbol {symbol}:");
                Console.WriteLine($"  BingX: {bingXPos.PositionSide} {bingXPos.PositionSize} @ {bingXPos.EntryPrice}");
                Console.WriteLine($"  Bybit: {bybitPos.PositionSide} {bybitPos.PositionSize} @ {bybitPos.EntryPrice}");
            }
        }

        // Both should return valid results
        bingXPositions.Should().NotBeNull();
        bybitPositions.Should().NotBeNull();
    }

    #endregion
}
