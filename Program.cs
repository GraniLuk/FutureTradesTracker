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

            // Get futures trade history (last 30 days)
            var bingxFuturesTrades = await client.GetFuturesTradeHistoryAsync(startTime: thirtyDaysAgo);
            futuresTrades.AddRange(bingxFuturesTrades);

            // Get current positions
            var bingxPositions = await client.GetPositionsAsync();
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
