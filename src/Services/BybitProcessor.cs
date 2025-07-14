using FutureTradesTracker.Models;
using Microsoft.Extensions.Logging;

namespace FutureTradesTracker.Services;

public class BybitProcessor : IExchangeProcessor
{
    private readonly BybitApiSettings _settings;
    private readonly RateLimitingSettings _rateLimitSettings;
    private readonly ILogger<BybitProcessor> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public string ExchangeName => "Bybit";

    public BybitProcessor(
        BybitApiSettings settings,
        RateLimitingSettings rateLimitSettings,
        ILoggerFactory loggerFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _rateLimitSettings = rateLimitSettings ?? throw new ArgumentNullException(nameof(rateLimitSettings));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<BybitProcessor>();
    }

    public async Task<ExchangeProcessingResult> ProcessExchangeDataAsync()
    {
        var result = new ExchangeProcessingResult();

        if (string.IsNullOrEmpty(_settings.ApiKey) || _settings.ApiKey == "your-bybit-api-key")
        {
            _logger.LogWarning("Bybit API credentials not configured. Skipping Bybit processing.");
            result.ErrorMessage = "API credentials not configured";
            return result;
        }

        _logger.LogInformation("Processing Bybit exchange data...");

        using var client = new BybitApiClient(_settings, _rateLimitSettings, _loggerFactory.CreateLogger<BybitApiClient>());

        try
        {
            // Get spot balances
            var spotBalances = await client.GetSpotBalancesAsync();
            result.SpotBalances.AddRange(spotBalances);
            _logger.LogInformation("Fetched {Count} spot balances from Bybit", spotBalances.Count);

            // Get futures balances
            var futuresBalances = await client.GetFuturesBalancesAsync();
            result.FuturesBalances.AddRange(futuresBalances);
            _logger.LogInformation("Fetched {Count} futures balances from Bybit", futuresBalances.Count);

            // Get spot trade history (last 30 days)
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();
            var spotTrades = await client.GetSpotTradeHistoryAsync(startTime: thirtyDaysAgo);
            result.SpotTrades.AddRange(spotTrades);
            _logger.LogInformation("Fetched {Count} spot trades from Bybit", spotTrades.Count);

            // Get futures trade history (last 30 days, in 6-day chunks due to API limit)
            await ProcessFuturesTradeHistory(client, result);

            // Get current positions
            var positions = await client.GetPositionsAsync();
            result.Positions.AddRange(positions);
            _logger.LogInformation("Fetched {Count} positions from Bybit", positions.Count);

            result.IsSuccess = true;
            _logger.LogInformation("Bybit data processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Bybit data");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task ProcessFuturesTradeHistory(BybitApiClient client, ExchangeProcessingResult result)
    {
        _logger.LogInformation("Fetching Bybit futures trade history in 6-day chunks...");

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
                _logger.LogWarning("Failed to fetch Bybit futures trades for chunk {ChunkIndex}: {Error}", i, chunkEx.Message);
            }
        }

        _logger.LogInformation("Total Bybit futures trades fetched: {Count}", result.FuturesTrades.Count);
    }
}
