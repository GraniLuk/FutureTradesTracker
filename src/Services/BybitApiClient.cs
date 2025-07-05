using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using CryptoPositionAnalysis.Models;
using CryptoPositionAnalysis.Services;
using CryptoPositionAnalysis.Utils;

namespace CryptoPositionAnalysis.Services;

public class BybitApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly BybitApiSettings _settings;
    private readonly RateLimitingSettings _rateLimitSettings;
    private readonly ILogger<BybitApiClient> _logger;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public BybitApiClient(BybitApiSettings settings, RateLimitingSettings rateLimitSettings, ILogger<BybitApiClient> logger)
    {
        _settings = settings;
        _rateLimitSettings = rateLimitSettings;
        _logger = logger;
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);

        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(_settings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoPositionAnalysis/1.0");
    }

    public async Task<List<Balance>> GetSpotBalancesAsync()
    {
        const string endpoint = "/v5/asset/transfer/query-account-coins-balance";
        _logger.LogApiCall("Bybit", endpoint);

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "accountType", "FUND" }
            };

            var response = await MakeAuthenticatedRequestAsync<BybitApiResponse<BybitBalanceData>>(endpoint, queryParams);

            if (response?.RetCode == 0 && response.Result?.Balance != null)
            {
                var balances = response.Result.Balance
                    .Where(b => decimal.Parse(b.WalletBalance) > 0 || decimal.Parse(b.Locked) > 0) // Filter out zero balances
                    .Select(b => new Balance
                    {
                        Asset = b.Coin,
                        Available = decimal.Parse(b.WalletBalance),
                        Locked = decimal.Parse(b.Locked),
                        Exchange = "Bybit"
                    }).ToList();

                _logger.LogApiSuccess("Bybit", endpoint, balances.Count);
                return balances;
            }

            _logger.LogWarning("Bybit API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.RetMsg);
            return new List<Balance>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("Bybit", endpoint, ex);
            return new List<Balance>();
        }
    }

    public async Task<List<FuturesBalance>> GetFuturesBalancesAsync()
    {
        const string endpoint = "/v5/asset/transfer/query-account-coins-balance";
        _logger.LogApiCall("Bybit", endpoint);

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "accountType", "UNIFIED" },
                { "coin", "USDT" }
            };

            var response = await MakeAuthenticatedRequestAsync<BybitApiResponse<BybitBalanceData>>(endpoint, queryParams);

            if (response?.RetCode == 0 && response.Result?.Balance != null)
            {
                var balances = response.Result.Balance.Select(b => new FuturesBalance
                {
                    Asset = b.Coin,
                    Balance = decimal.Parse(b.WalletBalance),
                    AvailableBalance = decimal.Parse(b.WalletBalance) - decimal.Parse(b.Locked),
                    Exchange = "Bybit"
                }).ToList();

                _logger.LogApiSuccess("Bybit", endpoint, balances.Count);
                return balances;
            }

            _logger.LogWarning("Bybit API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.RetMsg);
            return new List<FuturesBalance>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("Bybit", endpoint, ex);
            return new List<FuturesBalance>();
        }
    }

    public async Task<List<Trade>> GetSpotTradeHistoryAsync(string? symbol = null, long? startTime = null, long? endTime = null, int limit = 50)
    {
        const string endpoint = "/v5/order/history";
        _logger.LogApiCall("Bybit", endpoint);

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "category", "spot" },
                { "limit", Math.Min(limit, 50).ToString() }
            };

            if (!string.IsNullOrEmpty(symbol))
                queryParams["symbol"] = symbol;

            if (startTime.HasValue)
                queryParams["startTime"] = startTime.Value.ToString();

            if (endTime.HasValue)
                queryParams["endTime"] = endTime.Value.ToString();

            var response = await MakeAuthenticatedRequestAsync<BybitApiResponse<BybitOrderHistoryData>>(endpoint, queryParams);

            if (response?.RetCode == 0 && response.Result?.List != null)
            {
                var trades = response.Result.List
                    .Select(order => Trade.FromBybitOrder(order))
                    .ToList();

                _logger.LogApiSuccess("Bybit", endpoint, trades.Count);
                return trades;
            }

            _logger.LogWarning("Bybit API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.RetMsg);
            return new List<Trade>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("Bybit", endpoint, ex);
            return new List<Trade>();
        }
    }

    public async Task<List<FuturesTrade>> GetFuturesTradeHistoryAsync(string? symbol = null, long? startTime = null, long? endTime = null, int limit = 50)
    {
        const string endpoint = "/v5/order/history";
        _logger.LogApiCall("Bybit", endpoint);

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "category", "linear" },
                { "limit", Math.Min(limit, 50).ToString() }
            };

            if (!string.IsNullOrEmpty(symbol))
                queryParams["symbol"] = symbol;

            if (startTime.HasValue)
                queryParams["startTime"] = startTime.Value.ToString();

            if (endTime.HasValue)
                queryParams["endTime"] = endTime.Value.ToString();

            var response = await MakeAuthenticatedRequestAsync<BybitApiResponse<BybitOrderHistoryData>>(endpoint, queryParams);

            if (response?.RetCode == 0 && response.Result?.List != null)
            {
                var trades = response.Result.List
                    .Select(order => FuturesTrade.FromBybitOrder(order))
                    .ToList();

                _logger.LogApiSuccess("Bybit", endpoint, trades.Count);
                return trades;
            }

            _logger.LogWarning("Bybit API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.RetMsg);
            return new List<FuturesTrade>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("Bybit", endpoint, ex);
            return new List<FuturesTrade>();
        }
    }

    public async Task<List<Position>> GetPositionsAsync(string? symbol = null)
    {
        const string endpoint = "/v5/position/list";
        _logger.LogApiCall("Bybit", endpoint);

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "category", "linear" },
                {"settleCoin",  "USDT" }
            };

            if (!string.IsNullOrEmpty(symbol))
                queryParams["symbol"] = symbol;

            var response = await MakeAuthenticatedRequestAsync<BybitApiResponse<BybitPositionData>>(endpoint, queryParams);

            if (response?.RetCode == 0 && response.Result?.List != null)
            {
                var positions = response.Result.List.Where(p => decimal.Parse(p.Size) != 0).Select(p => new Position
                {
                    Symbol = p.Symbol,
                    PositionSide = ParsePositionSide(p.Side),
                    PositionSize = decimal.Parse(p.Size),
                    EntryPrice = decimal.Parse(p.AvgPrice),
                    MarkPrice = decimal.Parse(p.MarkPrice),
                    UnrealizedPnl = decimal.Parse(p.UnrealisedPnl),
                    Leverage = decimal.Parse(p.Leverage),
                    UpdateTime = long.Parse(p.UpdatedTime),
                    Exchange = "Bybit"
                }).ToList();

                _logger.LogApiSuccess("Bybit", endpoint, positions.Count);
                return positions;
            }

            _logger.LogWarning("Bybit API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.RetMsg);
            return new List<Position>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("Bybit", endpoint, ex);
            return new List<Position>();
        }
    }

    private async Task<T?> MakeAuthenticatedRequestAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        await ApplyRateLimitingAsync();

        var timestamp = SignatureGenerator.GetUnixTimestamp().ToString();
        var recvWindow = "5000";

        var queryString = "";
        if (queryParams != null && queryParams.Count > 0)
        {
            queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        var signature = SignatureGenerator.GenerateBybitSignature(timestamp, _settings.ApiKey, recvWindow, queryString, _settings.SecretKey);

        var requestUri = endpoint;
        if (!string.IsNullOrEmpty(queryString))
        {
            requestUri += "?" + queryString;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-BAPI-API-KEY", _settings.ApiKey);
        request.Headers.Add("X-BAPI-SIGN", signature);
        request.Headers.Add("X-BAPI-SIGN-TYPE", "2");
        request.Headers.Add("X-BAPI-TIMESTAMP", timestamp);
        request.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);

        for (int attempt = 1; attempt <= _rateLimitSettings.RetryAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? _rateLimitSettings.RetryDelaySeconds;
                    _logger.LogWarning("Rate limited by Bybit API, waiting {RetryAfter} seconds", retryAfter);
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                    continue;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Bybit API request failed with status {StatusCode}: {Content}", response.StatusCode, errorContent);

                if (attempt < _rateLimitSettings.RetryAttempts)
                {
                    _logger.LogRetry("Bybit", endpoint, attempt, _rateLimitSettings.RetryAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(_rateLimitSettings.RetryDelaySeconds * attempt));
                }
            }
            catch (HttpRequestException ex) when (attempt < _rateLimitSettings.RetryAttempts)
            {
                _logger.LogRetry("Bybit", endpoint, attempt, _rateLimitSettings.RetryAttempts);
                _logger.LogError(ex, "HTTP request exception on attempt {Attempt}", attempt);
                await Task.Delay(TimeSpan.FromSeconds(_rateLimitSettings.RetryDelaySeconds * attempt));
            }
        }

        return default;
    }

    private async Task ApplyRateLimitingAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minInterval = TimeSpan.FromSeconds(1.0 / _rateLimitSettings.BybitRequestsPerSecond);

            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                _logger.LogRateLimit("Bybit", (int)delay.TotalMilliseconds);
                await Task.Delay(delay);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private static PositionSide ParsePositionSide(string positionSide)
    {
        return positionSide?.ToUpperInvariant() switch
        {
            "Buy" => PositionSide.Long,
            "Sell" => PositionSide.Short,
            _ => throw new ArgumentException($"Unknown position side: {positionSide}")
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _rateLimitSemaphore?.Dispose();
    }

    public static List<Position> CreatePositionsFromFuturesTrades(IEnumerable<FuturesTrade> futuresTrades)
    {
        var positions = new List<Position>();
        
        // Group trades by symbol
        var tradesBySymbol = futuresTrades.GroupBy(t => t.Symbol);
        
        foreach (var symbolGroup in tradesBySymbol)
        {
            var symbol = symbolGroup.Key;
            var trades = symbolGroup.OrderBy(t => t.Time).ToList();
            
            // Calculate position from trades
            decimal totalQuantity = 0;
            decimal totalValue = 0;
            decimal totalFees = 0;
            string? positionSide = null;
            
            foreach (var trade in trades)
            {
                var qty = trade.ExecutedQuantity;
                var value = trade.CumulativeQuoteQuantity;
                
                if (trade.Side == "Buy")
                {
                    totalQuantity += qty;
                    totalValue += value;
                }
                else // Sell
                {
                    totalQuantity -= qty;
                    totalValue -= value;
                }
                
                totalFees += trade.Fee;
                positionSide = trade.Side;
            }
            
            // Only add position if there's a non-zero quantity
            if (Math.Abs(totalQuantity) > 0.00001m)
            {
                var avgPrice = totalValue / totalQuantity;
                
                positions.Add(new Position
                {
                    Symbol = symbol,
                    PositionSide = totalQuantity > 0 ? PositionSide.Long : PositionSide.Short,
                    PositionSize = Math.Abs(totalQuantity),
                    EntryPrice = Math.Abs(avgPrice),
                    MarkPrice = Math.Abs(avgPrice), // Use entry price as mark price since we don't have real-time data
                    UnrealizedPnl = 0, // Cannot calculate without current mark price
                    Leverage = 1, // Default leverage
                    UpdateTime = trades.Max(t => t.UpdateTime),
                    Exchange = "Bybit"
                });
            }
        }
        
        return positions;
    }
}

// Bybit API response models
public class BybitApiResponse<T>
{
    public int RetCode { get; set; }
    public string RetMsg { get; set; } = string.Empty;
    public T? Result { get; set; }
    public long Time { get; set; }
}

public class BybitBalanceData
{
    public List<BybitBalance>? Balance { get; set; }
}

public class BybitBalance
{
    public string Coin { get; set; } = string.Empty;
    public string WalletBalance { get; set; } = "0";
    public string Locked { get; set; } = "0";
}

public class BybitWalletData
{
    public List<BybitWalletAccount>? List { get; set; }
}

public class BybitWalletAccount
{
    public string AccountType { get; set; } = string.Empty;
    public List<BybitWalletCoin>? Coin { get; set; }
}

public class BybitWalletCoin
{
    public string CoinName { get; set; } = string.Empty;
    public string WalletBalance { get; set; } = "0";
    public string AvailableToWithdraw { get; set; } = "0";
}

public class BybitOrderHistoryData
{
    public List<BybitOrder>? List { get; set; }
}

public class BybitOrder
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;
    
    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty;
    
    [JsonPropertyName("qty")]
    public string Qty { get; set; } = "0";
    
    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";
    
    [JsonPropertyName("avgPrice")]
    public string AvgPrice { get; set; } = "0";
    
    [JsonPropertyName("cumExecQty")]
    public string CumExecQty { get; set; } = "0";
    
    [JsonPropertyName("cumExecValue")]
    public string CumExecValue { get; set; } = "0";
    
    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;
    
    [JsonPropertyName("timeInForce")]
    public string TimeInForce { get; set; } = string.Empty;
    
    [JsonPropertyName("createdTime")]
    public string CreatedTime { get; set; } = "0";
    
    [JsonPropertyName("updatedTime")]
    public string UpdatedTime { get; set; } = "0";
    
    [JsonPropertyName("stopPrice")]
    public string StopPrice { get; set; } = "0";
    
    [JsonPropertyName("takeProfitPrice")]
    public string TakeProfitPrice { get; set; } = "0";
    
    [JsonPropertyName("stopLossPrice")]
    public string StopLossPrice { get; set; } = "0";
    
    [JsonPropertyName("positionIdx")]
    public int PositionIdx { get; set; } = 0;
    
    [JsonPropertyName("reduceOnly")]
    public bool ReduceOnly { get; set; } = false;
    
    [JsonPropertyName("closeOnTrigger")]
    public bool CloseOnTrigger { get; set; } = false;
    
    [JsonPropertyName("cumExecFee")]
    public string CumExecFee { get; set; } = "0";
    
    [JsonPropertyName("rejectReason")]
    public string RejectReason { get; set; } = string.Empty;
    
    [JsonPropertyName("triggerPrice")]
    public string TriggerPrice { get; set; } = "0";
    
    [JsonPropertyName("takeProfit")]
    public string TakeProfit { get; set; } = "0";
    
    [JsonPropertyName("stopLoss")]
    public string StopLoss { get; set; } = "0";
    
    [JsonPropertyName("triggerBy")]
    public string TriggerBy { get; set; } = string.Empty;
    
    [JsonPropertyName("tpTriggerBy")]
    public string TpTriggerBy { get; set; } = string.Empty;
    
    [JsonPropertyName("slTriggerBy")]
    public string SlTriggerBy { get; set; } = string.Empty;
    
    [JsonPropertyName("triggerDirection")]
    public int TriggerDirection { get; set; } = 0;
    
    [JsonPropertyName("smpGroup")]
    public int SmpGroup { get; set; } = 0;
    
    [JsonPropertyName("stopOrderType")]
    public string StopOrderType { get; set; } = string.Empty;
}

public class BybitPositionData
{
    public List<BybitPosition>? List { get; set; }
}

public class BybitPosition
{
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Size { get; set; } = "0";
    public string AvgPrice { get; set; } = "0";
    public string MarkPrice { get; set; } = "0";
    public string UnrealisedPnl { get; set; } = "0";
    public string Leverage { get; set; } = "1";
    public string UpdatedTime { get; set; } = "0";
}
