using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using FutureTradesTracker.Models;
using FutureTradesTracker.Services;
using FutureTradesTracker.Utils;

namespace FutureTradesTracker.Services;

public class BingXApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly BingXApiSettings _settings;
    private readonly RateLimitingSettings _rateLimitSettings;
    private readonly ILogger<BingXApiClient> _logger;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public BingXApiClient(BingXApiSettings settings, RateLimitingSettings rateLimitSettings, ILogger<BingXApiClient> logger)
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

        _httpClient.DefaultRequestHeaders.Add("X-BX-APIKEY", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("X-SOURCE-KEY", "FutureTradesTracker");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FutureTradesTracker/1.0");
    }

    public async Task<List<Balance>> GetSpotBalancesAsync()
    {
        const string endpoint = "/openApi/spot/v1/account/balance";
        _logger.LogApiCall("BingX", endpoint);

        try
        {
            var response = await MakeAuthenticatedRequestAsync<BingXApiResponse<BingXBalanceData>>(endpoint);
            
            if (response?.IsSuccess == true && response.Data?.Balances != null)
            {
                var balances = new List<Balance>();
                
                foreach (var bingxBalance in response.Data.Balances)
                {
                    if (decimal.TryParse(bingxBalance.Free, out var available) && 
                        decimal.TryParse(bingxBalance.Locked, out var locked))
                    {
                        // Only include balances with non-zero amounts
                        if (available > 0 || locked > 0)
                        {
                            balances.Add(new Balance
                            {
                                Asset = bingxBalance.Asset,
                                Available = available,
                                Locked = locked,
                                Exchange = "BingX"
                            });
                        }
                    }
                }
                
                _logger.LogApiSuccess("BingX", endpoint, balances.Count);
                return balances;
            }

            _logger.LogWarning("BingX API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.Message);
            return new List<Balance>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("BingX", endpoint, ex);
            return new List<Balance>();
        }
    }

    public async Task<List<FuturesBalance>> GetFuturesBalancesAsync()
    {
        const string endpoint = "/openApi/swap/v2/user/balance";
        _logger.LogApiCall("BingX", endpoint);

        try
        {
            var response = await MakeAuthenticatedRequestAsync<BingXApiResponse<BingXFuturesBalanceData>>(endpoint);
            
            if (response?.IsSuccess == true && response.Data?.Balance != null)
            {
                var balances = new List<FuturesBalance>();
                var balance = response.Data.Balance;
                
                if (decimal.TryParse(balance.Balance, out var balanceAmount) &&
                    decimal.TryParse(balance.AvailableMargin, out var availableAmount) &&
                    decimal.TryParse(balance.UnrealizedProfit, out var crossPnl) &&
                    decimal.TryParse(balance.Equity, out var maxWithdraw))
                {
                    balances.Add(new FuturesBalance
                    {
                        Asset = balance.Asset,
                        Balance = balanceAmount,
                        AvailableBalance = availableAmount,
                        CrossUnrealizedPnl = crossPnl,
                        MaxWithdrawAmount = maxWithdraw,
                        Exchange = "BingX"
                    });
                }
                
                _logger.LogApiSuccess("BingX", endpoint, balances.Count);
                return balances;
            }

            _logger.LogWarning("BingX API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.Message);
            return new List<FuturesBalance>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("BingX", endpoint, ex);
            return new List<FuturesBalance>();
        }
    }

    public async Task<List<Trade>> GetSpotTradeHistoryAsync(string? symbol = null, long? startTime = null, long? endTime = null, int limit = 500)
    {
        const string endpoint = "/openApi/spot/v1/trade/historyOrders";
        _logger.LogApiCall("BingX", endpoint);

        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(symbol))
                queryParams.Add($"symbol={symbol}");
            
            if (startTime.HasValue)
                queryParams.Add($"startTime={startTime.Value}");
            
            if (endTime.HasValue)
                queryParams.Add($"endTime={endTime.Value}");
            
            queryParams.Add($"limit={Math.Min(limit, 1000)}");

            var response = await MakeAuthenticatedRequestAsync<BingXApiResponse<BingXTradeHistoryData>>(endpoint, queryParams);
            
            if (response?.IsSuccess == true && response.Data?.Orders != null)
            {
                var trades = response.Data.Orders
                    .Select(order => Trade.FromBingXOrder(order))
                    .ToList();
                
                _logger.LogApiSuccess("BingX", endpoint, trades.Count);
                return trades;
            }

            _logger.LogWarning("BingX API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.Message);
            return new List<Trade>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("BingX", endpoint, ex);
            return new List<Trade>();
        }
    }

    public async Task<List<FuturesTrade>> GetFuturesTradeHistoryAsync(string? symbol = null, long? startTime = null, long? endTime = null, int limit = 500)
    {
        const string endpoint = "/openApi/swap/v2/trade/allOrders";
        _logger.LogApiCall("BingX", endpoint);

        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(symbol))
                queryParams.Add($"symbol={symbol}");
            
            if (startTime.HasValue)
                queryParams.Add($"startTime={startTime.Value}");
            
            if (endTime.HasValue)
                queryParams.Add($"endTime={endTime.Value}");
            
            queryParams.Add($"limit={Math.Min(limit, 1000)}");

            var response = await MakeAuthenticatedRequestAsync<BingXApiResponse<BingXFuturesTradeData>>(endpoint, queryParams);
            
            if (response?.IsSuccess == true && response.Data?.Orders != null)
            {
                var trades = response.Data.Orders
                    .Select(order => FuturesTrade.FromBingXFuturesOrder(order))
                    .ToList();
                
                _logger.LogApiSuccess("BingX", endpoint, trades.Count);
                return trades;
            }

            _logger.LogWarning("BingX API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.Message);
            return new List<FuturesTrade>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("BingX", endpoint, ex);
            return new List<FuturesTrade>();
        }
    }

    private async Task<T?> MakeAuthenticatedRequestAsync<T>(string endpoint, List<string>? queryParams = null)
    {
        await ApplyRateLimitingAsync();

        var timestamp = SignatureGenerator.GetUnixTimestamp();
        var parameters = queryParams ?? new List<string>();
        parameters.Add($"timestamp={timestamp}");

        var queryString = string.Join("&", parameters);
        var signature = SignatureGenerator.GenerateBingXSignature(queryString, _settings.SecretKey);
        var fullQueryString = $"{queryString}&signature={signature}";

        var requestUri = $"{endpoint}?{fullQueryString}";

        for (int attempt = 1; attempt <= _rateLimitSettings.RetryAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(requestUri);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("BingX API Response: {Content}", content);
                    
                    try
                    {
                        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize BingX API response. Content: {Content}", content);
                        throw;
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? _rateLimitSettings.RetryDelaySeconds;
                    _logger.LogWarning("Rate limited by BingX API, waiting {RetryAfter} seconds", retryAfter);
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                    continue;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("BingX API request failed with status {StatusCode}: {Content}", response.StatusCode, errorContent);

                if (attempt < _rateLimitSettings.RetryAttempts)
                {
                    _logger.LogRetry("BingX", endpoint, attempt, _rateLimitSettings.RetryAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(_rateLimitSettings.RetryDelaySeconds * attempt));
                }
            }
            catch (HttpRequestException ex) when (attempt < _rateLimitSettings.RetryAttempts)
            {
                _logger.LogRetry("BingX", endpoint, attempt, _rateLimitSettings.RetryAttempts);
                _logger.LogError(ex, "HTTP request exception on attempt {Attempt}", attempt);
                await Task.Delay(TimeSpan.FromSeconds(_rateLimitSettings.RetryDelaySeconds * attempt));
            }
        }

        return default;
    }

    public async Task<List<Position>> GetOpenPositionsAsync(string? symbol = null)
    {
        const string endpoint = "/openApi/swap/v2/user/positions";
        _logger.LogApiCall("BingX", endpoint);

        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(symbol))
                queryParams.Add($"symbol={symbol}");

            var response = await MakeAuthenticatedRequestAsync<BingXPositionsResponse>(endpoint, queryParams);
            
            if (response?.Code == 0 && response.Data != null)
            {
                var positions = new List<Position>();
                
                foreach (var bingxPosition in response.Data)
                {
                    // Only include positions with non-zero amounts
                    if (decimal.TryParse(bingxPosition.PositionAmt, out var positionSize) && 
                        Math.Abs(positionSize) > 0.000001m)
                    {
                        positions.Add(new Position
                        {
                            Symbol = bingxPosition.Symbol,
                            PositionSide = ParsePositionSide(bingxPosition.PositionSide),
                            PositionSize = positionSize,
                            EntryPrice = decimal.TryParse(bingxPosition.AvgPrice, out var avgPrice) ? avgPrice : 0m,
                            MarkPrice = decimal.TryParse(bingxPosition.MarkPrice, out var markPrice) ? markPrice : 0m,
                            UnrealizedPnl = decimal.TryParse(bingxPosition.UnrealizedProfit, out var unrealizedPnl) ? unrealizedPnl : 0m,
                            Leverage = bingxPosition.Leverage,
                            IsolatedMargin = decimal.TryParse(bingxPosition.Margin, out var margin) ? margin : 0m,
                            Isolated = bingxPosition.Isolated,
                            UpdateTime = bingxPosition.UpdateTime,
                            Exchange = "BingX"
                        });
                    }
                }
                
                _logger.LogApiSuccess("BingX", endpoint, positions.Count);
                return positions;
            }

            _logger.LogWarning("BingX API returned unsuccessful response for {Endpoint}: {Message}", endpoint, response?.Msg);
            return new List<Position>();
        }
        catch (Exception ex)
        {
            _logger.LogApiError("BingX", endpoint, ex);
            return new List<Position>();
        }
    }

    private async Task ApplyRateLimitingAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minInterval = TimeSpan.FromSeconds(1.0 / _rateLimitSettings.BingXRequestsPerSecond);

            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                _logger.LogRateLimit("BingX", (int)delay.TotalMilliseconds);
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
            "LONG" => PositionSide.Long,
            "SHORT" => PositionSide.Short,
            _ => throw new ArgumentException($"Unknown position side: {positionSide}")
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _rateLimitSemaphore?.Dispose();
    }
}
