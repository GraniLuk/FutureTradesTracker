namespace FutureTradesTracker.Models;

public class Position
{
    public string Symbol { get; set; } = string.Empty;

    public PositionSide PositionSide { get; set; }

    public decimal PositionSize { get; set; }

    public decimal EntryPrice { get; set; }

    public decimal MarkPrice { get; set; }

    public decimal UnrealizedPnl { get; set; }

    public decimal Percentage { get; set; }

    public decimal IsolatedMargin { get; set; }

    public decimal Notional { get; set; }

    public decimal IsolatedWallet { get; set; }

    public long UpdateTime { get; set; }

    public bool Isolated { get; set; }

    public int AdlQuantile { get; set; }

    public decimal BidNotional { get; set; }

    public decimal AskNotional { get; set; }

    public decimal PositionInitialMargin { get; set; }

    public decimal OpenOrderInitialMargin { get; set; }

    public decimal Leverage { get; set; }

    public decimal MaxNotional { get; set; }

    public DateTime LastUpdateTime => DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).DateTime;

    public string Exchange { get; set; } = string.Empty;

    public bool HasPosition => Math.Abs(PositionSize) > 0;
}
