using System.Text.Json.Serialization;
using FutureTradesTracker.Services;

namespace FutureTradesTracker.Models;

public class FuturesTrade
{
    public string Symbol { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public PositionSide PositionSide { get; set; }

    public string OrderType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal AvgPrice { get; set; }

    public decimal ExecutedQuantity { get; set; }

    public decimal CumulativeQuoteQuantity { get; set; }

    public decimal? StopPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TimeInForce { get; set; } = string.Empty;

    public long Time { get; set; }

    public long UpdateTime { get; set; }

    public decimal Fee { get; set; }

    public string FeeAsset { get; set; } = string.Empty;

    public decimal RealizedPnl { get; set; }

    public string? Leverage { get; set; }

    public bool? ReduceOnly { get; set; }

    public string? WorkingType { get; set; }

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
            OrderId = bybitOrder.OrderId,
            Side = bybitOrder.Side.ToUpper(),
            PositionSide = ParsePositionSide(bybitOrder.PositionIdx.ToString()),
            OrderType = bybitOrder.OrderType,
            Status = bybitOrder.OrderStatus,
            TimeInForce = bybitOrder.TimeInForce,
            ReduceOnly = bybitOrder.ReduceOnly,
            FeeAsset = "USDT", // Bybit typically uses USDT for futures fees
            Exchange = "Bybit"
        };

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
            OrderId = bingxOrder.OrderId.ToString(),
            Side = bingxOrder.Side,
            PositionSide = ParsePositionSide(bingxOrder.PositionSide),
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

    /// <summary>
    /// Creates a FuturesTrade instance from a Bybit execution (fill) record
    /// </summary>
    /// <param name="bybitExecution">The Bybit execution record</param>
    /// <returns>A FuturesTrade instance with mapped values from the Bybit execution</returns>
    public static FuturesTrade FromBybitExecution(BybitExecution bybitExecution)
    {
        var trade = new FuturesTrade
        {
            Symbol = bybitExecution.Symbol,
            OrderId = bybitExecution.OrderId,
            Side = bybitExecution.Side,
            PositionSide = ParsePositionSide(bybitExecution.Side),
            OrderType = bybitExecution.OrderType,
            Status = "FILLED", // Executions are always filled
            TimeInForce = "IOC", // Default for executions
            FeeAsset = "USDT", // Bybit typically uses USDT for futures fees
            Exchange = "Bybit"
        };

        // Parse decimal values with error handling
        if (decimal.TryParse(bybitExecution.ExecQty, out var execQty))
        {
            trade.Quantity = execQty;
            trade.ExecutedQuantity = execQty; // For executions, these are the same
        }

        if (decimal.TryParse(bybitExecution.ExecPrice, out var execPrice))
        {
            trade.Price = execPrice;
            trade.AvgPrice = execPrice; // For single execution, avg price = exec price
        }

        if (decimal.TryParse(bybitExecution.ExecValue, out var execValue))
            trade.CumulativeQuoteQuantity = execValue;

        if (decimal.TryParse(bybitExecution.ExecFee, out var execFee))
            trade.Fee = execFee;

        // This is the key field - per-execution realized PnL!
        if (decimal.TryParse(bybitExecution.ClosedPnl, out var closedPnl))
            trade.RealizedPnl = closedPnl;

        // Parse timestamps
        if (long.TryParse(bybitExecution.ExecTime, out var execTime))
        {
            trade.Time = execTime;
            trade.UpdateTime = execTime; // For executions, these are typically the same
        }

        return trade;
    }

    /// <summary>
    /// Creates a FuturesTrade instance from a Bybit closed PnL record
    /// </summary>
    /// <param name="bybitClosedPnl">The Bybit closed PnL record</param>
    /// <returns>A FuturesTrade instance with mapped values from the Bybit closed PnL record</returns>
    public static FuturesTrade FromBybitClosedPnl(BybitClosedPnl bybitClosedPnl)
    {
        var trade = new FuturesTrade
        {
            Symbol = bybitClosedPnl.Symbol,
            OrderId = bybitClosedPnl.OrderId,
            Side = bybitClosedPnl.Side.ToUpper(),
            PositionSide = ParsePositionSide(bybitClosedPnl.Side),
            OrderType = bybitClosedPnl.OrderType,
            Status = "CLOSED", // Closed PnL records represent closed positions
            TimeInForce = "IOC", // Default for closed positions
            FeeAsset = "USDT", // Bybit typically uses USDT for futures fees
            Exchange = "Bybit" // Distinguish from regular orders
        };

        // Parse decimal values with error handling
        if (decimal.TryParse(bybitClosedPnl.Qty, out var qty))
        {
            trade.Quantity = qty;
            trade.ExecutedQuantity = qty; // For closed positions, these are the same
        }

        if (decimal.TryParse(bybitClosedPnl.ClosedSize, out var closedSize))
            trade.ExecutedQuantity = closedSize; // Use closed size as executed quantity

        if (decimal.TryParse(bybitClosedPnl.OrderPrice, out var orderPrice))
            trade.Price = orderPrice;

        if (decimal.TryParse(bybitClosedPnl.AvgEntryPrice, out var avgEntryPrice))
            trade.AvgPrice = avgEntryPrice;

        if (decimal.TryParse(bybitClosedPnl.AvgExitPrice, out var avgExitPrice))
            trade.AvgPrice = avgExitPrice; // Use exit price as the average price for closed positions

        if (decimal.TryParse(bybitClosedPnl.CumExitValue, out var cumExitValue))
            trade.CumulativeQuoteQuantity = cumExitValue;

        // Parse fees - combine open and close fees
        decimal totalFee = 0;
        if (decimal.TryParse(bybitClosedPnl.OpenFee, out var openFee))
            totalFee += openFee;
        if (decimal.TryParse(bybitClosedPnl.CloseFee, out var closeFee))
            totalFee += closeFee;
        trade.Fee = totalFee;

        // This is the key field - realized PnL from closed position!
        if (decimal.TryParse(bybitClosedPnl.ClosedPnl, out var closedPnl))
            trade.RealizedPnl = closedPnl;

        // Parse leverage
        if (decimal.TryParse(bybitClosedPnl.Leverage, out var leverage))
            trade.Leverage = leverage.ToString() + "X"; // Append 'x' to indicate leverage

        // Parse timestamps
        if (long.TryParse(bybitClosedPnl.CreatedTime, out var createdTime))
        {
            trade.Time = createdTime;
        }

        if (long.TryParse(bybitClosedPnl.UpdatedTime, out var updatedTime))
        {
            trade.UpdateTime = updatedTime;
        }

        return trade;
    }

    private static PositionSide ParsePositionSide(string positionSide)
    {
        return positionSide?.ToUpperInvariant() switch
        {
            "0" => PositionSide.Long,
            "1" => PositionSide.Short,
            "LONG" => PositionSide.Long,
            "SHORT" => PositionSide.Short,
            "SELL" => PositionSide.Long,  // Bybit execution endpoint uses "Buy" for long positions
            "BUY" => PositionSide.Short, // Bybit execution endpoint uses "Sell" for short positions
            _ => throw new ArgumentException($"Unknown position side: {positionSide}")
        };
    }
}