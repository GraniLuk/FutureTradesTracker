using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.Extensions.Logging;
using CryptoPositionAnalysis.Models;
using CryptoPositionAnalysis.Utils;
using System.Drawing;

namespace CryptoPositionAnalysis.Services;

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
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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
            row++;
        }

        FormatWorksheet(worksheet, headers.Length, row - 1);
    }

    private void CreateFuturesTradesSheet(ExcelPackage package, List<FuturesTrade> trades)
    {
        var worksheet = package.Workbook.Worksheets.Add("Futures Trading History");
        
        // Headers
        var headers = new[] { "Exchange", "Symbol", "Order ID", "Side", "Type", "Quantity", "Price", "Avg Price", "Executed Qty", "Status", "Fee", "Fee Asset", "Realized PnL", "Trade Time" };
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
            worksheet.Cells[row, 5].Value = trade.OrderType;
            worksheet.Cells[row, 6].Value = trade.Quantity;
            worksheet.Cells[row, 7].Value = trade.Price;
            worksheet.Cells[row, 8].Value = trade.AvgPrice;
            worksheet.Cells[row, 9].Value = trade.ExecutedQuantity;
            worksheet.Cells[row, 10].Value = trade.Status;
            worksheet.Cells[row, 11].Value = trade.Fee;
            worksheet.Cells[row, 12].Value = trade.FeeAsset;
            worksheet.Cells[row, 13].Value = trade.RealizedPnl;
            worksheet.Cells[row, 14].Value = trade.TradeDateTime;
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
            worksheet.Cells[row, 3].Value = position.PositionSide;
            worksheet.Cells[row, 4].Value = position.PositionSize;
            worksheet.Cells[row, 5].Value = position.EntryPrice;
            worksheet.Cells[row, 6].Value = position.MarkPrice;
            worksheet.Cells[row, 7].Value = position.UnrealizedPnl;
            worksheet.Cells[row, 8].Value = position.Leverage;
            worksheet.Cells[row, 9].Value = position.IsolatedMargin;
            worksheet.Cells[row, 10].Value = position.LastUpdateTime;
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
            // Set solid pattern background (EPPlus version compatibility)
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
