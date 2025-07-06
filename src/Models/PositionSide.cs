using System.Text.Json.Serialization;

namespace FutureTradesTracker.Models;

/// <summary>
/// Represents the side of a futures position
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PositionSide
{
    /// <summary>
    /// Long position (buying to profit from price increases)
    /// </summary>
    Long,

    /// <summary>
    /// Short position (selling to profit from price decreases)
    /// </summary>
    Short
}
