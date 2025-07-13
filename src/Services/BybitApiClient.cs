using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using FutureTradesTracker.Models;
using FutureTradesTracker.Services;
using FutureTradesTracker.Utils;

namespace FutureTradesTracker.Services;

public class BybitApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly BybitApiSettings _settings;
    private readonly RateLimitingSettings _rateLimitSettings;
    private readonly ILogger<BybitApiClient> _logger;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private long _clockSkewMs = 0; // Clock skew compensation in milliseconds
    private bool _clockSkewDetected = false;

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

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FutureTradesTracker/1.0");
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
        _logger.LogInformation("Fetching futures order history and closed PnL data as separate records");
        
        // Fetch order history and closed PnL in parallel for better performance
        var orderHistoryTask = GetFuturesOrderHistoryAsync(symbol, startTime, endTime, limit);
        var closedPnlTask = GetClosedPnlAsync(symbol, startTime, endTime, limit);
        
        await Task.WhenAll(orderHistoryTask, closedPnlTask);
        
        var orders = await orderHistoryTask;
        var closedPnlRecords = await closedPnlTask;
        
        var allTrades = new List<FuturesTrade>();
        
        // Add all order history records as trades
        var orderTrades = orders.Select(order => FuturesTrade.FromBybitOrder(order)).ToList();
        allTrades.AddRange(orderTrades);
        
        // Add all closed PnL records as separate trades with realized PnL
        var pnlTrades = closedPnlRecords.Select(pnl => FuturesTrade.FromBybitClosedPnl(pnl)).ToList();
        allTrades.AddRange(pnlTrades);
        
        _logger.LogInformation("Retrieved {OrderCount} order records and {PnlCount} closed PnL records, total {TotalTrades} trade records", 
            orders.Count, closedPnlRecords.Count, allTrades.Count);
        
        return allTrades;
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
                    CumRealisedPnl = decimal.Parse(p.CumRealisedPnl),
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

    public async Task<List<FuturesTrade>> GetFuturesExecutionHistoryAsync(string? symbol = null, long? startTime = null, long? endTime = null, int limit = 50)
    {
        const string endpoint = "/v5/execution/list";
        _logger.LogApiCall("Bybit", endpoint);

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "category", "linear" },
                { "limit", Math.Min(limit, 100).ToString() } // Execution endpoint supports up to 100
            };

            if (!string.IsNullOrEmpty(symbol))
                queryParams["symbol"] = symbol;

            if (startTime.HasValue)
                queryParams["startTime"] = startTime.Value.ToString();

            if (endTime.HasValue)
                queryParams["endTime"] = endTime.Value.ToString();

            var response = await MakeAuthenticatedRequestAsync<BybitApiResponse<BybitExecutionData>>(endpoint, queryParams);

            if (response?.RetCode == 0 && response.Result?.List != null)
            {
                var trades = response.Result.List
                    .Select(execution => FuturesTrade.FromBybitExecution(execution))
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

    public async Task<List<BybitOrder>> GetFuturesOrderHistoryAsync(string? symbol = null, long? startTime = null, long? endTime = null, int limit = 50)
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
                _logger.LogApiSuccess("Bybit", endpoint, response.Result.List.Count);
                return response.Result.List;
            }

            _logger.LogWarning("Bybit API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.RetMsg);
            return new List<BybitOrder>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("Bybit", endpoint, ex);
            return new List<BybitOrder>();
        }
    }

    public async Task<List<BybitClosedPnl>> GetClosedPnlAsync(string? symbol = null, long? startTime = null, long? endTime = null, int limit = 50)
    {
        const string endpoint = "/v5/position/closed-pnl";
        _logger.LogApiCall("Bybit", endpoint);

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "category", "linear" },
                { "limit", Math.Min(limit, 100).ToString() }
            };

            if (!string.IsNullOrEmpty(symbol))
                queryParams["symbol"] = symbol;

            if (startTime.HasValue)
                queryParams["startTime"] = startTime.Value.ToString();

            if (endTime.HasValue)
                queryParams["endTime"] = endTime.Value.ToString();

            var response = await MakeAuthenticatedRequestAsync<BybitApiResponse<BybitClosedPnlData>>(endpoint, queryParams);

            if (response?.RetCode == 0 && response.Result?.List != null)
            {
                _logger.LogApiSuccess("Bybit", endpoint, response.Result.List.Count);
                return response.Result.List;
            }

            _logger.LogWarning("Bybit API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.RetMsg);
            return new List<BybitClosedPnl>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("Bybit", endpoint, ex);
            return new List<BybitClosedPnl>();
        }
    }

    private async Task<T?> MakeAuthenticatedRequestAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null) where T : class
    {
        await ApplyRateLimitingAsync();

        var queryString = "";
        if (queryParams != null && queryParams.Count > 0)
        {
            queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        var requestUri = endpoint;
        if (!string.IsNullOrEmpty(queryString))
        {
            requestUri += "?" + queryString;
        }

        for (int attempt = 1; attempt <= _rateLimitSettings.RetryAttempts; attempt++)
        {
            try
            {
                // Generate timestamp with clock skew compensation
                var baseTimestamp = SignatureGenerator.GetUnixTimestamp();
                var timestamp = (baseTimestamp + _clockSkewMs).ToString();
                var recvWindow = "10000"; // Increased receive window to handle clock skew and network latency
                
                if (_clockSkewMs != 0)
                {
                    _logger.LogDebug("Applying clock skew compensation: {ClockSkewMs}ms (Base: {BaseTimestamp}, Adjusted: {AdjustedTimestamp})", 
                        _clockSkewMs, baseTimestamp, timestamp);
                }
                
                var signature = SignatureGenerator.GenerateBybitSignature(timestamp, _settings.ApiKey, recvWindow, queryString, _settings.SecretKey);

                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Add("X-BAPI-API-KEY", _settings.ApiKey);
                request.Headers.Add("X-BAPI-SIGN", signature);
                request.Headers.Add("X-BAPI-SIGN-TYPE", "2");
                request.Headers.Add("X-BAPI-TIMESTAMP", timestamp);
                request.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Check for timestamp errors in the JSON response before deserializing
                    if ((content.Contains("timestamp") || content.Contains("recv_window")) && 
                        content.Contains("retCode") && !content.Contains("\"retCode\":0") && !_clockSkewDetected)
                    {
                        var skewDetected = TryExtractClockSkew(content);
                        if (skewDetected && attempt == 1)
                        {
                            _logger.LogInformation("Clock skew detected and compensated: {ClockSkewMs}ms. Retrying request...", _clockSkewMs);
                            _clockSkewDetected = true;
                            continue; // Retry immediately with compensation
                        }
                    }

                    var deserializedResponse = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return deserializedResponse;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? _rateLimitSettings.RetryDelaySeconds;
                    _logger.LogWarning("Rate limited by Bybit API, waiting {RetryAfter} seconds", retryAfter);
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                    continue;
                }

                _logger.LogError("Bybit API request failed with status {StatusCode}: {Content}", response.StatusCode, content);

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

    /// <summary>
    /// Attempts to extract clock skew from Bybit error message and set compensation
    /// </summary>
    /// <param name="errorContent">The error response content</param>
    /// <returns>True if clock skew was detected and compensation was set</returns>
    private bool TryExtractClockSkew(string errorContent)
    {
        try
        {
            // Parse error message format: req_timestamp[1752404596342],server_timestamp[1752404594907]
            var reqMatch = System.Text.RegularExpressions.Regex.Match(errorContent, @"req_timestamp\[(\d+)\]");
            var serverMatch = System.Text.RegularExpressions.Regex.Match(errorContent, @"server_timestamp\[(\d+)\]");

            if (reqMatch.Success && serverMatch.Success)
            {
                var reqTimestamp = long.Parse(reqMatch.Groups[1].Value);
                var serverTimestamp = long.Parse(serverMatch.Groups[1].Value);
                
                // Calculate clock skew (negative means our clock is fast)
                var skew = serverTimestamp - reqTimestamp;
                
                _logger.LogInformation("Clock skew detected - Req: {ReqTimestamp}, Server: {ServerTimestamp}, Skew: {SkewMs}ms", 
                    reqTimestamp, serverTimestamp, skew);
                
                // Only apply compensation if skew is significant (>100ms) and reasonable (<30 seconds)
                if (Math.Abs(skew) > 100 && Math.Abs(skew) < 30000)
                {
                    _clockSkewMs = skew;
                    _logger.LogInformation("Applied clock skew compensation: {ClockSkewMs}ms", _clockSkewMs);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Clock skew {SkewMs}ms is outside acceptable range (100ms to 30000ms)", skew);
                }
            }
            else
            {
                _logger.LogDebug("Failed to match timestamp regex in error content: {ErrorContent}", errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse clock skew from error message: {ErrorContent}", errorContent);
        }

        return false;
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
            "BUY" => PositionSide.Long,
            "SELL" => PositionSide.Short,
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
    public string CumRealisedPnl { get; set; } = "0";
    public string Leverage { get; set; } = "1";
    public string UpdatedTime { get; set; } = "0";
}

public class BybitExecutionData
{
    public List<BybitExecution>? List { get; set; }
}

public class BybitExecution
{
    [JsonPropertyName("execId")]
    public string ExecId { get; set; } = string.Empty;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;
    
    [JsonPropertyName("orderLinkId")]
    public string OrderLinkId { get; set; } = string.Empty;
    
    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;
    
    [JsonPropertyName("execQty")]
    public string ExecQty { get; set; } = "0";
    
    [JsonPropertyName("execPrice")]
    public string ExecPrice { get; set; } = "0";
    
    [JsonPropertyName("execValue")]
    public string ExecValue { get; set; } = "0";
    
    [JsonPropertyName("execTime")]
    public string ExecTime { get; set; } = "0";
    
    [JsonPropertyName("execFee")]
    public string ExecFee { get; set; } = "0";
    
    [JsonPropertyName("feeRate")]
    public string FeeRate { get; set; } = "0";
    
    [JsonPropertyName("execType")]
    public string ExecType { get; set; } = string.Empty;
    
    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty;
    
    [JsonPropertyName("closedSize")]
    public string ClosedSize { get; set; } = "0";
    
    [JsonPropertyName("markPrice")]
    public string MarkPrice { get; set; } = "0";
    
    // This might contain per-execution realized PnL
    [JsonPropertyName("closedPnl")]
    public string ClosedPnl { get; set; } = "0";
}

// Bybit Closed PnL models
public class BybitClosedPnlData
{
    public List<BybitClosedPnl>? List { get; set; }
    public string NextPageCursor { get; set; } = string.Empty;
}

public class BybitClosedPnl
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;
    
    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;
    
    [JsonPropertyName("qty")]
    public string Qty { get; set; } = "0";
    
    [JsonPropertyName("orderPrice")]
    public string OrderPrice { get; set; } = "0";
    
    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty;
    
    [JsonPropertyName("execType")]
    public string ExecType { get; set; } = string.Empty;
    
    [JsonPropertyName("closedSize")]
    public string ClosedSize { get; set; } = "0";
    
    [JsonPropertyName("cumEntryValue")]
    public string CumEntryValue { get; set; } = "0";
    
    [JsonPropertyName("avgEntryPrice")]
    public string AvgEntryPrice { get; set; } = "0";
    
    [JsonPropertyName("cumExitValue")]
    public string CumExitValue { get; set; } = "0";
    
    [JsonPropertyName("avgExitPrice")]
    public string AvgExitPrice { get; set; } = "0";
    
    [JsonPropertyName("closedPnl")]
    public string ClosedPnl { get; set; } = "0";
    
    [JsonPropertyName("fillCount")]
    public string FillCount { get; set; } = "0";
    
    [JsonPropertyName("leverage")]
    public string Leverage { get; set; } = "1";
    
    [JsonPropertyName("openFee")]
    public string OpenFee { get; set; } = "0";
    
    [JsonPropertyName("closeFee")]
    public string CloseFee { get; set; } = "0";
    
    [JsonPropertyName("createdTime")]
    public string CreatedTime { get; set; } = "0";
    
    [JsonPropertyName("updatedTime")]
    public string UpdatedTime { get; set; } = "0";
}
