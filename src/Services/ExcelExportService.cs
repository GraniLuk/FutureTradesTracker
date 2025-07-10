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

            var filePath = Path.Combine(_settings.OutputDirectory, _settings.FileName);
            
            ExcelPackage package;
            bool isNewFile = !File.Exists(filePath);

            if (isNewFile)
            {
                package = new ExcelPackage();
                _logger.LogInformation("Creating new Excel file: {FilePath}", filePath);
            }
            else
            {
                package = new ExcelPackage(new FileInfo(filePath));
                _logger.LogInformation("Updating existing Excel file: {FilePath}", filePath);
            }

            using (package)
            {
                var sheetsCreated = 0;
                var timestamp = DateTime.UtcNow;

                // Update/Create Spot Balances sheet (always add new snapshot)
                if (spotBalances.Count > 0)
                {
                    AppendSpotBalancesSnapshot(package, spotBalances, timestamp);
                    sheetsCreated++;
                }

                // Update/Create Futures Balances sheet (always add new snapshot)
                if (futuresBalances.Count > 0)
                {
                    AppendFuturesBalancesSnapshot(package, futuresBalances, timestamp);
                    sheetsCreated++;
                }

                // Update/Create Spot Trading History sheet (add only new trades)
                if (spotTrades.Count > 0)
                {
                    AppendNewSpotTrades(package, spotTrades);
                    sheetsCreated++;
                }

                // Update/Create Futures Trading History sheet (add only new trades)
                if (futuresTrades.Count > 0)
                {
                    AppendNewFuturesTrades(package, futuresTrades);
                    sheetsCreated++;
                }

                // Update/Create Current Positions sheet (always add new snapshot)
                if (positions.Count > 0)
                {
                    AppendPositionsSnapshot(package, positions, timestamp);
                    sheetsCreated++;
                }

                // Create/Update Performance Analysis sheets (if we have futures trades)
                if (futuresTrades.Count > 0)
                {
                    CreateOrUpdatePerformanceSummarySheet(package, futuresTrades);
                    sheetsCreated++;
                    
                    CreateOrUpdateTradePerformanceSheet(package, futuresTrades);
                    sheetsCreated++;
                    
                    CreateOrUpdateMonthlyPerformanceSheet(package, futuresTrades);
                    sheetsCreated++;
                    
                    CreateOrUpdateSymbolPerformanceSheet(package, futuresTrades);
                    sheetsCreated++;
                }

                // Create/Update Summary sheet
                CreateOrUpdateSummarySheet(package, spotBalances, futuresBalances, positions);
                sheetsCreated++;

                await package.SaveAsAsync(new FileInfo(filePath));
                
                _logger.LogExcelExport(filePath, sheetsCreated);
                return filePath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting portfolio data to Excel");
            throw;
        }
    }

    private void AppendSpotBalancesSnapshot(ExcelPackage package, List<Balance> balances, DateTime timestamp)
    {
        var worksheetName = "Spot Balances";
        var worksheet = package.Workbook.Worksheets[worksheetName] ?? package.Workbook.Worksheets.Add(worksheetName);
        
        // Headers (only if new worksheet)
        if (worksheet.Dimension == null)
        {
            var headers = new[] { "Timestamp", "Exchange", "Asset", "Available", "Locked", "Total", "USD Value" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
            FormatHeaders(worksheet, headers.Length);
        }

        // Find the next available row
        var nextRow = worksheet.Dimension?.End.Row + 1 ?? 2;

        // Add new snapshot data
        foreach (var balance in balances.OrderBy(b => b.Exchange).ThenBy(b => b.Asset))
        {
            worksheet.Cells[nextRow, 1].Value = timestamp;
            worksheet.Cells[nextRow, 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cells[nextRow, 2].Value = balance.Exchange;
            worksheet.Cells[nextRow, 3].Value = balance.Asset;
            worksheet.Cells[nextRow, 4].Value = balance.Available;
            worksheet.Cells[nextRow, 5].Value = balance.Locked;
            worksheet.Cells[nextRow, 6].Value = balance.Total;
            worksheet.Cells[nextRow, 7].Value = balance.UsdValue;
            nextRow++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }

    private void AppendFuturesBalancesSnapshot(ExcelPackage package, List<FuturesBalance> balances, DateTime timestamp)
    {
        var worksheetName = "Futures Balances";
        var worksheet = package.Workbook.Worksheets[worksheetName] ?? package.Workbook.Worksheets.Add(worksheetName);
        
        // Headers (only if new worksheet)
        if (worksheet.Dimension == null)
        {
            var headers = new[] { "Timestamp", "Exchange", "Asset", "Balance", "Available", "Cross PnL", "Max Withdraw" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
            FormatHeaders(worksheet, headers.Length);
        }

        // Find the next available row
        var nextRow = worksheet.Dimension?.End.Row + 1 ?? 2;

        // Add new snapshot data
        foreach (var balance in balances.OrderBy(b => b.Exchange).ThenBy(b => b.Asset))
        {
            worksheet.Cells[nextRow, 1].Value = timestamp;
            worksheet.Cells[nextRow, 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cells[nextRow, 2].Value = balance.Exchange;
            worksheet.Cells[nextRow, 3].Value = balance.Asset;
            worksheet.Cells[nextRow, 4].Value = balance.Balance;
            worksheet.Cells[nextRow, 5].Value = balance.AvailableBalance;
            worksheet.Cells[nextRow, 6].Value = balance.CrossUnrealizedPnl;
            worksheet.Cells[nextRow, 7].Value = balance.MaxWithdrawAmount;
            nextRow++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }

    private void AppendNewSpotTrades(ExcelPackage package, List<Trade> trades)
    {
        var worksheetName = "Spot Trading History";
        var worksheet = package.Workbook.Worksheets[worksheetName] ?? package.Workbook.Worksheets.Add(worksheetName);
        
        // Headers (only if new worksheet)
        if (worksheet.Dimension == null)
        {
            var headers = new[] { "Exchange", "Symbol", "Order ID", "Trade ID", "Side", "Type", "Quantity", "Price", "Executed Qty", "Status", "Fee", "Fee Asset", "Trade Time" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
            FormatHeaders(worksheet, headers.Length);
        }

        // Get existing trade IDs to avoid duplicates
        var existingTradeIds = new HashSet<long>();
        if (worksheet.Dimension != null)
        {
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var tradeIdValue = worksheet.Cells[row, 4].Value?.ToString();
                if (!string.IsNullOrEmpty(tradeIdValue) && long.TryParse(tradeIdValue, out var tradeId))
                {
                    existingTradeIds.Add(tradeId);
                }
            }
        }

        // Find the next available row
        var nextRow = worksheet.Dimension?.End.Row + 1 ?? 2;

        // Add only new trades
        foreach (var trade in trades.Where(t => !existingTradeIds.Contains(t.TradeId)).OrderByDescending(t => t.TradeTime))
        {
            worksheet.Cells[nextRow, 1].Value = trade.Exchange;
            worksheet.Cells[nextRow, 2].Value = trade.Symbol;
            worksheet.Cells[nextRow, 3].Value = trade.OrderId;
            worksheet.Cells[nextRow, 4].Value = trade.TradeId;
            worksheet.Cells[nextRow, 5].Value = trade.Side;
            worksheet.Cells[nextRow, 6].Value = trade.OrderType;
            worksheet.Cells[nextRow, 7].Value = trade.Quantity;
            worksheet.Cells[nextRow, 8].Value = trade.Price;
            worksheet.Cells[nextRow, 9].Value = trade.ExecutedQuantity;
            worksheet.Cells[nextRow, 10].Value = trade.Status;
            worksheet.Cells[nextRow, 11].Value = trade.Fee;
            worksheet.Cells[nextRow, 12].Value = trade.FeeAsset;
            worksheet.Cells[nextRow, 13].Value = trade.TradeDateTime;
            worksheet.Cells[nextRow, 13].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            nextRow++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }

    private void AppendNewFuturesTrades(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheetName = "Futures Trading History";
        var worksheet = package.Workbook.Worksheets[worksheetName] ?? package.Workbook.Worksheets.Add(worksheetName);
        
        // Headers (only if new worksheet)
        if (worksheet.Dimension == null)
        {
            var headers = new[] { 
                "Exchange", "Symbol", "Order ID", "Side", "Position Side", "Type", "Quantity", 
                "Price", "Avg Price", "Executed Qty", "Stop Price", "Status", "Leverage", 
                "Fee", "Fee Asset", "Realized PnL", "Reduce Only", "Working Type", "Trade Time" 
            };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
            FormatHeaders(worksheet, headers.Length);
        }

        // Get existing trade IDs to avoid duplicates
        var existingTradeIds = new HashSet<string>();
        if (worksheet.Dimension != null)
        {
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var tradeId = worksheet.Cells[row, 3].Value?.ToString(); // Order ID column
                if (!string.IsNullOrEmpty(tradeId))
                {
                    existingTradeIds.Add(tradeId);
                }
            }
        }

        // Find the next available row
        var nextRow = worksheet.Dimension?.End.Row + 1 ?? 2;

        // Add only new trades
        foreach (var trade in trades.Where(t => !existingTradeIds.Contains(t.OrderId)).OrderByDescending(t => t.Time))
        {
            worksheet.Cells[nextRow, 1].Value = trade.Exchange;
            worksheet.Cells[nextRow, 2].Value = trade.Symbol;
            worksheet.Cells[nextRow, 3].Value = trade.OrderId;
            worksheet.Cells[nextRow, 4].Value = trade.Side;
            worksheet.Cells[nextRow, 5].Value = trade.PositionSide.ToString().ToUpper();
            worksheet.Cells[nextRow, 6].Value = trade.OrderType;
            worksheet.Cells[nextRow, 7].Value = trade.Quantity;
            worksheet.Cells[nextRow, 8].Value = trade.Price;
            worksheet.Cells[nextRow, 9].Value = trade.AvgPrice;
            worksheet.Cells[nextRow, 10].Value = trade.ExecutedQuantity;
            worksheet.Cells[nextRow, 11].Value = trade.StopPrice ?? 0;
            worksheet.Cells[nextRow, 12].Value = trade.Status;
            worksheet.Cells[nextRow, 13].Value = trade.Leverage ?? "";
            worksheet.Cells[nextRow, 14].Value = trade.Fee;
            worksheet.Cells[nextRow, 15].Value = trade.FeeAsset;
            worksheet.Cells[nextRow, 16].Value = trade.RealizedPnl;
            worksheet.Cells[nextRow, 17].Value = trade.ReduceOnly?.ToString() ?? "";
            worksheet.Cells[nextRow, 18].Value = trade.WorkingType ?? "";
            worksheet.Cells[nextRow, 19].Value = trade.TradeDateTime;
            worksheet.Cells[nextRow, 19].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            
            // Color-code PnL
            var pnlCell = worksheet.Cells[nextRow, 16];
            if (trade.RealizedPnl > 0)
            {
                pnlCell.Style.Font.Color.SetColor(Color.Green);
            }
            else if (trade.RealizedPnl < 0)
            {
                pnlCell.Style.Font.Color.SetColor(Color.Red);
            }
            
            nextRow++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }

    private void AppendPositionsSnapshot(ExcelPackage package, List<Position> positions, DateTime timestamp)
    {
        var worksheetName = "Current Positions";
        var worksheet = package.Workbook.Worksheets[worksheetName] ?? package.Workbook.Worksheets.Add(worksheetName);
        
        // Headers (only if new worksheet)
        if (worksheet.Dimension == null)
        {
            var headers = new[] { "Timestamp", "Exchange", "Symbol", "Side", "Size", "Entry Price", "Mark Price", "Unrealized PnL", "Leverage", "Margin" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
            FormatHeaders(worksheet, headers.Length);
        }

        // Find the next available row
        var nextRow = worksheet.Dimension?.End.Row + 1 ?? 2;

        // Add new snapshot data
        foreach (var position in positions.OrderBy(p => p.Exchange).ThenBy(p => p.Symbol))
        {
            worksheet.Cells[nextRow, 1].Value = timestamp;
            worksheet.Cells[nextRow, 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cells[nextRow, 2].Value = position.Exchange;
            worksheet.Cells[nextRow, 3].Value = position.Symbol;
            worksheet.Cells[nextRow, 4].Value = position.PositionSide.ToString().ToUpper();
            worksheet.Cells[nextRow, 5].Value = position.PositionSize;
            worksheet.Cells[nextRow, 6].Value = position.EntryPrice;
            worksheet.Cells[nextRow, 7].Value = position.MarkPrice;
            worksheet.Cells[nextRow, 8].Value = position.UnrealizedPnl;
            worksheet.Cells[nextRow, 9].Value = position.Leverage;
            worksheet.Cells[nextRow, 10].Value = position.IsolatedMargin;
            
            // Color-code PnL
            var pnlCell = worksheet.Cells[nextRow, 8];
            if (position.UnrealizedPnl > 0)
            {
                pnlCell.Style.Font.Color.SetColor(Color.Green);
            }
            else if (position.UnrealizedPnl < 0)
            {
                pnlCell.Style.Font.Color.SetColor(Color.Red);
            }
            
            nextRow++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }

    private void CreateOrUpdatePerformanceSummarySheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheetName = "Performance Summary";
        var worksheet = package.Workbook.Worksheets[worksheetName];
        
        // Always recreate this sheet with latest data
        if (worksheet != null)
        {
            package.Workbook.Worksheets.Delete(worksheet);
        }
        
        worksheet = package.Workbook.Worksheets.Add(worksheetName);
        
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

    private void CreateOrUpdateTradePerformanceSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheetName = "Trade Performance";
        var worksheet = package.Workbook.Worksheets[worksheetName];
        
        // Always recreate this sheet with latest data
        if (worksheet != null)
        {
            package.Workbook.Worksheets.Delete(worksheet);
        }
        
        worksheet = package.Workbook.Worksheets.Add(worksheetName);
        
        // Headers - Enhanced with position side
        var headers = new[] { "Date", "Symbol", "Side", "Position Side", "Type", "Quantity", "Entry Price", "Exit Price", "Realized P&L", "Fee", "Net P&L", "ROE %", "Leverage", "Duration" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }
        FormatHeaders(worksheet, headers.Length);

        // Data
        var row = 2;
        foreach (var trade in trades.OrderByDescending(t => t.Time))
        {
            var roe = trade.Price > 0 ? ((trade.AvgPrice - trade.Price) / trade.Price * 100 * (trade.Side == "BUY" ? 1 : -1)) : 0;
            var netPnl = trade.RealizedPnl - trade.Fee;
            
            worksheet.Cells[row, 1].Value = trade.TradeDateTime;
            worksheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
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

        worksheet.Cells.AutoFitColumns();
    }

    private void CreateOrUpdateMonthlyPerformanceSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheetName = "Monthly Performance";
        var worksheet = package.Workbook.Worksheets[worksheetName];
        
        // Always recreate this sheet with latest data
        if (worksheet != null)
        {
            package.Workbook.Worksheets.Delete(worksheet);
        }
        
        worksheet = package.Workbook.Worksheets.Add(worksheetName);
        
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
        FormatHeaders(worksheet, headers.Length);

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

        worksheet.Cells.AutoFitColumns();
    }

    private void CreateOrUpdateSymbolPerformanceSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheetName = "Symbol Performance";
        var worksheet = package.Workbook.Worksheets[worksheetName];
        
        // Always recreate this sheet with latest data
        if (worksheet != null)
        {
            package.Workbook.Worksheets.Delete(worksheet);
        }
        
        worksheet = package.Workbook.Worksheets.Add(worksheetName);
        
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
        FormatHeaders(worksheet, headers.Length);

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

        worksheet.Cells.AutoFitColumns();
    }

    private void CreateOrUpdateSummarySheet(ExcelPackage package, List<Balance> spotBalances, List<FuturesBalance> futuresBalances, List<Position> positions)
    {
        var worksheetName = "Portfolio Summary";
        var worksheet = package.Workbook.Worksheets[worksheetName];
        
        // Always recreate this sheet with latest data
        if (worksheet != null)
        {
            package.Workbook.Worksheets.Delete(worksheet);
        }
        
        worksheet = package.Workbook.Worksheets.Add(worksheetName);
        
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

    private void FormatHeaders(ExcelWorksheet worksheet, int columns)
    {
        using (var range = worksheet.Cells[1, 1, 1, columns])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        // Freeze header row
        worksheet.View.FreezePanes(2, 1);
    }
}
