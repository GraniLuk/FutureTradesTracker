using System.Text.Json.Serialization;

namespace CryptoPositionAnalysis.Models;

public class Trade
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("tradeId")]
    public long TradeId { get; set; }

    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string OrderType { get; set; } = string.Empty;

    [JsonPropertyName("origQty")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("executedQty")]
    public decimal ExecutedQuantity { get; set; }

    [JsonPropertyName("cummulativeQuoteQty")]
    public decimal CumulativeQuoteQuantity { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timeInForce")]
    public string TimeInForce { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long TradeTime { get; set; }

    [JsonPropertyName("updateTime")]
    public long UpdateTime { get; set; }

    [JsonPropertyName("commission")]
    public decimal Fee { get; set; }

    [JsonPropertyName("commissionAsset")]
    public string FeeAsset { get; set; } = string.Empty;

    public DateTime TradeDateTime => DateTimeOffset.FromUnixTimeMilliseconds(TradeTime).DateTime;

    public string Exchange { get; set; } = string.Empty;
}

public class FuturesTrade
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;

    [JsonPropertyName("positionSide")]
    public string? PositionSide { get; set; }

    [JsonPropertyName("type")]
    public string OrderType { get; set; } = string.Empty;

    [JsonPropertyName("origQty")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("avgPrice")]
    public decimal AvgPrice { get; set; }

    [JsonPropertyName("executedQty")]
    public decimal ExecutedQuantity { get; set; }

    [JsonPropertyName("cumQuote")]
    public decimal CumulativeQuoteQuantity { get; set; }

    [JsonPropertyName("stopPrice")]
    public decimal? StopPrice { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timeInForce")]
    public string TimeInForce { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("updateTime")]
    public long UpdateTime { get; set; }

    [JsonPropertyName("commission")]
    public decimal Fee { get; set; }

    [JsonPropertyName("commissionAsset")]
    public string FeeAsset { get; set; } = string.Empty;

    [JsonPropertyName("realizedPnl")]
    public decimal RealizedPnl { get; set; }

    [JsonPropertyName("leverage")]
    public string? Leverage { get; set; }

    [JsonPropertyName("reduceOnly")]
    public bool? ReduceOnly { get; set; }

    [JsonPropertyName("workingType")]
    public string? WorkingType { get; set; }

    [JsonPropertyName("clientOrderId")]
    public string? ClientOrderId { get; set; }

    public DateTime TradeDateTime => DateTimeOffset.FromUnixTimeMilliseconds(Time).DateTime;

    public string Exchange { get; set; } = string.Empty;
}
