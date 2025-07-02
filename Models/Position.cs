using System.Text.Json.Serialization;

namespace CryptoPositionAnalysis.Models;

public class Position
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("positionSide")]
    public string PositionSide { get; set; } = string.Empty;

    [JsonPropertyName("positionAmt")]
    public decimal PositionSize { get; set; }

    [JsonPropertyName("entryPrice")]
    public decimal EntryPrice { get; set; }

    [JsonPropertyName("markPrice")]
    public decimal MarkPrice { get; set; }

    [JsonPropertyName("unRealizedProfit")]
    public decimal UnrealizedPnl { get; set; }

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }

    [JsonPropertyName("isolatedMargin")]
    public decimal IsolatedMargin { get; set; }

    [JsonPropertyName("notional")]
    public decimal Notional { get; set; }

    [JsonPropertyName("isolatedWallet")]
    public decimal IsolatedWallet { get; set; }

    [JsonPropertyName("updateTime")]
    public long UpdateTime { get; set; }

    [JsonPropertyName("isolated")]
    public bool Isolated { get; set; }

    [JsonPropertyName("adlQuantile")]
    public int AdlQuantile { get; set; }

    [JsonPropertyName("bidNotional")]
    public decimal BidNotional { get; set; }

    [JsonPropertyName("askNotional")]
    public decimal AskNotional { get; set; }

    [JsonPropertyName("positionInitialMargin")]
    public decimal PositionInitialMargin { get; set; }

    [JsonPropertyName("openOrderInitialMargin")]
    public decimal OpenOrderInitialMargin { get; set; }

    [JsonPropertyName("leverage")]
    public decimal Leverage { get; set; }

    [JsonPropertyName("maxNotional")]
    public decimal MaxNotional { get; set; }

    public DateTime LastUpdateTime => DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).DateTime;

    public string Exchange { get; set; } = string.Empty;

    public bool HasPosition => Math.Abs(PositionSize) > 0;
}
