using System.Text.Json.Serialization;

namespace CryptoPositionAnalysis.Models;

// BingX API specific response models
public class BingXApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    public bool IsSuccess => Code == 0;
}

// BingX Balance response structure
public class BingXBalanceData
{
    [JsonPropertyName("balances")]
    public List<BingXBalance>? Balances { get; set; }
}

public class BingXBalance
{
    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("free")]
    public string Free { get; set; } = "0";

    [JsonPropertyName("locked")]
    public string Locked { get; set; } = "0";
}

// BingX Futures Balance response
public class BingXFuturesBalanceData
{
    [JsonPropertyName("balance")]
    public BingXFuturesBalanceInfo? Balance { get; set; }
}

public class BingXFuturesBalanceInfo
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public string Balance { get; set; } = "0";

    [JsonPropertyName("equity")]
    public string Equity { get; set; } = "0";

    [JsonPropertyName("unrealizedProfit")]
    public string UnrealizedProfit { get; set; } = "0";

    [JsonPropertyName("realisedProfit")]
    public string RealisedProfit { get; set; } = "0";

    [JsonPropertyName("availableMargin")]
    public string AvailableMargin { get; set; } = "0";

    [JsonPropertyName("usedMargin")]
    public string UsedMargin { get; set; } = "0";

    [JsonPropertyName("freezedMargin")]
    public string FreezedMargin { get; set; } = "0";

    [JsonPropertyName("shortUid")]
    public string ShortUid { get; set; } = string.Empty;
}

// BingX Trade History response
public class BingXTradeHistoryData
{
    [JsonPropertyName("orders")]
    public List<BingXTradeOrder>? Orders { get; set; }
}

public class BingXTradeOrder
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("clientOrderId")]
    public string ClientOrderId { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";

    [JsonPropertyName("origQty")]
    public string OrigQty { get; set; } = "0";

    [JsonPropertyName("executedQty")]
    public string ExecutedQty { get; set; } = "0";

    [JsonPropertyName("cummulativeQuoteQty")]
    public string CummulativeQuoteQty { get; set; } = "0";

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timeInForce")]
    public string TimeInForce { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("updateTime")]
    public long UpdateTime { get; set; }
}

// BingX Futures Trade History response
public class BingXFuturesTradeData
{
    [JsonPropertyName("orders")]
    public List<BingXFuturesTradeOrder>? Orders { get; set; }
}

public class BingXFuturesTradeOrder
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;

    [JsonPropertyName("positionSide")]
    public string PositionSide { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("origQty")]
    public string OrigQty { get; set; } = "0";

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";

    [JsonPropertyName("executedQty")]
    public string ExecutedQty { get; set; } = "0";

    [JsonPropertyName("avgPrice")]
    public string AvgPrice { get; set; } = "0";

    [JsonPropertyName("cumQuote")]
    public string CumQuote { get; set; } = "0";

    [JsonPropertyName("stopPrice")]
    public string StopPrice { get; set; } = "0";

    [JsonPropertyName("profit")]
    public string Profit { get; set; } = "0";

    [JsonPropertyName("commission")]
    public string Commission { get; set; } = "0";

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("updateTime")]
    public long UpdateTime { get; set; }

    [JsonPropertyName("clientOrderId")]
    public string ClientOrderId { get; set; } = string.Empty;

    [JsonPropertyName("leverage")]
    public string Leverage { get; set; } = string.Empty;

    [JsonPropertyName("takeProfit")]
    public BingXTakeProfitInfo? TakeProfit { get; set; }

    [JsonPropertyName("stopLoss")]
    public BingXStopLossInfo? StopLoss { get; set; }

    [JsonPropertyName("advanceAttr")]
    public int AdvanceAttr { get; set; }

    [JsonPropertyName("positionID")]
    public long PositionID { get; set; }

    [JsonPropertyName("takeProfitEntrustPrice")]
    public decimal TakeProfitEntrustPrice { get; set; }

    [JsonPropertyName("stopLossEntrustPrice")]
    public decimal StopLossEntrustPrice { get; set; }

    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty;

    [JsonPropertyName("workingType")]
    public string WorkingType { get; set; } = string.Empty;

    [JsonPropertyName("onlyOnePosition")]
    public bool OnlyOnePosition { get; set; }

    [JsonPropertyName("reduceOnly")]
    public bool ReduceOnly { get; set; }

    [JsonPropertyName("postOnly")]
    public bool PostOnly { get; set; }

    [JsonPropertyName("stopGuaranteed")]
    public string StopGuaranteed { get; set; } = string.Empty;

    [JsonPropertyName("triggerOrderId")]
    public long TriggerOrderId { get; set; }

    [JsonPropertyName("trailingStopRate")]
    public decimal TrailingStopRate { get; set; }

    [JsonPropertyName("trailingStopDistance")]
    public decimal TrailingStopDistance { get; set; }

    [JsonPropertyName("is_twap")]
    public bool IsTwap { get; set; }

    [JsonPropertyName("mainOrderId")]
    public string MainOrderId { get; set; } = string.Empty;

    [JsonPropertyName("priceRate")]
    public string PriceRate { get; set; } = string.Empty;

    [JsonPropertyName("actPrice")]
    public string ActPrice { get; set; } = string.Empty;

    [JsonPropertyName("closePosition")]
    public string ClosePosition { get; set; } = string.Empty;

    // Legacy property name mappings for backward compatibility
    [JsonPropertyName("timeInForce")]
    public string TimeInForce { get; set; } = string.Empty;

    [JsonPropertyName("commissionAsset")]
    public string CommissionAsset { get; set; } = string.Empty;

    [JsonPropertyName("realizedPnl")]
    public string RealizedPnl { get; set; } = "0";
}

// Supporting classes for takeProfit and stopLoss
public class BingXTakeProfitInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("stopPrice")]
    public decimal StopPrice { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("workingType")]
    public string WorkingType { get; set; } = string.Empty;

    [JsonPropertyName("stopGuaranteed")]
    public string StopGuaranteed { get; set; } = string.Empty;
}

public class BingXStopLossInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("stopPrice")]
    public decimal StopPrice { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("workingType")]
    public string WorkingType { get; set; } = string.Empty;

    [JsonPropertyName("stopGuaranteed")]
    public string StopGuaranteed { get; set; } = string.Empty;
}

// BingX Positions response - data is an array directly
public class BingXPositionsResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<BingXPosition>? Data { get; set; }
}

public class BingXPosition
{
    [JsonPropertyName("positionId")]
    public string PositionId { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("positionAmt")]
    public string PositionAmt { get; set; } = "0";

    [JsonPropertyName("availableAmt")]
    public string AvailableAmt { get; set; } = "0";

    [JsonPropertyName("positionSide")]
    public string PositionSide { get; set; } = string.Empty;

    [JsonPropertyName("isolated")]
    public bool Isolated { get; set; }

    [JsonPropertyName("avgPrice")]
    public string AvgPrice { get; set; } = "0";

    [JsonPropertyName("initialMargin")]
    public string InitialMargin { get; set; } = "0";

    [JsonPropertyName("margin")]
    public string Margin { get; set; } = "0";

    [JsonPropertyName("leverage")]
    public int Leverage { get; set; }

    [JsonPropertyName("unrealizedProfit")]
    public string UnrealizedProfit { get; set; } = "0";

    [JsonPropertyName("realisedProfit")]
    public string RealisedProfit { get; set; } = "0";

    [JsonPropertyName("liquidationPrice")]
    public decimal LiquidationPrice { get; set; }

    [JsonPropertyName("pnlRatio")]
    public string PnlRatio { get; set; } = "0";

    [JsonPropertyName("maxMarginReduction")]
    public string MaxMarginReduction { get; set; } = "0";

    [JsonPropertyName("riskRate")]
    public string RiskRate { get; set; } = "0";

    [JsonPropertyName("markPrice")]
    public string MarkPrice { get; set; } = "0";

    [JsonPropertyName("positionValue")]
    public string PositionValue { get; set; } = "0";

    [JsonPropertyName("onlyOnePosition")]
    public bool OnlyOnePosition { get; set; }

    [JsonPropertyName("createTime")]
    public long CreateTime { get; set; }

    [JsonPropertyName("updateTime")]
    public long UpdateTime { get; set; }
}
