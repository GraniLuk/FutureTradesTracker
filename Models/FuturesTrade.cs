using System.Text.Json.Serialization;
using CryptoPositionAnalysis.Services;

namespace CryptoPositionAnalysis.Models;

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

    /// <summary>
    /// Creates a FuturesTrade instance from a Bybit order
    /// </summary>
    /// <param name="bybitOrder">The Bybit order</param>
    /// <returns>A FuturesTrade instance with mapped values from the Bybit order</returns>
    public static FuturesTrade FromBybitOrder(BybitOrder bybitOrder)
    {
        var trade = new FuturesTrade
        {
            Symbol = bybitOrder.Symbol,
            Side = bybitOrder.Side.ToUpper(),
            OrderType = bybitOrder.OrderType,
            Status = bybitOrder.OrderStatus,
            TimeInForce = bybitOrder.TimeInForce,
            ReduceOnly = bybitOrder.ReduceOnly,
            FeeAsset = "USDT", // Bybit typically uses USDT for futures fees
            Exchange = "Bybit"
        };

        // Parse required fields with error handling
        if (long.TryParse(bybitOrder.OrderId, out var orderId))
            trade.OrderId = orderId;

        if (decimal.TryParse(bybitOrder.Qty, out var quantity))
            trade.Quantity = quantity;

        if (decimal.TryParse(bybitOrder.Price, out var price))
            trade.Price = price;

        if (decimal.TryParse(bybitOrder.CumExecQty, out var executedQty))
            trade.ExecutedQuantity = executedQty;

        if (long.TryParse(bybitOrder.CreatedTime, out var createdTime))
            trade.Time = createdTime;

        if (long.TryParse(bybitOrder.UpdatedTime, out var updatedTime))
            trade.UpdateTime = updatedTime;

        // Parse optional fields
        if (decimal.TryParse(bybitOrder.AvgPrice, out var avgPrice))
            trade.AvgPrice = avgPrice > 0 ? avgPrice : price;

        if (decimal.TryParse(bybitOrder.CumExecValue, out var cumExecValue))
            trade.CumulativeQuoteQuantity = cumExecValue > 0 ? cumExecValue : executedQty * (avgPrice > 0 ? avgPrice : price);

        if (decimal.TryParse(bybitOrder.StopPrice, out var stopPrice) && stopPrice > 0)
            trade.StopPrice = stopPrice;

        if (decimal.TryParse(bybitOrder.CumExecFee, out var cumExecFee))
            trade.Fee = cumExecFee;

        return trade;
    }

    /// <summary>
    /// Creates a FuturesTrade instance from a BingX futures trade order
    /// </summary>
    /// <param name="bingxOrder">The BingX futures trade order</param>
    /// <returns>A FuturesTrade instance with mapped values from the BingX order</returns>
    public static FuturesTrade FromBingXFuturesOrder(BingXFuturesTradeOrder bingxOrder)
    {
        var trade = new FuturesTrade
        {
            Symbol = bingxOrder.Symbol,
            OrderId = bingxOrder.OrderId,
            Side = bingxOrder.Side,
            PositionSide = bingxOrder.PositionSide,
            OrderType = bingxOrder.Type,
            Status = bingxOrder.Status,
            TimeInForce = bingxOrder.OrderType, // BingX uses different structure
            Time = bingxOrder.Time,
            UpdateTime = bingxOrder.UpdateTime,
            FeeAsset = "USDT", // BingX typically uses USDT for futures fees
            Leverage = bingxOrder.Leverage,
            ReduceOnly = bingxOrder.ReduceOnly,
            WorkingType = bingxOrder.WorkingType,
            ClientOrderId = bingxOrder.ClientOrderId,
            Exchange = "BingX"
        };

        // Parse decimal values with error handling
        if (decimal.TryParse(bingxOrder.OrigQty, out var quantity))
            trade.Quantity = quantity;

        if (decimal.TryParse(bingxOrder.Price, out var price))
            trade.Price = price;

        if (decimal.TryParse(bingxOrder.AvgPrice, out var avgPrice))
            trade.AvgPrice = avgPrice;

        if (decimal.TryParse(bingxOrder.ExecutedQty, out var executedQty))
            trade.ExecutedQuantity = executedQty;

        if (decimal.TryParse(bingxOrder.CumQuote, out var cumQuote))
            trade.CumulativeQuoteQuantity = cumQuote;

        if (decimal.TryParse(bingxOrder.Commission, out var commission))
            trade.Fee = commission;

        if (decimal.TryParse(bingxOrder.Profit, out var profit))
            trade.RealizedPnl = profit;

        // Parse optional fields
        if (decimal.TryParse(bingxOrder.StopPrice, out var stopPrice) && stopPrice > 0)
            trade.StopPrice = stopPrice;

        return trade;
    }
}
