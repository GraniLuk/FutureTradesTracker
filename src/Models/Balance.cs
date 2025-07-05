using System.Text.Json.Serialization;

namespace CryptoPositionAnalysis.Models;

public class Balance
{
    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("free")]
    public decimal Available { get; set; }

    [JsonPropertyName("locked")]
    public decimal Locked { get; set; }

    public decimal Total => Available + Locked;

    [JsonPropertyName("usdValue")]
    public decimal? UsdValue { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Exchange { get; set; } = string.Empty;
}

public class FuturesBalance
{
    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("crossWalletBalance")]
    public decimal CrossWalletBalance { get; set; }

    [JsonPropertyName("crossUnPnl")]
    public decimal CrossUnrealizedPnl { get; set; }

    [JsonPropertyName("availableBalance")]
    public decimal AvailableBalance { get; set; }

    [JsonPropertyName("maxWithdrawAmount")]
    public decimal MaxWithdrawAmount { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Exchange { get; set; } = string.Empty;
}
