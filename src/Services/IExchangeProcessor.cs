using FutureTradesTracker.Models;
using Microsoft.Extensions.Logging;

namespace FutureTradesTracker.Services;

public interface IExchangeProcessor
{
    string ExchangeName { get; }
    Task<ExchangeProcessingResult> ProcessExchangeDataAsync();
}

public class ExchangeProcessingResult
{
    public List<Balance> SpotBalances { get; set; } = new();
    public List<FuturesBalance> FuturesBalances { get; set; } = new();
    public List<Trade> SpotTrades { get; set; } = new();
    public List<FuturesTrade> FuturesTrades { get; set; } = new();
    public List<Position> Positions { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
