using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.Extensions.Logging;
using FutureTradesTracker.Models;
using FutureTradesTracker.Utils;
using System.Drawing;

namespace FutureTradesTracker.Services;

public class ExcelExportService
{
    private readonly ExcelSettings _settings;
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(ExcelSettings settings, ILogger<ExcelExportService> logger)
    {
        _settings = settings;
        _logger = logger;
        
        // Set EPPlus license context for non-commercial use
        try
        {
            new EPPlusLicense().SetNonCommercialPersonal("FutureTradesTracker");
        }
        catch
        {
            // License context may already be set or using newer version
        }
    }

    public async Task<string> ExportPortfolioDataAsync(
        List<Balance> spotBalances,
        List<FuturesBalance> futuresBalances,
        List<Trade> spotTrades,
        List<FuturesTrade> futuresTrades,
        List<Position> positions)
    {
        try
        {
            // Ensure output directory exists
            if (!Directory.Exists(_settings.OutputDirectory))
            {
                Directory.CreateDirectory(_settings.OutputDirectory);
            }

            var timestamp = DateTime.UtcNow.ToString(_settings.DateFormat);
            var fileName = $"{_settings.FileNamePrefix}{timestamp}.xlsx";
            var filePath = Path.Combine(_settings.OutputDirectory, fileName);

            using var package = new ExcelPackage();
            
            var sheetsCreated = 0;

            // Create Spot Balances sheet
            if (spotBalances.Count > 0)
            {
                CreateSpotBalancesSheet(package, spotBalances);
                sheetsCreated++;
            }

            // Create Futures Balances sheet
            if (futuresBalances.Count > 0)
            {
                CreateFuturesBalancesSheet(package, futuresBalances);
                sheetsCreated++;
            }

            // Create Spot Trading History sheet
            if (spotTrades.Count > 0)
            {
                CreateSpotTradesSheet(package, spotTrades);
                sheetsCreated++;
            }

            // Create Futures Trading History sheet
            if (futuresTrades.Count > 0)
            {
                CreateFuturesTradesSheet(package, futuresTrades);
                sheetsCreated++;
            }

            // Create Current Positions sheet
            if (positions.Count > 0)
            {
                CreatePositionsSheet(package, positions);
                sheetsCreated++;
            }

            // Create Performance Analysis sheets (if we have futures trades)
            if (futuresTrades.Count > 0)
            {
                CreatePerformanceSummarySheet(package, futuresTrades);
                sheetsCreated++;
                
                CreateTradePerformanceSheet(package, futuresTrades);
                sheetsCreated++;
                
                CreateMonthlyPerformanceSheet(package, futuresTrades);
                sheetsCreated++;
                
                CreateSymbolPerformanceSheet(package, futuresTrades);
                sheetsCreated++;
            }

            // Create Summary sheet
            CreateSummarySheet(package, spotBalances, futuresBalances, positions);
            sheetsCreated++;

            await package.SaveAsAsync(new FileInfo(filePath));
            
            _logger.LogExcelExport(filePath, sheetsCreated);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting portfolio data to Excel");
            throw;
        }
    }

    private void CreateSpotBalancesSheet(ExcelPackage package, List<Balance> balances)
    {
        var worksheet = package.Workbook.Worksheets.Add("Spot Balances");
        
        // Headers
        var headers = new[] { "Exchange", "Asset", "Available", "Locked", "Total", "USD Value", "Timestamp" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var balance in balances.OrderBy(b => b.Exchange).ThenBy(b => b.Asset))
        {
            worksheet.Cells[row, 1].Value = balance.Exchange;
            worksheet.Cells[row, 2].Value = balance.Asset;
            worksheet.Cells[row, 3].Value = balance.Available;
            worksheet.Cells[row, 4].Value = balance.Locked;
            worksheet.Cells[row, 5].Value = balance.Total;
            worksheet.Cells[row, 6].Value = balance.UsdValue;
            worksheet.Cells[row, 7].Value = balance.Timestamp;
            worksheet.Cells[row, 7].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreateFuturesBalancesSheet(ExcelPackage package, List<FuturesBalance> balances)
    {
        var worksheet = package.Workbook.Worksheets.Add("Futures Balances");
        
        // Headers
        var headers = new[] { "Exchange", "Asset", "Balance", "Available", "Cross PnL", "Max Withdraw", "Timestamp" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var balance in balances.OrderBy(b => b.Exchange).ThenBy(b => b.Asset))
        {
            worksheet.Cells[row, 1].Value = balance.Exchange;
            worksheet.Cells[row, 2].Value = balance.Asset;
            worksheet.Cells[row, 3].Value = balance.Balance;
            worksheet.Cells[row, 4].Value = balance.AvailableBalance;
            worksheet.Cells[row, 5].Value = balance.CrossUnrealizedPnl;
            worksheet.Cells[row, 6].Value = balance.MaxWithdrawAmount;
            worksheet.Cells[row, 7].Value = balance.Timestamp;
            worksheet.Cells[row, 7].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreateSpotTradesSheet(ExcelPackage package, List<Trade> trades)
    {
        var worksheet = package.Workbook.Worksheets.Add("Spot Trading History");
        
        // Headers
        var headers = new[] { "Exchange", "Symbol", "Order ID", "Trade ID", "Side", "Type", "Quantity", "Price", "Executed Qty", "Status", "Fee", "Fee Asset", "Trade Time" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var trade in trades.OrderByDescending(t => t.TradeTime))
        {
            worksheet.Cells[row, 1].Value = trade.Exchange;
            worksheet.Cells[row, 2].Value = trade.Symbol;
            worksheet.Cells[row, 3].Value = trade.OrderId;
            worksheet.Cells[row, 4].Value = trade.TradeId;
            worksheet.Cells[row, 5].Value = trade.Side;
            worksheet.Cells[row, 6].Value = trade.OrderType;
            worksheet.Cells[row, 7].Value = trade.Quantity;
            worksheet.Cells[row, 8].Value = trade.Price;
            worksheet.Cells[row, 9].Value = trade.ExecutedQuantity;
            worksheet.Cells[row, 10].Value = trade.Status;
            worksheet.Cells[row, 11].Value = trade.Fee;
            worksheet.Cells[row, 12].Value = trade.FeeAsset;
            worksheet.Cells[row, 13].Value = trade.TradeDateTime;
            worksheet.Cells[row, 13].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreateFuturesTradesSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheet = package.Workbook.Worksheets.Add("Futures Trading History");
        
        // Headers - Enhanced with new fields from BingX API
        var headers = new[] { 
            "Exchange", "Symbol", "Order ID", "Side", "Position Side", "Type", "Quantity", 
            "Price", "Avg Price", "Executed Qty", "Stop Price", "Status", "Leverage", 
            "Fee", "Fee Asset", "Realized PnL", "Reduce Only", "Working Type", "Trade Time" 
        };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var trade in trades.OrderByDescending(t => t.Time))
        {
            worksheet.Cells[row, 1].Value = trade.Exchange;
            worksheet.Cells[row, 2].Value = trade.Symbol;
            worksheet.Cells[row, 3].Value = trade.OrderId;
            worksheet.Cells[row, 4].Value = trade.Side;
            worksheet.Cells[row, 5].Value = trade.PositionSide.ToString().ToUpper(); // New field
            worksheet.Cells[row, 6].Value = trade.OrderType;
            worksheet.Cells[row, 7].Value = trade.Quantity;
            worksheet.Cells[row, 8].Value = trade.Price;
            worksheet.Cells[row, 9].Value = trade.AvgPrice;
            worksheet.Cells[row, 10].Value = trade.ExecutedQuantity;
            worksheet.Cells[row, 11].Value = trade.StopPrice ?? 0; // New field
            worksheet.Cells[row, 12].Value = trade.Status;
            worksheet.Cells[row, 13].Value = trade.Leverage ?? ""; // New field
            worksheet.Cells[row, 14].Value = trade.Fee;
            worksheet.Cells[row, 15].Value = trade.FeeAsset;
            worksheet.Cells[row, 16].Value = trade.RealizedPnl;
            worksheet.Cells[row, 17].Value = trade.ReduceOnly?.ToString() ?? ""; // New field
            worksheet.Cells[row, 18].Value = trade.WorkingType ?? ""; // New field
            worksheet.Cells[row, 19].Value = trade.TradeDateTime;
            worksheet.Cells[row, 19].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
            
            // Color-code PnL
            var pnlCell = worksheet.Cells[row, 16];
            if (trade.RealizedPnl > 0)
            {
                pnlCell.Style.Font.Color.SetColor(Color.Green);
            }
            else if (trade.RealizedPnl < 0)
            {
                pnlCell.Style.Font.Color.SetColor(Color.Red);
            }
            
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreatePositionsSheet(ExcelPackage package, List<Position> positions)
    {
        var worksheet = package.Workbook.Worksheets.Add("Current Positions");
        
        // Headers
        var headers = new[] { "Exchange", "Symbol", "Side", "Size", "Entry Price", "Mark Price", "Unrealized PnL", "Leverage", "Margin", "Last Update" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var position in positions.OrderBy(p => p.Exchange).ThenBy(p => p.Symbol))
        {
            worksheet.Cells[row, 1].Value = position.Exchange;
            worksheet.Cells[row, 2].Value = position.Symbol;
            worksheet.Cells[row, 3].Value = position.PositionSide.ToString().ToUpper();
            worksheet.Cells[row, 4].Value = position.PositionSize;
            worksheet.Cells[row, 5].Value = position.EntryPrice;
            worksheet.Cells[row, 6].Value = position.MarkPrice;
            worksheet.Cells[row, 7].Value = position.UnrealizedPnl;
            worksheet.Cells[row, 8].Value = position.Leverage;
            worksheet.Cells[row, 9].Value = position.IsolatedMargin;
            worksheet.Cells[row, 10].Value = position.LastUpdateTime;
            worksheet.Cells[row, 10].Style.Numberformat.Format = "dd/mm/yyyy";
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);

        // Color-code PnL
        if (row > 2)
        {
            for (int r = 2; r < row; r++)
            {
                var pnlCell = worksheet.Cells[r, 7];
                if (pnlCell.Value != null && decimal.TryParse(pnlCell.Value.ToString(), out var pnl))
                {
                    if (pnl > 0)
                    {
                        pnlCell.Style.Font.Color.SetColor(Color.Green);
                    }
                    else if (pnl < 0)
                    {
                        pnlCell.Style.Font.Color.SetColor(Color.Red);
                    }
                }
            }
        }
    }

    private void CreatePerformanceSummarySheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheet = package.Workbook.Worksheets.Add("Performance Summary");
        
        var row = 1;

        // Title
        worksheet.Cells[row, 1].Value = "Trading Performance Summary";
        worksheet.Cells[row, 1].Style.Font.Size = 16;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row += 2;

        // Overall Statistics
        worksheet.Cells[row, 1].Value = "Overall Performance";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        var totalPnl = trades.Sum(t => t.RealizedPnl);
        var totalTrades = trades.Count;
        var winningTrades = trades.Count(t => t.RealizedPnl > 0);
        var losingTrades = trades.Count(t => t.RealizedPnl < 0);
        var winRate = totalTrades > 0 ? (double)winningTrades / totalTrades * 100 : 0;
        var avgPnl = totalTrades > 0 ? totalPnl / totalTrades : 0;
        var avgWin = winningTrades > 0 ? trades.Where(t => t.RealizedPnl > 0).Average(t => t.RealizedPnl) : 0;
        var avgLoss = losingTrades > 0 ? trades.Where(t => t.RealizedPnl < 0).Average(t => t.RealizedPnl) : 0;
        var maxWin = trades.Any() ? trades.Max(t => t.RealizedPnl) : 0;
        var maxLoss = trades.Any() ? trades.Min(t => t.RealizedPnl) : 0;

        worksheet.Cells[row, 1].Value = "Total P&L:";
        worksheet.Cells[row, 2].Value = totalPnl;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(totalPnl >= 0 ? Color.Green : Color.Red);
        row++;

        worksheet.Cells[row, 1].Value = "Total Trades:";
        worksheet.Cells[row, 2].Value = totalTrades;
        row++;

        worksheet.Cells[row, 1].Value = "Winning Trades:";
        worksheet.Cells[row, 2].Value = winningTrades;
        row++;

        worksheet.Cells[row, 1].Value = "Losing Trades:";
        worksheet.Cells[row, 2].Value = losingTrades;
        row++;

        worksheet.Cells[row, 1].Value = "Win Rate:";
        worksheet.Cells[row, 2].Value = $"{winRate:F1}%";
        row++;

        worksheet.Cells[row, 1].Value = "Average P&L per Trade:";
        worksheet.Cells[row, 2].Value = avgPnl;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(avgPnl >= 0 ? Color.Green : Color.Red);
        row++;

        worksheet.Cells[row, 1].Value = "Average Win:";
        worksheet.Cells[row, 2].Value = avgWin;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Green);
        row++;

        worksheet.Cells[row, 1].Value = "Average Loss:";
        worksheet.Cells[row, 2].Value = avgLoss;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
        row++;

        worksheet.Cells[row, 1].Value = "Best Trade:";
        worksheet.Cells[row, 2].Value = maxWin;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Green);
        row++;

        worksheet.Cells[row, 1].Value = "Worst Trade:";
        worksheet.Cells[row, 2].Value = maxLoss;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
        row += 2;

        // Risk Metrics
        worksheet.Cells[row, 1].Value = "Risk Metrics";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        var profitFactor = Math.Abs(avgLoss) > 0 ? avgWin / Math.Abs(avgLoss) : 0;
        worksheet.Cells[row, 1].Value = "Profit Factor:";
        worksheet.Cells[row, 2].Value = profitFactor;
        row++;

        var totalVolume = trades.Sum(t => t.CumulativeQuoteQuantity);
        worksheet.Cells[row, 1].Value = "Total Volume:";
        worksheet.Cells[row, 2].Value = totalVolume;
        row++;

        var totalFees = trades.Sum(t => t.Fee);
        worksheet.Cells[row, 1].Value = "Total Fees:";
        worksheet.Cells[row, 2].Value = totalFees;
        row++;

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }

    private void CreateTradePerformanceSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheet = package.Workbook.Worksheets.Add("Trade Performance");
        
        // Headers - Enhanced with position side
        var headers = new[] { "Date", "Symbol", "Side", "Position Side", "Type", "Quantity", "Entry Price", "Exit Price", "Realized P&L", "Fee", "Net P&L", "ROE %", "Leverage", "Duration" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var trade in trades.OrderByDescending(t => t.Time))
        {
            var roe = trade.Price > 0 ? ((trade.AvgPrice - trade.Price) / trade.Price * 100 * (trade.Side == "BUY" ? 1 : -1)) : 0;
            var netPnl = trade.RealizedPnl - trade.Fee;
            
            worksheet.Cells[row, 1].Value = trade.TradeDateTime;
            worksheet.Cells[row, 1].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
            worksheet.Cells[row, 2].Value = trade.Symbol;
            worksheet.Cells[row, 3].Value = trade.Side;
            worksheet.Cells[row, 4].Value = trade.PositionSide.ToString().ToUpper();
            worksheet.Cells[row, 5].Value = trade.OrderType;
            worksheet.Cells[row, 6].Value = trade.ExecutedQuantity;
            worksheet.Cells[row, 7].Value = trade.Price;
            worksheet.Cells[row, 8].Value = trade.AvgPrice;
            worksheet.Cells[row, 9].Value = trade.RealizedPnl;
            worksheet.Cells[row, 10].Value = trade.Fee;
            worksheet.Cells[row, 11].Value = netPnl;
            worksheet.Cells[row, 12].Value = roe;
            worksheet.Cells[row, 13].Value = trade.Leverage ?? "";
            worksheet.Cells[row, 14].Value = ""; // Duration calculation would need order pairing
            
            // Color-code P&L
            if (trade.RealizedPnl > 0)
            {
                worksheet.Cells[row, 9].Style.Font.Color.SetColor(Color.Green);
                worksheet.Cells[row, 11].Style.Font.Color.SetColor(netPnl > 0 ? Color.Green : Color.Red);
            }
            else if (trade.RealizedPnl < 0)
            {
                worksheet.Cells[row, 9].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[row, 11].Style.Font.Color.SetColor(Color.Red);
            }
            
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreateMonthlyPerformanceSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheet = package.Workbook.Worksheets.Add("Monthly Performance");
        
        // Group trades by month
        var monthlyData = trades
            .GroupBy(t => new { t.TradeDateTime.Year, t.TradeDateTime.Month })
            .Select(g => new
            {
                Period = $"{g.Key.Year}-{g.Key.Month:00}",
                TotalPnl = g.Sum(t => t.RealizedPnl),
                TotalTrades = g.Count(),
                WinningTrades = g.Count(t => t.RealizedPnl > 0),
                LosingTrades = g.Count(t => t.RealizedPnl < 0),
                TotalVolume = g.Sum(t => t.CumulativeQuoteQuantity),
                TotalFees = g.Sum(t => t.Fee)
            })
            .OrderBy(x => x.Period)
            .ToList();

        // Headers
        var headers = new[] { "Month", "Total P&L", "Trades", "Wins", "Losses", "Win Rate %", "Volume", "Fees", "Net P&L" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var monthData in monthlyData)
        {
            var winRate = monthData.TotalTrades > 0 ? (double)monthData.WinningTrades / monthData.TotalTrades * 100 : 0;
            var netPnl = monthData.TotalPnl - monthData.TotalFees;
            
            worksheet.Cells[row, 1].Value = monthData.Period;
            worksheet.Cells[row, 2].Value = monthData.TotalPnl;
            worksheet.Cells[row, 3].Value = monthData.TotalTrades;
            worksheet.Cells[row, 4].Value = monthData.WinningTrades;
            worksheet.Cells[row, 5].Value = monthData.LosingTrades;
            worksheet.Cells[row, 6].Value = winRate;
            worksheet.Cells[row, 7].Value = monthData.TotalVolume;
            worksheet.Cells[row, 8].Value = monthData.TotalFees;
            worksheet.Cells[row, 9].Value = netPnl;
            
            // Color-code P&L
            worksheet.Cells[row, 2].Style.Font.Color.SetColor(monthData.TotalPnl >= 0 ? Color.Green : Color.Red);
            worksheet.Cells[row, 9].Style.Font.Color.SetColor(netPnl >= 0 ? Color.Green : Color.Red);
            
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreateSymbolPerformanceSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheet = package.Workbook.Worksheets.Add("Symbol Performance");
        
        // Group trades by symbol
        var symbolData = trades
            .GroupBy(t => t.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                TotalPnl = g.Sum(t => t.RealizedPnl),
                TotalTrades = g.Count(),
                WinningTrades = g.Count(t => t.RealizedPnl > 0),
                LosingTrades = g.Count(t => t.RealizedPnl < 0),
                TotalVolume = g.Sum(t => t.CumulativeQuoteQuantity),
                TotalFees = g.Sum(t => t.Fee),
                AvgPnl = g.Average(t => t.RealizedPnl),
                MaxWin = g.Max(t => t.RealizedPnl),
                MaxLoss = g.Min(t => t.RealizedPnl)
            })
            .OrderByDescending(x => x.TotalPnl)
            .ToList();

        // Headers
        var headers = new[] { "Symbol", "Total P&L", "Trades", "Wins", "Losses", "Win Rate %", "Avg P&L", "Best Trade", "Worst Trade", "Volume", "Fees" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Data
        var row = 2;
        foreach (var symbol in symbolData)
        {
            var winRate = symbol.TotalTrades > 0 ? (double)symbol.WinningTrades / symbol.TotalTrades * 100 : 0;
            
            worksheet.Cells[row, 1].Value = symbol.Symbol;
            worksheet.Cells[row, 2].Value = symbol.TotalPnl;
            worksheet.Cells[row, 3].Value = symbol.TotalTrades;
            worksheet.Cells[row, 4].Value = symbol.WinningTrades;
            worksheet.Cells[row, 5].Value = symbol.LosingTrades;
            worksheet.Cells[row, 6].Value = winRate;
            worksheet.Cells[row, 7].Value = symbol.AvgPnl;
            worksheet.Cells[row, 8].Value = symbol.MaxWin;
            worksheet.Cells[row, 9].Value = symbol.MaxLoss;
            worksheet.Cells[row, 10].Value = symbol.TotalVolume;
            worksheet.Cells[row, 11].Value = symbol.TotalFees;
            
            // Color-code P&L
            worksheet.Cells[row, 2].Style.Font.Color.SetColor(symbol.TotalPnl >= 0 ? Color.Green : Color.Red);
            worksheet.Cells[row, 7].Style.Font.Color.SetColor(symbol.AvgPnl >= 0 ? Color.Green : Color.Red);
            worksheet.Cells[row, 8].Style.Font.Color.SetColor(Color.Green);
            worksheet.Cells[row, 9].Style.Font.Color.SetColor(Color.Red);
            
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreateSummarySheet(ExcelPackage package, List<Balance> spotBalances, List<FuturesBalance> futuresBalances, List<Position> positions)
    {
        var worksheet = package.Workbook.Worksheets.Add("Portfolio Summary");
        
        var row = 1;

        // Report metadata
        worksheet.Cells[row, 1].Value = "Portfolio Analysis Report";
        worksheet.Cells[row, 1].Style.Font.Size = 16;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row += 2;

        worksheet.Cells[row, 1].Value = "Generated:";
        worksheet.Cells[row, 2].Value = DateTime.UtcNow;
        worksheet.Cells[row, 2].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
        row += 2;

        // Summary statistics
        worksheet.Cells[row, 1].Value = "Summary Statistics";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        worksheet.Cells[row, 1].Value = "Total Spot Assets:";
        worksheet.Cells[row, 2].Value = spotBalances.Count(b => b.Total > 0);
        row++;

        worksheet.Cells[row, 1].Value = "Total Futures Assets:";
        worksheet.Cells[row, 2].Value = futuresBalances.Count(b => b.Balance > 0);
        row++;

        worksheet.Cells[row, 1].Value = "Active Positions:";
        worksheet.Cells[row, 2].Value = positions.Count;
        row++;

        if (positions.Count > 0)
        {
            var totalUnrealizedPnl = positions.Sum(p => p.UnrealizedPnl);
            worksheet.Cells[row, 1].Value = "Total Unrealized PnL:";
            worksheet.Cells[row, 2].Value = totalUnrealizedPnl;
            
            if (totalUnrealizedPnl > 0)
                worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Green);
            else if (totalUnrealizedPnl < 0)
                worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
            
            row++;
        }

        row += 2;

        // Exchange breakdown
        worksheet.Cells[row, 1].Value = "Exchange Breakdown";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        var exchangeGroups = spotBalances.Concat(futuresBalances.Select(f => new Balance { Exchange = f.Exchange }))
            .GroupBy(b => b.Exchange)
            .Where(g => !string.IsNullOrEmpty(g.Key));

        foreach (var group in exchangeGroups)
        {
            worksheet.Cells[row, 1].Value = $"{group.Key}:";
            worksheet.Cells[row, 2].Value = $"{group.Count()} assets";
            row++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }

    private void FormatWorksheet(ExcelWorksheet worksheet, int columns, int rows)
    {
        // Format headers
        using (var range = worksheet.Cells[1, 1, 1, columns])
        {
            range.Style.Font.Bold = true;
            // Set fill pattern first, then background color (EPPlus requirement)
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        // Format data range
        if (rows > 1)
        {
            using (var range = worksheet.Cells[2, 1, rows, columns])
            {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        // Freeze header row
        worksheet.View.FreezePanes(2, 1);
    }
}
