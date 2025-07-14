using Microsoft.Extensions.Logging;
using FutureTradesTracker.Services;
using FutureTradesTracker.Utils;
using FutureTradesTracker.Models;

namespace FutureTradesTracker;

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
            logger.LogInformation("=== Future Trades Tracker Started ===");
            logger.LogInformation("Timestamp: {Timestamp}", DateTime.UtcNow);

            // Load configuration
            var configService = new ConfigurationService();
            var bingxSettings = configService.GetBingXApiSettings();
            var bybitSettings = configService.GetBybitApiSettings();
            var excelSettings = configService.GetExcelSettings();
            var rateLimitSettings = configService.GetRateLimitingSettings();

            // Initialize portfolio processing service
            var portfolioService = new PortfolioProcessingService(loggerFactory.CreateLogger<PortfolioProcessingService>());

            // Add exchange processors
            portfolioService.AddExchangeProcessor(new BingXProcessor(bingxSettings, rateLimitSettings, loggerFactory));
            portfolioService.AddExchangeProcessor(new BybitProcessor(bybitSettings, rateLimitSettings, loggerFactory));

            // Process all exchanges
            var portfolioData = await portfolioService.ProcessAllExchangesAsync();

            // Export to Excel if we have data
            if (portfolioData.HasAnyData)
            {
                logger.LogInformation("Exporting data to Excel...");
                var excelService = new ExcelExportService(excelSettings, loggerFactory.CreateLogger<ExcelExportService>());
                var filePath = await excelService.ExportPortfolioDataAsync(
                    portfolioData.SpotBalances,
                    portfolioData.FuturesBalances,
                    portfolioData.SpotTrades,
                    portfolioData.FuturesTrades,
                    portfolioData.Positions);

                logger.LogInformation("Excel export completed successfully!");
                logger.LogInformation("File saved: {FilePath}", filePath);
                
                // Print summary
                PrintSummary(logger, portfolioData);
            }
            else
            {
                logger.LogWarning("No data retrieved from any configured exchanges. Please check your API credentials and network connectivity.");
            }

            logger.LogInformation("=== Future Trades Tracker Completed ===");
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

    private static void PrintSummary(ILogger logger, PortfolioData portfolioData)
    {
        logger.LogInformation("\n=== PORTFOLIO SUMMARY ===");
        logger.LogInformation("Spot Balances: {Count} assets", portfolioData.SpotBalances.Count(b => b.Total > 0));
        logger.LogInformation("Futures Balances: {Count} assets", portfolioData.FuturesBalances.Count(b => b.Balance > 0));
        logger.LogInformation("Spot Trades (30 days): {Count} orders", portfolioData.SpotTrades.Count);
        logger.LogInformation("Futures Trades (30 days): {Count} orders", portfolioData.FuturesTrades.Count);
        
        // Break down by exchange
        var exchangeBreakdown = portfolioData.FuturesTrades.GroupBy(t => t.Exchange).ToDictionary(g => g.Key, g => g.Count());
        foreach (var exchange in exchangeBreakdown)
        {
            logger.LogInformation("  {Exchange}: {Count} futures trades", exchange.Key, exchange.Value);
        }
        logger.LogInformation("Active Positions: {Count} positions", portfolioData.Positions.Count);

        if (portfolioData.Positions.Count > 0)
        {
            var totalPnl = portfolioData.Positions.Sum(p => p.UnrealizedPnl);
            logger.LogInformation("Total Unrealized PnL: {PnL:F4}", totalPnl);
        }

        // Exchange breakdown
        var exchanges = portfolioData.SpotBalances.Select(b => b.Exchange)
            .Concat(portfolioData.FuturesBalances.Select(b => b.Exchange))
            .Concat(portfolioData.Positions.Select(p => p.Exchange))
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .ToList();

        logger.LogInformation("Exchanges processed: {Exchanges}", string.Join(", ", exchanges));
    }
}
