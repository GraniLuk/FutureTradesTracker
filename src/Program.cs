using Microsoft.Extensions.Logging;
using CryptoPositionAnalysis.Services;
using CryptoPositionAnalysis.Utils;
using CryptoPositionAnalysis.Models;

namespace CryptoPositionAnalysis;

class Program
{
    private static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        Utils.LoggerFactory.Initialize(loggerFactory);
        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            logger.LogInformation("=== Crypto Position Analysis Started ===");
            logger.LogInformation("Timestamp: {Timestamp}", DateTime.UtcNow);

            // Load configuration
            var configService = new ConfigurationService();
            var bingxSettings = configService.GetBingXApiSettings();
            var bybitSettings = configService.GetBybitApiSettings();
            var excelSettings = configService.GetExcelSettings();
            var rateLimitSettings = configService.GetRateLimitingSettings();

            // Validate configuration
            if (string.IsNullOrEmpty(bingxSettings.ApiKey) || bingxSettings.ApiKey == "your-bingx-api-key")
            {
                logger.LogWarning("BingX API credentials not configured. Please update appsettings.json with your API credentials.");
            }

            if (string.IsNullOrEmpty(bybitSettings.ApiKey) || bybitSettings.ApiKey == "your-bybit-api-key")
            {
                logger.LogWarning("Bybit API credentials not configured. Please update appsettings.json with your API credentials.");
            }

            // Initialize data collections
            var allSpotBalances = new List<Balance>();
            var allFuturesBalances = new List<FuturesBalance>();
            var allSpotTrades = new List<Trade>();
            var allFuturesTrades = new List<FuturesTrade>();
            var allPositions = new List<Position>();

            // Process BingX data if configured
            if (!string.IsNullOrEmpty(bingxSettings.ApiKey) && bingxSettings.ApiKey != "your-bingx-api-key")
            {
                logger.LogInformation("Processing BingX exchange data...");
                await ProcessBingXData(
                    bingxSettings, 
                    rateLimitSettings, 
                    loggerFactory,
                    allSpotBalances,
                    allFuturesBalances,
                    allSpotTrades,
                    allFuturesTrades,
                    allPositions);
            }

            // Process Bybit data if configured
            if (!string.IsNullOrEmpty(bybitSettings.ApiKey) && bybitSettings.ApiKey != "your-bybit-api-key")
            {
                logger.LogInformation("Processing Bybit exchange data...");
                await ProcessBybitData(
                    bybitSettings, 
                    rateLimitSettings, 
                    loggerFactory,
                    allSpotBalances,
                    allFuturesBalances,
                    allSpotTrades,
                    allFuturesTrades,
                    allPositions);
            }

            // Export to Excel
            if (allSpotBalances.Count > 0 || allFuturesBalances.Count > 0 || 
                allSpotTrades.Count > 0 || allFuturesTrades.Count > 0 || allPositions.Count > 0)
            {
                logger.LogInformation("Exporting data to Excel...");
                var excelService = new ExcelExportService(excelSettings, loggerFactory.CreateLogger<ExcelExportService>());
                var filePath = await excelService.ExportPortfolioDataAsync(
                    allSpotBalances,
                    allFuturesBalances,
                    allSpotTrades,
                    allFuturesTrades,
                    allPositions);

                logger.LogInformation("Excel export completed successfully!");
                logger.LogInformation("File saved: {FilePath}", filePath);
                
                // Print summary
                PrintSummary(logger, allSpotBalances, allFuturesBalances, allSpotTrades, allFuturesTrades, allPositions);
            }
            else
            {
                logger.LogWarning("No data retrieved from any configured exchanges. Please check your API credentials and network connectivity.");
            }

            logger.LogInformation("=== Crypto Position Analysis Completed ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during execution");
            Environment.Exit(1);
        }

        // Keep console open if running in debug mode
        if (System.Diagnostics.Debugger.IsAttached)
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    private static async Task ProcessBingXData(
        BingXApiSettings settings,
        RateLimitingSettings rateLimitSettings,
        ILoggerFactory loggerFactory,
        List<Balance> spotBalances,
        List<FuturesBalance> futuresBalances,
        List<Trade> spotTrades,
        List<FuturesTrade> futuresTrades,
        List<Position> positions)
    {
        using var client = new BingXApiClient(settings, rateLimitSettings, loggerFactory.CreateLogger<BingXApiClient>());

        try
        {
            // Get spot balances
            var bingxSpotBalances = await client.GetSpotBalancesAsync();
            spotBalances.AddRange(bingxSpotBalances);

            // Get futures balances
            var bingxFuturesBalances = await client.GetFuturesBalancesAsync();
            futuresBalances.AddRange(bingxFuturesBalances);

            // Get spot trade history (last 30 days)
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();
            var bingxSpotTrades = await client.GetSpotTradeHistoryAsync(startTime: thirtyDaysAgo);
            spotTrades.AddRange(bingxSpotTrades);

            // Get futures trade history (last 30 days, in 6-day chunks due to API limit)
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Fetching futures trade history in 6-day chunks...");
            
            for (int i = 0; i < 5; i++) // 5 chunks of 6 days = 30 days
            {
                var chunkEndTime = DateTimeOffset.UtcNow.AddDays(-i * 6).ToUnixTimeMilliseconds();
                var chunkStartTime = DateTimeOffset.UtcNow.AddDays(-(i + 1) * 6).ToUnixTimeMilliseconds();
                
                try
                {
                    var chunkTrades = await client.GetFuturesTradeHistoryAsync(startTime: chunkStartTime, endTime: chunkEndTime);
                    futuresTrades.AddRange(chunkTrades);
                    logger.LogInformation("Fetched {Count} futures trades for period {StartDate} to {EndDate}", 
                        chunkTrades.Count, 
                        DateTimeOffset.FromUnixTimeMilliseconds(chunkStartTime).ToString("yyyy-MM-dd"),
                        DateTimeOffset.FromUnixTimeMilliseconds(chunkEndTime).ToString("yyyy-MM-dd"));
                    
                    // Small delay between requests to respect rate limits
                    await Task.Delay(1000);
                }
                catch (Exception chunkEx)
                {
                    logger.LogWarning("Failed to fetch futures trades for chunk {ChunkIndex}: {Error}", i, chunkEx.Message);
                }
            }

            // Get open futures positions directly from the API
            var bingxPositions = await client.GetOpenPositionsAsync();
            logger.LogInformation("Fetched {PositionCount} open positions from BingX", bingxPositions.Count);
            positions.AddRange(bingxPositions);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "Error processing BingX data");
        }
    }

    private static async Task ProcessBybitData(
        BybitApiSettings settings,
        RateLimitingSettings rateLimitSettings,
        ILoggerFactory loggerFactory,
        List<Balance> spotBalances,
        List<FuturesBalance> futuresBalances,
        List<Trade> spotTrades,
        List<FuturesTrade> futuresTrades,
        List<Position> positions)
    {
        using var client = new BybitApiClient(settings, rateLimitSettings, loggerFactory.CreateLogger<BybitApiClient>());

        try
        {
            // Get spot balances
            var bybitSpotBalances = await client.GetSpotBalancesAsync();
            spotBalances.AddRange(bybitSpotBalances);

            // Get futures balances
            var bybitFuturesBalances = await client.GetFuturesBalancesAsync();
            futuresBalances.AddRange(bybitFuturesBalances);

            // Get spot trade history (last 30 days)
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();
            var bybitSpotTrades = await client.GetSpotTradeHistoryAsync(startTime: thirtyDaysAgo);
            spotTrades.AddRange(bybitSpotTrades);

            // Get futures trade history (last 30 days, in 6-day chunks due to API limit)
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Fetching Bybit futures trade history in 6-day chunks...");
            
            var bybitFuturesTradeCount = 0;
            for (int i = 0; i < 5; i++) // 5 chunks of 6 days = 30 days
            {
                var chunkEndTime = DateTimeOffset.UtcNow.AddDays(-i * 6).ToUnixTimeMilliseconds();
                var chunkStartTime = DateTimeOffset.UtcNow.AddDays(-(i + 1) * 6).ToUnixTimeMilliseconds();
                
                try
                {
                    var chunkTrades = await client.GetFuturesTradeHistoryAsync(startTime: chunkStartTime, endTime: chunkEndTime);
                    futuresTrades.AddRange(chunkTrades);
                    bybitFuturesTradeCount += chunkTrades.Count;
                    logger.LogInformation("Fetched {Count} futures trades for period {StartDate} to {EndDate}", 
                        chunkTrades.Count, 
                        DateTimeOffset.FromUnixTimeMilliseconds(chunkStartTime).ToString("yyyy-MM-dd"),
                        DateTimeOffset.FromUnixTimeMilliseconds(chunkEndTime).ToString("yyyy-MM-dd"));
                    
                    // Small delay between requests to respect rate limits
                    await Task.Delay(1000);
                }
                catch (Exception chunkEx)
                {
                    logger.LogWarning("Failed to fetch Bybit futures trades for chunk {ChunkIndex}: {Error}", i, chunkEx.Message);
                }
            }
            
            logger.LogInformation("Total Bybit futures trades fetched: {Count}", bybitFuturesTradeCount);

            // Get current positions
            var bybitPositions = await client.GetPositionsAsync();
            positions.AddRange(bybitPositions);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "Error processing Bybit data");
        }
    }

    private static void PrintSummary(
        ILogger logger,
        List<Balance> spotBalances,
        List<FuturesBalance> futuresBalances,
        List<Trade> spotTrades,
        List<FuturesTrade> futuresTrades,
        List<Position> positions)
    {
        logger.LogInformation("\n=== PORTFOLIO SUMMARY ===");
        logger.LogInformation("Spot Balances: {Count} assets", spotBalances.Count(b => b.Total > 0));
        logger.LogInformation("Futures Balances: {Count} assets", futuresBalances.Count(b => b.Balance > 0));
        logger.LogInformation("Spot Trades (30 days): {Count} orders", spotTrades.Count);
        logger.LogInformation("Futures Trades (30 days): {Count} orders", futuresTrades.Count);
        
        // Break down by exchange
        var exchangeBreakdown = futuresTrades.GroupBy(t => t.Exchange).ToDictionary(g => g.Key, g => g.Count());
        foreach (var exchange in exchangeBreakdown)
        {
            logger.LogInformation("  {Exchange}: {Count} futures trades", exchange.Key, exchange.Value);
        }
        logger.LogInformation("Active Positions: {Count} positions", positions.Count);

        if (positions.Count > 0)
        {
            var totalPnl = positions.Sum(p => p.UnrealizedPnl);
            logger.LogInformation("Total Unrealized PnL: {PnL:F4}", totalPnl);
        }

        // Exchange breakdown
        var exchanges = spotBalances.Select(b => b.Exchange)
            .Concat(futuresBalances.Select(b => b.Exchange))
            .Concat(positions.Select(p => p.Exchange))
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .ToList();

        logger.LogInformation("Exchanges processed: {Exchanges}", string.Join(", ", exchanges));
    }
}
