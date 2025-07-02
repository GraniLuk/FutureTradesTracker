# Sample Application Output

This document shows what to expect when running the Crypto Position Analysis application.

## Successful Run with Configured APIs

```
info: CryptoPositionAnalysis.Program[0]
      === Crypto Position Analysis Started ===
info: CryptoPositionAnalysis.Program[0]
      Timestamp: 07/02/2025 13:49:04
info: CryptoPositionAnalysis.Program[0]
      Processing BingX exchange data...
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Making GET request to BingX API: /openApi/spot/v1/account/balance
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Successfully retrieved data from BingX API: /openApi/spot/v1/account/balance, Items: 12
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Making GET request to BingX API: /openApi/swap/v2/user/balance
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Successfully retrieved data from BingX API: /openApi/swap/v2/user/balance, Items: 5
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Making GET request to BingX API: /openApi/spot/v1/trade/historyOrders
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Successfully retrieved data from BingX API: /openApi/spot/v1/trade/historyOrders, Items: 150
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Making GET request to BingX API: /openApi/swap/v2/trade/allOrders
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Successfully retrieved data from BingX API: /openApi/swap/v2/trade/allOrders, Items: 75
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Making GET request to BingX API: /openApi/swap/v2/user/positions
info: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Successfully retrieved data from BingX API: /openApi/swap/v2/user/positions, Items: 3
info: CryptoPositionAnalysis.Program[0]
      Processing Bybit exchange data...
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Making GET request to Bybit API: /v5/asset/transfer/query-account-coin-balance
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Successfully retrieved data from Bybit API: /v5/asset/transfer/query-account-coin-balance, Items: 8
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Making GET request to Bybit API: /v5/account/wallet-balance
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Successfully retrieved data from Bybit API: /v5/account/wallet-balance, Items: 6
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Making GET request to Bybit API: /v5/order/history
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Successfully retrieved data from Bybit API: /v5/order/history, Items: 42
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Making GET request to Bybit API: /v5/position/list
info: CryptoPositionAnalysis.Services.BybitApiClient[0]
      Successfully retrieved data from Bybit API: /v5/position/list, Items: 2
info: CryptoPositionAnalysis.Program[0]
      Exporting data to Excel...
info: CryptoPositionAnalysis.Services.ExcelExportService[0]
      Excel file exported successfully: ./ExcelReports/CryptoPortfolio_2025-07-02_13-49-15.xlsx, Sheets: 6
info: CryptoPositionAnalysis.Program[0]
      Excel export completed successfully!
info: CryptoPositionAnalysis.Program[0]
      File saved: ./ExcelReports/CryptoPortfolio_2025-07-02_13-49-15.xlsx
info: CryptoPositionAnalysis.Program[0]

=== PORTFOLIO SUMMARY ===
info: CryptoPositionAnalysis.Program[0]
      Spot Balances: 20 assets
info: CryptoPositionAnalysis.Program[0]
      Futures Balances: 11 assets
info: CryptoPositionAnalysis.Program[0]
      Spot Trades (30 days): 192 orders
info: CryptoPositionAnalysis.Program[0]
      Futures Trades (30 days): 75 orders
info: CryptoPositionAnalysis.Program[0]
      Active Positions: 5 positions
info: CryptoPositionAnalysis.Program[0]
      Total Unrealized PnL: 1,234.5678
info: CryptoPositionAnalysis.Program[0]
      Exchanges processed: BingX, Bybit
info: CryptoPositionAnalysis.Program[0]
      === Crypto Position Analysis Completed ===
```

## Run Without API Credentials

```
info: CryptoPositionAnalysis.Program[0]
      === Crypto Position Analysis Started ===
info: CryptoPositionAnalysis.Program[0]
      Timestamp: 07/02/2025 13:49:04
warn: CryptoPositionAnalysis.Program[0]
      BingX API credentials not configured. Please update appsettings.json with your API credentials.
warn: CryptoPositionAnalysis.Program[0]
      Bybit API credentials not configured. Please update appsettings.json with your API credentials.
warn: CryptoPositionAnalysis.Program[0]
      No data retrieved from any configured exchanges. Please check your API credentials and network connectivity.
info: CryptoPositionAnalysis.Program[0]
      === Crypto Position Analysis Completed ===
```

## Excel File Output Structure

The generated Excel file contains the following worksheets:

### 1. Spot Balances
| Exchange | Asset | Available | Locked | Total | USD Value | Timestamp |
|----------|-------|-----------|--------|-------|-----------|-----------|
| BingX    | BTC   | 0.5000    | 0.0000 | 0.5000| 15,000.00 | 2025-07-02 13:49:04 |
| BingX    | ETH   | 2.3500    | 0.1000 | 2.4500| 4,900.00  | 2025-07-02 13:49:04 |
| Bybit    | USDT  | 1,000.00  | 0.0000 | 1,000.00| 1,000.00| 2025-07-02 13:49:04 |

### 2. Futures Balances
| Exchange | Asset | Balance | Available | Cross PnL | Max Withdraw | Timestamp |
|----------|-------|---------|-----------|-----------|--------------|-----------|
| BingX    | USDT  | 5,000.00| 4,500.00  | 150.00    | 4,500.00     | 2025-07-02 13:49:04 |

### 3. Spot Trading History
| Exchange | Symbol  | Order ID | Trade ID | Side | Type  | Quantity | Price  | Executed Qty | Status | Fee  | Fee Asset | Trade Time |
|----------|---------|----------|----------|------|-------|----------|--------|--------------|--------|------|-----------|------------|
| BingX    | BTC-USDT| 12345678 | 87654321 | BUY  | LIMIT | 0.1000   | 30,000 | 0.1000       | FILLED | 3.00 | USDT      | 2025-07-01 10:30:15 |

### 4. Futures Trading History
| Exchange | Symbol    | Order ID | Side | Type  | Quantity | Price  | Avg Price | Executed Qty | Status | Fee  | Fee Asset | Realized PnL | Trade Time |
|----------|-----------|----------|------|-------|----------|--------|-----------|--------------|--------|------|-----------|--------------|------------|
| BingX    | BTC-USDT  | 23456789 | LONG | MARKET| 0.2000   | 29,500 | 29,500    | 0.2000       | FILLED | 5.90 | USDT      | 125.50       | 2025-07-01 15:45:30 |

### 5. Current Positions
| Exchange | Symbol    | Side | Size   | Entry Price | Mark Price | Unrealized PnL | Leverage | Margin  | Last Update |
|----------|-----------|------|--------|-------------|------------|----------------|----------|---------|-------------|
| BingX    | BTC-USDT  | LONG | 0.3000 | 29,000.00   | 30,500.00  | 450.00         | 10x      | 870.00  | 2025-07-02 13:45:00 |
| Bybit    | ETH-USDT  | SHORT| 1.5000 | 2,100.00    | 2,050.00   | 75.00          | 5x       | 630.00  | 2025-07-02 13:40:00 |

### 6. Portfolio Summary
- Generated timestamp
- Summary statistics (total assets, active positions, etc.)
- Total unrealized PnL with color coding
- Exchange breakdown

## Error Handling Examples

### API Rate Limiting
```
warn: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Rate limited by BingX API, waiting 5 seconds
```

### Network Issues
```
warn: CryptoPositionAnalysis.Services.BingXApiClient[0]
      Retrying BingX API call to /openApi/spot/v1/account/balance, attempt 2/3
error: CryptoPositionAnalysis.Services.BingXApiClient[0]
       Error calling BingX API: /openApi/spot/v1/account/balance
       System.HttpRequestException: No such host is known.
```

### Invalid Credentials
```
error: CryptoPositionAnalysis.Services.BingXApiClient[0]
       BingX API request failed with status Unauthorized: {"code":100001,"msg":"Signature verification failed"}
```
