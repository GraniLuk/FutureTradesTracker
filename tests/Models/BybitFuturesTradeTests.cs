using System.Text.Json;
using NUnit.Framework;
using FluentAssertions;
using FutureTradesTracker.Models;
using FutureTradesTracker.Services;

namespace FutureTradesTracker.Tests.Models;

[TestFixture]
public class BybitFuturesTradeTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public void JsonDeserialization_ShouldMapToBybitOrderHistoryData()
    {
        // Arrange
        var json = """
        {
            "retCode": 0,
            "retMsg": "OK",
            "result": {
                "list": [
                    {
                        "symbol": "VIRTUALUSDT",
                        "orderType": "Limit",
                        "orderLinkId": "",
                        "slLimitPrice": "0",
                        "orderId": "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
                        "cancelType": "UNKNOWN",
                        "avgPrice": "1.8149",
                        "stopOrderType": "",
                        "lastPriceOnCreated": "1.9562",
                        "orderStatus": "Filled",
                        "createType": "CreateByUser",
                        "takeProfit": "",
                        "cumExecValue": "1994.5751",
                        "tpslMode": "",
                        "smpType": "None",
                        "triggerDirection": 0,
                        "blockTradeId": "",
                        "rejectReason": "EC_NoError",
                        "isLeverage": "",
                        "price": "1.8149",
                        "orderIv": "",
                        "createdTime": "1749756677602",
                        "tpTriggerBy": "",
                        "positionIdx": 0,
                        "timeInForce": "GTC",
                        "leavesValue": "0",
                        "updatedTime": "1749773415705",
                        "side": "Buy",
                        "smpGroup": 0,
                        "triggerPrice": "",
                        "tpLimitPrice": "0",
                        "cumExecFee": "0.39891502",
                        "slTriggerBy": "",
                        "leavesQty": "0",
                        "closeOnTrigger": false,
                        "slippageToleranceType": "UNKNOWN",
                        "placeType": "",
                        "cumExecQty": "1099",
                        "reduceOnly": false,
                        "qty": "1099",
                        "stopLoss": "",
                        "smpOrderId": "",
                        "slippageTolerance": "0",
                        "triggerBy": "",
                        "extraFees": ""
                    }
                ]
            },
            "time": 1749773415705
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<BybitApiResponse<BybitOrderHistoryData>>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.RetCode.Should().Be(0);
        response.RetMsg.Should().Be("OK");
        response.Result.Should().NotBeNull();
        response.Result!.List.Should().NotBeNull();
        response.Result.List!.Should().HaveCount(1);

        var order = response.Result.List![0];
        order.Should().NotBeNull();
        order!.Symbol.Should().Be("VIRTUALUSDT");
        order.OrderType.Should().Be("Limit");
        order.OrderId.Should().Be("6c5d2b6e-5e29-41b3-accf-3d0c53a19363");
        order.AvgPrice.Should().Be("1.8149");
        order.OrderStatus.Should().Be("Filled");
        order.CumExecValue.Should().Be("1994.5751");
        order.Price.Should().Be("1.8149");
        order.CreatedTime.Should().Be("1749756677602");
        order.UpdatedTime.Should().Be("1749773415705");
        order.Side.Should().Be("Buy");
        order.CumExecFee.Should().Be("0.39891502");
        order.CumExecQty.Should().Be("1099");
        order.Qty.Should().Be("1099");
        order.TimeInForce.Should().Be("GTC");
        order.PositionIdx.Should().Be(0);
        order.ReduceOnly.Should().Be(false);
        order.CloseOnTrigger.Should().Be(false);
        order.TriggerDirection.Should().Be(0);
        order.SmpGroup.Should().Be(0);
    }

    [Test]
    public void FromBybitOrder_ShouldMapToFuturesTradeCorrectly()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderType = "Limit",
            OrderId = "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
            AvgPrice = "1.8149",
            OrderStatus = "Filled",
            CumExecValue = "1994.5751",
            Price = "1.8149",
            CreatedTime = "1749756677602",
            UpdatedTime = "1749773415705",
            Side = "Buy",
            CumExecFee = "0.39891502",
            CumExecQty = "1099",
            Qty = "1099",
            TimeInForce = "GTC",
            PositionIdx = 0,
            ReduceOnly = false,
            CloseOnTrigger = false,
            TriggerDirection = 0,
            SmpGroup = 0,
            StopPrice = "0",
            TakeProfitPrice = "0",
            StopLossPrice = "0",
            RejectReason = "EC_NoError",
            TriggerPrice = "0",
            TakeProfit = "",
            StopLoss = "",
            TriggerBy = "",
            TpTriggerBy = "",
            SlTriggerBy = ""
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY"); // Should be uppercase
        futuresTrade.OrderType.Should().Be("Limit");
        futuresTrade.Status.Should().Be("Filled");
        futuresTrade.TimeInForce.Should().Be("GTC");
        futuresTrade.ReduceOnly.Should().Be(false);
        futuresTrade.FeeAsset.Should().Be("USDT");
        futuresTrade.Exchange.Should().Be("Bybit");
        
        // Test decimal conversions
        futuresTrade.Quantity.Should().Be(1099m);
        futuresTrade.Price.Should().Be(1.8149m);
        futuresTrade.ExecutedQuantity.Should().Be(1099m);
        futuresTrade.AvgPrice.Should().Be(1.8149m);
        futuresTrade.CumulativeQuoteQuantity.Should().Be(1994.5751m);
        futuresTrade.Fee.Should().Be(0.39891502m);
        
        // Test timestamp conversions
        futuresTrade.Time.Should().Be(1749756677602L);
        futuresTrade.UpdateTime.Should().Be(1749773415705L);
        
        // Test DateTime conversion
        var expectedDateTime = DateTimeOffset.FromUnixTimeMilliseconds(1749756677602L).DateTime;
        futuresTrade.TradeDateTime.Should().Be(expectedDateTime);
        
        // StopPrice should be null for zero values
        futuresTrade.StopPrice.Should().BeNull();
    }

    [Test]
    public void FromBybitOrder_ShouldHandleInvalidOrderId()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "invalid-order-id", // Non-numeric order ID
            Side = "Buy",
            OrderType = "Limit",
            OrderStatus = "Filled",
            TimeInForce = "GTC",
            Qty = "1099",
            Price = "1.8149",
            CumExecQty = "1099",
            CreatedTime = "1749756677602",
            UpdatedTime = "1749773415705"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.OrderId.Should().BeEmpty(); // Should default to 0 for invalid OrderId
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY");
    }

    [Test]
    public void FromBybitOrder_ShouldHandleInvalidDecimalValues()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
            Side = "Buy",
            OrderType = "Limit",
            OrderStatus = "Filled",
            TimeInForce = "GTC",
            Qty = "invalid", // Invalid decimal
            Price = "not-a-number", // Invalid decimal
            CumExecQty = "", // Empty string
            AvgPrice = "null", // Invalid decimal
            CumExecValue = "abc", // Invalid decimal
            CumExecFee = "xyz", // Invalid decimal
            CreatedTime = "1749756677602",
            UpdatedTime = "1749773415705"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY");
        
        // Invalid decimal values should default to 0
        futuresTrade.Quantity.Should().Be(0m);
        futuresTrade.Price.Should().Be(0m);
        futuresTrade.ExecutedQuantity.Should().Be(0m);
        futuresTrade.AvgPrice.Should().Be(0m);
        futuresTrade.CumulativeQuoteQuantity.Should().Be(0m);
        futuresTrade.Fee.Should().Be(0m);
    }

    [Test]
    public void FromBybitOrder_ShouldHandleInvalidTimestamps()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
            Side = "Buy",
            OrderType = "Limit",
            OrderStatus = "Filled",
            TimeInForce = "GTC",
            Qty = "1099",
            Price = "1.8149",
            CumExecQty = "1099",
            CreatedTime = "invalid-timestamp", // Invalid timestamp
            UpdatedTime = "not-a-number" // Invalid timestamp
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY");
        
        // Invalid timestamp values should default to 0
        futuresTrade.Time.Should().Be(0L);
        futuresTrade.UpdateTime.Should().Be(0L);
    }

    [Test]
    public void FromBybitOrder_ShouldHandleStopPriceCorrectly()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
            Side = "Buy",
            OrderType = "Limit",
            OrderStatus = "Filled",
            TimeInForce = "GTC",
            Qty = "1099",
            Price = "1.8149",
            CumExecQty = "1099",
            CreatedTime = "1749756677602",
            UpdatedTime = "1749773415705",
            StopPrice = "1.9000" // Valid stop price
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.StopPrice.Should().Be(1.9000m);
    }

    [Test]
    public void FromBybitOrder_ShouldCalculateCumulativeQuoteQuantityWhenMissing()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
            Side = "Buy",
            OrderType = "Limit",
            OrderStatus = "Filled",
            TimeInForce = "GTC",
            Qty = "1099",
            Price = "1.8149",
            CumExecQty = "1099",
            AvgPrice = "1.8149",
            CumExecValue = "0", // Zero value, should be calculated
            CreatedTime = "1749756677602",
            UpdatedTime = "1749773415705"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        // Should calculate: executedQty * avgPrice = 1099 * 1.8149 = 1994.5751
        futuresTrade.CumulativeQuoteQuantity.Should().Be(1099m * 1.8149m);
    }

    [Test]
    public void FromBybitOrder_ShouldUseAvgPriceWhenAvailable()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
            Side = "Buy",
            OrderType = "Limit",
            OrderStatus = "Filled",
            TimeInForce = "GTC",
            Qty = "1099",
            Price = "1.8000", // Different from avg price
            CumExecQty = "1099",
            AvgPrice = "1.8149", // Should use this value
            CreatedTime = "1749756677602",
            UpdatedTime = "1749773415705"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.AvgPrice.Should().Be(1.8149m);
        futuresTrade.Price.Should().Be(1.8000m);
    }

    [Test]
    public void FromBybitOrder_ShouldFallbackToPriceWhenAvgPriceIsZero()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
            Side = "Buy",
            OrderType = "Limit",
            OrderStatus = "Filled",
            TimeInForce = "GTC",
            Qty = "1099",
            Price = "1.8000",
            CumExecQty = "1099",
            AvgPrice = "0", // Zero avg price, should fallback to price
            CreatedTime = "1749756677602",
            UpdatedTime = "1749773415705"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.AvgPrice.Should().Be(1.8000m); // Should fallback to price
        futuresTrade.Price.Should().Be(1.8000m);
    }

    [Test]
    public void EndToEnd_JsonDeserializationAndMapping_ShouldWork()
    {
        // Arrange
        var json = """
        {
            "retCode": 0,
            "retMsg": "OK",
            "result": {
                "list": [
                    {
                        "symbol": "VIRTUALUSDT",
                        "orderType": "Limit",
                        "orderLinkId": "",
                        "slLimitPrice": "0",
                        "orderId": "6c5d2b6e-5e29-41b3-accf-3d0c53a19363",
                        "cancelType": "UNKNOWN",
                        "avgPrice": "1.8149",
                        "stopOrderType": "",
                        "lastPriceOnCreated": "1.9562",
                        "orderStatus": "Filled",
                        "createType": "CreateByUser",
                        "takeProfit": "",
                        "cumExecValue": "1994.5751",
                        "tpslMode": "",
                        "smpType": "None",
                        "triggerDirection": 0,
                        "blockTradeId": "",
                        "rejectReason": "EC_NoError",
                        "isLeverage": "",
                        "price": "1.8149",
                        "orderIv": "",
                        "createdTime": "1749756677602",
                        "tpTriggerBy": "",
                        "positionIdx": 0,
                        "timeInForce": "GTC",
                        "leavesValue": "0",
                        "updatedTime": "1749773415705",
                        "side": "Buy",
                        "smpGroup": 0,
                        "triggerPrice": "",
                        "tpLimitPrice": "0",
                        "cumExecFee": "0.39891502",
                        "slTriggerBy": "",
                        "leavesQty": "0",
                        "closeOnTrigger": false,
                        "slippageToleranceType": "UNKNOWN",
                        "placeType": "",
                        "cumExecQty": "1099",
                        "reduceOnly": false,
                        "qty": "1099",
                        "stopLoss": "",
                        "smpOrderId": "",
                        "slippageTolerance": "0",
                        "triggerBy": "",
                        "extraFees": ""
                    }
                ]
            },
            "time": 1749773415705
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<BybitApiResponse<BybitOrderHistoryData>>(json, _jsonOptions);
        var bybitOrder = response?.Result?.List?[0];
        var futuresTrade = bybitOrder != null ? FuturesTrade.FromBybitOrder(bybitOrder) : null;

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade!.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY");
        futuresTrade.OrderType.Should().Be("Limit");
        futuresTrade.Status.Should().Be("Filled");
        futuresTrade.TimeInForce.Should().Be("GTC");
        futuresTrade.Quantity.Should().Be(1099m);
        futuresTrade.Price.Should().Be(1.8149m);
        futuresTrade.ExecutedQuantity.Should().Be(1099m);
        futuresTrade.AvgPrice.Should().Be(1.8149m);
        futuresTrade.CumulativeQuoteQuantity.Should().Be(1994.5751m);
        futuresTrade.Fee.Should().Be(0.39891502m);
        futuresTrade.Time.Should().Be(1749756677602L);
        futuresTrade.UpdateTime.Should().Be(1749773415705L);
        futuresTrade.Exchange.Should().Be("Bybit");
        futuresTrade.FeeAsset.Should().Be("USDT");
        futuresTrade.ReduceOnly.Should().Be(false);
        
        // Verify the calculated DateTime
        var expectedDateTime = DateTimeOffset.FromUnixTimeMilliseconds(1749756677602L).DateTime;
        futuresTrade.TradeDateTime.Should().Be(expectedDateTime);
    }

    [Test]
    public void JsonDeserialization_ShouldMapMarketStopLossOrderToBybitOrderHistoryData()
    {
        // Arrange
        var json = """
        {
            "retCode": 0,
            "retMsg": "OK",
            "result": {
                "list": [
                    {
                        "symbol": "VIRTUALUSDT",
                        "orderType": "Market",
                        "orderLinkId": "",
                        "slLimitPrice": "0",
                        "orderId": "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
                        "cancelType": "UNKNOWN",
                        "avgPrice": "2.18309991",
                        "stopOrderType": "StopLoss",
                        "lastPriceOnCreated": "2.1775",
                        "orderStatus": "Filled",
                        "createType": "CreateByStopLoss",
                        "takeProfit": "",
                        "cumExecValue": "4907.6086",
                        "tpslMode": "Full",
                        "smpType": "None",
                        "triggerDirection": 1,
                        "blockTradeId": "",
                        "rejectReason": "EC_NoError",
                        "isLeverage": "",
                        "price": "2.3943",
                        "orderIv": "",
                        "createdTime": "1749671285468",
                        "tpTriggerBy": "",
                        "positionIdx": 0,
                        "timeInForce": "IOC",
                        "leavesValue": "0",
                        "updatedTime": "1749672996947",
                        "side": "Buy",
                        "smpGroup": 0,
                        "triggerPrice": "2.18",
                        "tpLimitPrice": "0",
                        "cumExecFee": "2.69918474",
                        "slTriggerBy": "",
                        "leavesQty": "0",
                        "closeOnTrigger": true,
                        "slippageToleranceType": "UNKNOWN",
                        "placeType": "",
                        "cumExecQty": "2248",
                        "reduceOnly": true,
                        "qty": "2248",
                        "stopLoss": "",
                        "smpOrderId": "",
                        "slippageTolerance": "0",
                        "triggerBy": "LastPrice",
                        "extraFees": ""
                    }
                ]
            },
            "time": 1749672996947
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<BybitApiResponse<BybitOrderHistoryData>>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.RetCode.Should().Be(0);
        response.RetMsg.Should().Be("OK");
        response.Result.Should().NotBeNull();
        response.Result!.List.Should().NotBeNull();
        response.Result.List!.Should().HaveCount(1);

        var order = response.Result.List![0];
        order.Should().NotBeNull();
        order!.Symbol.Should().Be("VIRTUALUSDT");
        order.OrderType.Should().Be("Market");
        order.OrderId.Should().Be("44885ea7-1ddb-48ff-a7ee-dd9c4d05547a");
        order.AvgPrice.Should().Be("2.18309991");
        order.OrderStatus.Should().Be("Filled");
        order.CumExecValue.Should().Be("4907.6086");
        order.Price.Should().Be("2.3943");
        order.CreatedTime.Should().Be("1749671285468");
        order.UpdatedTime.Should().Be("1749672996947");
        order.Side.Should().Be("Buy");
        order.CumExecFee.Should().Be("2.69918474");
        order.CumExecQty.Should().Be("2248");
        order.Qty.Should().Be("2248");
        order.TimeInForce.Should().Be("IOC");
        order.PositionIdx.Should().Be(0);
        order.ReduceOnly.Should().Be(true);
        order.CloseOnTrigger.Should().Be(true);
        order.TriggerDirection.Should().Be(1);
        order.SmpGroup.Should().Be(0);
        order.TriggerPrice.Should().Be("2.18");
        order.TriggerBy.Should().Be("LastPrice");
        order.StopOrderType.Should().Be("StopLoss");
    }

    [Test]
    public void FromBybitOrder_ShouldMapMarketStopLossOrderToFuturesTradeCorrectly()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderType = "Market",
            OrderId = "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
            AvgPrice = "2.18309991",
            OrderStatus = "Filled",
            CumExecValue = "4907.6086",
            Price = "2.3943",
            CreatedTime = "1749671285468",
            UpdatedTime = "1749672996947",
            Side = "Buy",
            CumExecFee = "2.69918474",
            CumExecQty = "2248",
            Qty = "2248",
            TimeInForce = "IOC",
            PositionIdx = 0,
            ReduceOnly = true,
            CloseOnTrigger = true,
            TriggerDirection = 1,
            SmpGroup = 0,
            TriggerPrice = "2.18",
            TriggerBy = "LastPrice",
            StopOrderType = "StopLoss",
            StopPrice = "0",
            TakeProfitPrice = "0",
            StopLossPrice = "0",
            RejectReason = "EC_NoError",
            TakeProfit = "",
            StopLoss = "",
            TpTriggerBy = "",
            SlTriggerBy = ""
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY"); // Should be uppercase
        futuresTrade.OrderType.Should().Be("Market");
        futuresTrade.Status.Should().Be("Filled");
        futuresTrade.TimeInForce.Should().Be("IOC");
        futuresTrade.ReduceOnly.Should().Be(true);
        futuresTrade.FeeAsset.Should().Be("USDT");
        futuresTrade.Exchange.Should().Be("Bybit");
        
        // Test decimal conversions
        futuresTrade.Quantity.Should().Be(2248m);
        futuresTrade.Price.Should().Be(2.3943m);
        futuresTrade.ExecutedQuantity.Should().Be(2248m);
        futuresTrade.AvgPrice.Should().Be(2.18309991m);
        futuresTrade.CumulativeQuoteQuantity.Should().Be(4907.6086m);
        futuresTrade.Fee.Should().Be(2.69918474m);
        
        // Test timestamp conversions
        futuresTrade.Time.Should().Be(1749671285468L);
        futuresTrade.UpdateTime.Should().Be(1749672996947L);
        
        // Test DateTime conversion
        var expectedDateTime = DateTimeOffset.FromUnixTimeMilliseconds(1749671285468L).DateTime;
        futuresTrade.TradeDateTime.Should().Be(expectedDateTime);
        
        // StopPrice should be null for zero values
        futuresTrade.StopPrice.Should().BeNull();
    }

    [Test]
    public void FromBybitOrder_ShouldHandleTriggerPriceCorrectly()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
            Side = "Buy",
            OrderType = "Market",
            OrderStatus = "Filled",
            TimeInForce = "IOC",
            Qty = "2248",
            Price = "2.3943",
            CumExecQty = "2248",
            CreatedTime = "1749671285468",
            UpdatedTime = "1749672996947",
            TriggerPrice = "2.18", // Valid trigger price
            TriggerBy = "LastPrice"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY");
        futuresTrade.OrderType.Should().Be("Market");
        
        // Note: TriggerPrice is not directly mapped in the current FuturesTrade model
        // This test verifies the mapping doesn't break with trigger price present
    }

    [Test]
    public void FromBybitOrder_ShouldHandleReduceOnlyOrderCorrectly()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
            Side = "Buy",
            OrderType = "Market",
            OrderStatus = "Filled",
            TimeInForce = "IOC",
            Qty = "2248",
            Price = "2.3943",
            CumExecQty = "2248",
            AvgPrice = "2.18309991",
            CumExecValue = "4907.6086",
            CumExecFee = "2.69918474",
            CreatedTime = "1749671285468",
            UpdatedTime = "1749672996947",
            ReduceOnly = true, // Reduce only order
            CloseOnTrigger = true
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.ReduceOnly.Should().Be(true);
        futuresTrade.Side.Should().Be("BUY");
        futuresTrade.OrderType.Should().Be("Market");
        futuresTrade.TimeInForce.Should().Be("IOC");
        
        // Verify financial calculations
        futuresTrade.Quantity.Should().Be(2248m);
        futuresTrade.ExecutedQuantity.Should().Be(2248m);
        futuresTrade.AvgPrice.Should().Be(2.18309991m);
        futuresTrade.CumulativeQuoteQuantity.Should().Be(4907.6086m);
        futuresTrade.Fee.Should().Be(2.69918474m);
    }

    [Test]
    public void FromBybitOrder_ShouldHandleIOCTimeInForceCorrectly()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
            Side = "Buy",
            OrderType = "Market",
            OrderStatus = "Filled",
            TimeInForce = "IOC", // Immediate or Cancel
            Qty = "2248",
            Price = "2.3943",
            CumExecQty = "2248",
            CreatedTime = "1749671285468",
            UpdatedTime = "1749672996947"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.TimeInForce.Should().Be("IOC");
        futuresTrade.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY");
        futuresTrade.OrderType.Should().Be("Market");
    }

    [Test]
    public void FromBybitOrder_ShouldHandleDifferentAvgPriceAndPrice()
    {
        // Arrange - Market order where avg price differs significantly from limit price
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
            Side = "Buy",
            OrderType = "Market",
            OrderStatus = "Filled",
            TimeInForce = "IOC",
            Qty = "2248",
            Price = "2.3943", // Original price
            CumExecQty = "2248",
            AvgPrice = "2.18309991", // Actual filled price (different from original)
            CumExecValue = "4907.6086",
            CreatedTime = "1749671285468",
            UpdatedTime = "1749672996947"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Price.Should().Be(2.3943m); // Original price
        futuresTrade.AvgPrice.Should().Be(2.18309991m); // Actual filled price
        futuresTrade.CumulativeQuoteQuantity.Should().Be(4907.6086m);
        
        // Verify the calculation makes sense: 2248 * 2.18309991 ≈ 4907.6086
        var expectedValue = 2248m * 2.18309991m;
        futuresTrade.CumulativeQuoteQuantity.Should().BeApproximately(expectedValue, 0.01m);
    }

    [Test]
    public void FromBybitOrder_ShouldHandleHigherFeesCorrectly()
    {
        // Arrange
        var bybitOrder = new BybitOrder
        {
            Symbol = "VIRTUALUSDT",
            OrderId = "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
            Side = "Buy",
            OrderType = "Market",
            OrderStatus = "Filled",
            TimeInForce = "IOC",
            Qty = "2248",
            Price = "2.3943",
            CumExecQty = "2248",
            AvgPrice = "2.18309991",
            CumExecValue = "4907.6086",
            CumExecFee = "2.69918474", // Higher fee amount
            CreatedTime = "1749671285468",
            UpdatedTime = "1749672996947"
        };

        // Act
        var futuresTrade = FuturesTrade.FromBybitOrder(bybitOrder);

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade.Fee.Should().Be(2.69918474m);
        futuresTrade.FeeAsset.Should().Be("USDT");
        
        // Verify fee percentage is reasonable (around 0.055% of trade value)
        var feePercentage = (futuresTrade.Fee / futuresTrade.CumulativeQuoteQuantity) * 100;
        feePercentage.Should().BeLessThan(1m); // Should be less than 1%
    }

    [Test]
    public void EndToEnd_MarketStopLossOrder_JsonDeserializationAndMapping_ShouldWork()
    {
        // Arrange
        var json = """
        {
            "retCode": 0,
            "retMsg": "OK",
            "result": {
                "list": [
                    {
                        "symbol": "VIRTUALUSDT",
                        "orderType": "Market",
                        "orderLinkId": "",
                        "slLimitPrice": "0",
                        "orderId": "44885ea7-1ddb-48ff-a7ee-dd9c4d05547a",
                        "cancelType": "UNKNOWN",
                        "avgPrice": "2.18309991",
                        "stopOrderType": "StopLoss",
                        "lastPriceOnCreated": "2.1775",
                        "orderStatus": "Filled",
                        "createType": "CreateByStopLoss",
                        "takeProfit": "",
                        "cumExecValue": "4907.6086",
                        "tpslMode": "Full",
                        "smpType": "None",
                        "triggerDirection": 1,
                        "blockTradeId": "",
                        "rejectReason": "EC_NoError",
                        "isLeverage": "",
                        "price": "2.3943",
                        "orderIv": "",
                        "createdTime": "1749671285468",
                        "tpTriggerBy": "",
                        "positionIdx": 0,
                        "timeInForce": "IOC",
                        "leavesValue": "0",
                        "updatedTime": "1749672996947",
                        "side": "Buy",
                        "smpGroup": 0,
                        "triggerPrice": "2.18",
                        "tpLimitPrice": "0",
                        "cumExecFee": "2.69918474",
                        "slTriggerBy": "",
                        "leavesQty": "0",
                        "closeOnTrigger": true,
                        "slippageToleranceType": "UNKNOWN",
                        "placeType": "",
                        "cumExecQty": "2248",
                        "reduceOnly": true,
                        "qty": "2248",
                        "stopLoss": "",
                        "smpOrderId": "",
                        "slippageTolerance": "0",
                        "triggerBy": "LastPrice",
                        "extraFees": ""
                    }
                ]
            },
            "time": 1749672996947
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<BybitApiResponse<BybitOrderHistoryData>>(json, _jsonOptions);
        var bybitOrder = response?.Result?.List?[0];
        var futuresTrade = bybitOrder != null ? FuturesTrade.FromBybitOrder(bybitOrder) : null;

        // Assert
        futuresTrade.Should().NotBeNull();
        futuresTrade!.Symbol.Should().Be("VIRTUALUSDT");
        futuresTrade.Side.Should().Be("BUY");
        futuresTrade.OrderType.Should().Be("Market");
        futuresTrade.Status.Should().Be("Filled");
        futuresTrade.TimeInForce.Should().Be("IOC");
        futuresTrade.Quantity.Should().Be(2248m);
        futuresTrade.Price.Should().Be(2.3943m);
        futuresTrade.ExecutedQuantity.Should().Be(2248m);
        futuresTrade.AvgPrice.Should().Be(2.18309991m);
        futuresTrade.CumulativeQuoteQuantity.Should().Be(4907.6086m);
        futuresTrade.Fee.Should().Be(2.69918474m);
        futuresTrade.Time.Should().Be(1749671285468L);
        futuresTrade.UpdateTime.Should().Be(1749672996947L);
        futuresTrade.Exchange.Should().Be("Bybit");
        futuresTrade.FeeAsset.Should().Be("USDT");
        futuresTrade.ReduceOnly.Should().Be(true);
        
        // Verify the calculated DateTime
        var expectedDateTime = DateTimeOffset.FromUnixTimeMilliseconds(1749671285468L).DateTime;
        futuresTrade.TradeDateTime.Should().Be(expectedDateTime);
        
        // Verify financial calculations
        var calculatedValue = futuresTrade.ExecutedQuantity * futuresTrade.AvgPrice;
        futuresTrade.CumulativeQuoteQuantity.Should().BeApproximately(calculatedValue, 0.01m);
    }
}
