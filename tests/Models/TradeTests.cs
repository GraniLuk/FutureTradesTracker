using NUnit.Framework;
using FluentAssertions;
using CryptoPositionAnalysis.Models;

namespace CryptoPositionAnalysis.Tests.Models;

[TestFixture]
public class TradeTests
{
    [Test]
    public void FromBingXOrder_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var bingxOrder = new BingXTradeOrder
        {
            Symbol = "BTCUSDT",
            OrderId = 12345678901234567L,
            Side = "BUY",
            Type = "LIMIT",
            Status = "FILLED",
            TimeInForce = "GTC",
            Time = 1641024000000L, // 2022-01-01 12:00:00 UTC
            UpdateTime = 1641024300000L, // 2022-01-01 12:05:00 UTC
            Price = "50000.00",
            OrigQty = "0.001",
            ExecutedQty = "0.001",
            CummulativeQuoteQty = "50.00"
        };

        // Act
        var trade = Trade.FromBingXOrder(bingxOrder);

        // Assert
        trade.Should().NotBeNull();
        trade.Symbol.Should().Be("BTCUSDT");
        trade.OrderId.Should().Be(12345678901234567L);
        trade.Side.Should().Be("BUY");
        trade.OrderType.Should().Be("LIMIT");
        trade.Status.Should().Be("FILLED");
        trade.TimeInForce.Should().Be("GTC");
        trade.TradeTime.Should().Be(1641024000000L);
        trade.UpdateTime.Should().Be(1641024300000L);
        trade.Exchange.Should().Be("BingX");
        trade.Price.Should().Be(50000.00m);
        trade.Quantity.Should().Be(0.001m);
        trade.ExecutedQuantity.Should().Be(0.001m);
        trade.CumulativeQuoteQuantity.Should().Be(50.00m);
    }

    [Test]
    public void FromBingXOrder_ShouldHandleInvalidDecimalValues()
    {
        // Arrange
        var bingxOrder = new BingXTradeOrder
        {
            Symbol = "BTCUSDT",
            OrderId = 12345678901234567L,
            Side = "BUY",
            Type = "LIMIT",
            Status = "FILLED",
            TimeInForce = "GTC",
            Time = 1641024000000L,
            UpdateTime = 1641024300000L,
            Price = "invalid",
            OrigQty = "not_a_number",
            ExecutedQty = "",
            CummulativeQuoteQty = "null"
        };

        // Act
        var trade = Trade.FromBingXOrder(bingxOrder);

        // Assert
        trade.Should().NotBeNull();
        trade.Symbol.Should().Be("BTCUSDT");
        trade.OrderId.Should().Be(12345678901234567L);
        trade.Side.Should().Be("BUY");
        trade.OrderType.Should().Be("LIMIT");
        trade.Status.Should().Be("FILLED");
        trade.TimeInForce.Should().Be("GTC");
        trade.TradeTime.Should().Be(1641024000000L);
        trade.UpdateTime.Should().Be(1641024300000L);
        trade.Exchange.Should().Be("BingX");
        
        // Invalid decimal values should default to 0
        trade.Price.Should().Be(0m);
        trade.Quantity.Should().Be(0m);
        trade.ExecutedQuantity.Should().Be(0m);
        trade.CumulativeQuoteQuantity.Should().Be(0m);
    }

    [Test]
    public void FromBingXOrder_ShouldSetTradeDateTimeCorrectly()
    {
        // Arrange
        var bingxOrder = new BingXTradeOrder
        {
            Symbol = "BTCUSDT",
            OrderId = 12345678901234567L,
            Side = "BUY",
            Type = "LIMIT",
            Status = "FILLED",
            TimeInForce = "GTC",
            Time = 1641024000000L, // 2022-01-01 12:00:00 UTC
            UpdateTime = 1641024300000L,
            Price = "50000.00",
            OrigQty = "0.001",
            ExecutedQty = "0.001",
            CummulativeQuoteQty = "50.00"
        };

        // Act
        var trade = Trade.FromBingXOrder(bingxOrder);

        // Assert
        var expectedDateTime = DateTimeOffset.FromUnixTimeMilliseconds(1641024000000L).DateTime;
        trade.TradeDateTime.Should().Be(expectedDateTime);
    }

    [Test]
    public void FromBingXOrder_ShouldHandleNullOrEmptyStringValues()
    {
        // Arrange
        var bingxOrder = new BingXTradeOrder
        {
            Symbol = "",
            OrderId = 0,
            Side = null!,
            Type = null!,
            Status = null!,
            TimeInForce = null!,
            Time = 0,
            UpdateTime = 0,
            Price = null!,
            OrigQty = null!,
            ExecutedQty = null!,
            CummulativeQuoteQty = null!
        };

        // Act
        var trade = Trade.FromBingXOrder(bingxOrder);

        // Assert
        trade.Should().NotBeNull();
        trade.Symbol.Should().Be("");
        trade.OrderId.Should().Be(0);
        trade.Side.Should().Be(null);
        trade.OrderType.Should().Be(null);
        trade.Status.Should().Be(null);
        trade.TimeInForce.Should().Be(null);
        trade.TradeTime.Should().Be(0);
        trade.UpdateTime.Should().Be(0);
        trade.Exchange.Should().Be("BingX");
        
        // Null/empty decimal values should default to 0
        trade.Price.Should().Be(0m);
        trade.Quantity.Should().Be(0m);
        trade.ExecutedQuantity.Should().Be(0m);
        trade.CumulativeQuoteQuantity.Should().Be(0m);
    }

    [Test]
    public void FromBingXOrder_ShouldHandleZeroValues()
    {
        // Arrange
        var bingxOrder = new BingXTradeOrder
        {
            Symbol = "BTCUSDT",
            OrderId = 12345678901234567L,
            Side = "BUY",
            Type = "LIMIT",
            Status = "FILLED",
            TimeInForce = "GTC",
            Time = 1641024000000L,
            UpdateTime = 1641024300000L,
            Price = "0.00",
            OrigQty = "0.000",
            ExecutedQty = "0",
            CummulativeQuoteQty = "0.0"
        };

        // Act
        var trade = Trade.FromBingXOrder(bingxOrder);

        // Assert
        trade.Should().NotBeNull();
        trade.Price.Should().Be(0m);
        trade.Quantity.Should().Be(0m);
        trade.ExecutedQuantity.Should().Be(0m);
        trade.CumulativeQuoteQuantity.Should().Be(0m);
    }

    [Test]
    public void FromBingXOrder_ShouldHandleLargeDecimalValues()
    {
        // Arrange
        var bingxOrder = new BingXTradeOrder
        {
            Symbol = "BTCUSDT",
            OrderId = 12345678901234567L,
            Side = "BUY",
            Type = "LIMIT",
            Status = "FILLED",
            TimeInForce = "GTC",
            Time = 1641024000000L,
            UpdateTime = 1641024300000L,
            Price = "999999999.99999999",
            OrigQty = "123456789.12345678",
            ExecutedQty = "123456789.12345678",
            CummulativeQuoteQty = "123456789123456789.99"
        };

        // Act
        var trade = Trade.FromBingXOrder(bingxOrder);

        // Assert
        trade.Should().NotBeNull();
        trade.Price.Should().Be(999999999.99999999m);
        trade.Quantity.Should().Be(123456789.12345678m);
        trade.ExecutedQuantity.Should().Be(123456789.12345678m);
        trade.CumulativeQuoteQuantity.Should().Be(123456789123456789.99m);
    }
}
