using FutureTradesTracker.Models;
using Microsoft.Extensions.Logging;

namespace FutureTradesTracker.Services;

public class BingXProcessor : IExchangeProcessor
{
    private readonly BingXApiSettings _settings;
    private readonly RateLimitingSettings _rateLimitSettings;
    private readonly ILogger<BingXProcessor> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public string ExchangeName => "BingX";

    public BingXProcessor(
        BingXApiSettings settings,
        RateLimitingSettings rateLimitSettings,
        ILoggerFactory loggerFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _rateLimitSettings = rateLimitSettings ?? throw new ArgumentNullException(nameof(rateLimitSettings));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<BingXProcessor>();
    }

    public async Task<ExchangeProcessingResult> ProcessExchangeDataAsync()
    {
        var result = new ExchangeProcessingResult();

        if (string.IsNullOrEmpty(_settings.ApiKey) || _settings.ApiKey == "your-bingx-api-key")
        {
            _logger.LogWarning("BingX API credentials not configured. Skipping BingX processing.");
            result.ErrorMessage = "API credentials not configured";
            return result;
        }

        _logger.LogInformation("Processing BingX exchange data...");

        using var client = new BingXApiClient(_settings, _rateLimitSettings, _loggerFactory.CreateLogger<BingXApiClient>());

        try
        {
            // Get spot balances
            var spotBalances = await client.GetSpotBalancesAsync();
            result.SpotBalances.AddRange(spotBalances);
            _logger.LogInformation("Fetched {Count} spot balances from BingX", spotBalances.Count);

            // Get futures balances
            var futuresBalances = await client.GetFuturesBalancesAsync();
            result.FuturesBalances.AddRange(futuresBalances);
            _logger.LogInformation("Fetched {Count} futures balances from BingX", futuresBalances.Count);

            // Get spot trade history (last 30 days)
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();
            var spotTrades = await client.GetSpotTradeHistoryAsync(startTime: thirtyDaysAgo);
            result.SpotTrades.AddRange(spotTrades);
            _logger.LogInformation("Fetched {Count} spot trades from BingX", spotTrades.Count);

            // Get futures trade history (last 30 days, in 6-day chunks due to API limit)
            await ProcessFuturesTradeHistory(client, result);

            // Get open futures positions
            var positions = await client.GetOpenPositionsAsync();
            result.Positions.AddRange(positions);
            _logger.LogInformation("Fetched {Count} open positions from BingX", positions.Count);

            result.IsSuccess = true;
            _logger.LogInformation("BingX data processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BingX data");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task ProcessFuturesTradeHistory(BingXApiClient client, ExchangeProcessingResult result)
    {
        _logger.LogInformation("Fetching BingX futures trade history in 6-day chunks...");

        for (int i = 0; i < 1; i++) // 1 chunk of 6 days for now
        {
            var chunkEndTime = DateTimeOffset.UtcNow.AddDays(-i * 6).ToUnixTimeMilliseconds();
            var chunkStartTime = DateTimeOffset.UtcNow.AddDays(-(i + 1) * 6).ToUnixTimeMilliseconds();

            try
            {
                var chunkTrades = await client.GetFuturesTradeHistoryAsync(startTime: chunkStartTime, endTime: chunkEndTime);
                result.FuturesTrades.AddRange(chunkTrades);
                _logger.LogInformation("Fetched {Count} futures trades for period {StartDate} to {EndDate}",
                    chunkTrades.Count,
                    DateTimeOffset.FromUnixTimeMilliseconds(chunkStartTime).ToString("yyyy-MM-dd"),
                    DateTimeOffset.FromUnixTimeMilliseconds(chunkEndTime).ToString("yyyy-MM-dd"));

                // Small delay between requests to respect rate limits
                await Task.Delay(1000);
            }
            catch (Exception chunkEx)
            {
                _logger.LogWarning("Failed to fetch BingX futures trades for chunk {ChunkIndex}: {Error}", i, chunkEx.Message);
            }
        }

        _logger.LogInformation("Total BingX futures trades fetched: {Count}", result.FuturesTrades.Count);
    }
}
