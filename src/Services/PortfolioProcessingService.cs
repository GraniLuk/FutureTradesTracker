using FutureTradesTracker.Models;
using Microsoft.Extensions.Logging;

namespace FutureTradesTracker.Services;

public class PortfolioProcessingService
{
    private readonly ILogger<PortfolioProcessingService> _logger;
    private readonly List<IExchangeProcessor> _exchangeProcessors;

    public PortfolioProcessingService(ILogger<PortfolioProcessingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exchangeProcessors = new List<IExchangeProcessor>();
    }

    public void AddExchangeProcessor(IExchangeProcessor processor)
    {
        _exchangeProcessors.Add(processor);
    }

    public async Task<PortfolioData> ProcessAllExchangesAsync()
    {
        var portfolioData = new PortfolioData();
        var processedExchanges = new List<string>();

        foreach (var processor in _exchangeProcessors)
        {
            try
            {
                _logger.LogInformation("Processing {ExchangeName} data...", processor.ExchangeName);
                var result = await processor.ProcessExchangeDataAsync();

                if (result.IsSuccess)
                {
                    portfolioData.SpotBalances.AddRange(result.SpotBalances);
                    portfolioData.FuturesBalances.AddRange(result.FuturesBalances);
                    portfolioData.SpotTrades.AddRange(result.SpotTrades);
                    portfolioData.FuturesTrades.AddRange(result.FuturesTrades);
                    portfolioData.Positions.AddRange(result.Positions);
                    processedExchanges.Add(processor.ExchangeName);
                    _logger.LogInformation("{ExchangeName} processing completed successfully", processor.ExchangeName);
                }
                else
                {
                    _logger.LogWarning("{ExchangeName} processing failed: {Error}", processor.ExchangeName, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing {ExchangeName}", processor.ExchangeName);
            }
        }

        _logger.LogInformation("Processed data from {Count} exchanges: {Exchanges}", 
            processedExchanges.Count, string.Join(", ", processedExchanges));

        return portfolioData;
    }
}

public class PortfolioData
{
    public List<Balance> SpotBalances { get; set; } = new();
    public List<FuturesBalance> FuturesBalances { get; set; } = new();
    public List<Trade> SpotTrades { get; set; } = new();
    public List<FuturesTrade> FuturesTrades { get; set; } = new();
    public List<Position> Positions { get; set; } = new();

    public bool HasAnyData => 
        SpotBalances.Count > 0 || FuturesBalances.Count > 0 || 
        SpotTrades.Count > 0 || FuturesTrades.Count > 0 || Positions.Count > 0;
}
