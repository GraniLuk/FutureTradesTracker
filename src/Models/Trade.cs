using System.Text.Json.Serialization;
using FutureTradesTracker.Services;

namespace FutureTradesTracker.Models;

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

    /// <summary>
    /// Creates a Trade instance from a BingX trade order
    /// </summary>
    /// <param name="bingxOrder">The BingX trade order</param>
    /// <returns>A Trade instance with mapped values from the BingX order</returns>
    public static Trade FromBingXOrder(BingXTradeOrder bingxOrder)
    {
        var trade = new Trade
        {
            Symbol = bingxOrder.Symbol,
            OrderId = bingxOrder.OrderId,
            Side = bingxOrder.Side,
            OrderType = bingxOrder.Type,
            Status = bingxOrder.Status,
            TimeInForce = bingxOrder.TimeInForce,
            TradeTime = bingxOrder.Time,
            UpdateTime = bingxOrder.UpdateTime,
            Exchange = "BingX"
        };

        // Parse decimal values with error handling
        if (decimal.TryParse(bingxOrder.OrigQty, out var quantity))
            trade.Quantity = quantity;

        if (decimal.TryParse(bingxOrder.Price, out var price))
            trade.Price = price;

        if (decimal.TryParse(bingxOrder.ExecutedQty, out var executedQty))
            trade.ExecutedQuantity = executedQty;

        if (decimal.TryParse(bingxOrder.CummulativeQuoteQty, out var cumQuote))
            trade.CumulativeQuoteQuantity = cumQuote;

        return trade;
    }

    /// <summary>
    /// Creates a Trade instance from a Bybit order
    /// </summary>
    /// <param name="bybitOrder">The Bybit order</param>
    /// <returns>A Trade instance with mapped values from the Bybit order</returns>
    public static Trade FromBybitOrder(BybitOrder bybitOrder)
    {
        var trade = new Trade
        {
            Symbol = bybitOrder.Symbol,
            Side = bybitOrder.Side,
            OrderType = bybitOrder.OrderType,
            Status = bybitOrder.OrderStatus,
            TimeInForce = bybitOrder.TimeInForce,
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
            trade.TradeTime = createdTime;

        if (long.TryParse(bybitOrder.UpdatedTime, out var updatedTime))
            trade.UpdateTime = updatedTime;

        return trade;
    }
}
