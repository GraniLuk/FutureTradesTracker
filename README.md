# Crypto Position Analysis

A comprehensive C# .NET console application that fetches cryptocurrency trading data from multiple exchanges (BingX and Bybit) and exports it to Excel files for portfolio analysis and tracking.

## Features

- **Multi-Exchange Support**: Currently supports BingX and Bybit exchanges
- **Comprehensive Data Collection**: 
  - Spot and futures account balances
  - Trading history (30 days)
  - Current open positions
- **Professional Excel Reports**: Multiple worksheets with formatted data and summary statistics
- **Rate Limiting Compliance**: Respects API rate limits with exponential backoff retry
- **Robust Error Handling**: Comprehensive logging and error recovery
- **Secure Authentication**: HMAC-SHA256 signature generation for API security
- **Configurable Settings**: Easy configuration through JSON files

## Prerequisites

- .NET 9.0 or later
- Valid API credentials from supported exchanges:
  - BingX API Key and Secret
  - Bybit API Key and Secret

## Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd FutureTradesTracker
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Update configuration file `appsettings.json` with your API credentials:
```json
{
  "BingXApi": {
    "ApiKey": "your-bingx-api-key",
    "SecretKey": "your-bingx-secret-key",
    "BaseUrl": "https://open-api.bingx.com"
  },
  "BybitApi": {
    "ApiKey": "your-bybit-api-key",
    "SecretKey": "your-bybit-secret-key",
    "BaseUrl": "https://api.bybit.com"
  }
}
```

## Usage

### Running the Application

```bash
dotnet run
```

### Command Line Options

The application can be run with default settings or customized through the configuration file.

### Output

The application generates Excel files in the `ExcelReports/` directory with the following worksheets:

1. **Spot Balances**: Available and locked balances for spot trading
2. **Futures Balances**: Futures account balances with PnL information
3. **Spot Trading History**: Recent spot trading activity (30 days)
4. **Futures Trading History**: Recent futures trading activity (30 days)
5. **Current Positions**: Active futures positions with unrealized PnL
6. **Portfolio Summary**: Overview statistics and exchange breakdown

### Sample Output Structure

```
ExcelReports/
└── CryptoPortfolio_2025-07-02_14-30-45.xlsx
    ├── Spot Balances
    ├── Futures Balances
    ├── Spot Trading History
    ├── Futures Trading History
    ├── Current Positions
    └── Portfolio Summary
```

## Configuration

### appsettings.json Structure

```json
{
  "BingXApi": {
    "ApiKey": "your-bingx-api-key",
    "SecretKey": "your-bingx-secret-key",
    "BaseUrl": "https://open-api.bingx.com"
  },
  "BybitApi": {
    "ApiKey": "your-bybit-api-key",
    "SecretKey": "your-bybit-secret-key",
    "BaseUrl": "https://api.bybit.com"
  },
  "ExcelSettings": {
    "OutputDirectory": "./ExcelReports/",
    "FileNamePrefix": "CryptoPortfolio_",
    "DateFormat": "yyyy-MM-dd_HH-mm-ss"
  },
  "RateLimiting": {
    "BingXRequestsPerSecond": 5,
    "BybitRequestsPerSecond": 10,
    "RetryAttempts": 3,
    "RetryDelaySeconds": 2
  }
}
```

### User Secrets (Recommended for Development)

For security, use .NET User Secrets to store API credentials:

```bash
dotnet user-secrets init
dotnet user-secrets set "BingXApi:ApiKey" "your-actual-api-key"
dotnet user-secrets set "BingXApi:SecretKey" "your-actual-secret-key"
dotnet user-secrets set "BybitApi:ApiKey" "your-actual-api-key"
dotnet user-secrets set "BybitApi:SecretKey" "your-actual-secret-key"
```

## API Endpoints Used

### BingX API
- `/openApi/spot/v1/account/balance` - Spot balances
- `/openApi/swap/v2/user/balance` - Futures balances
- `/openApi/spot/v1/trade/historyOrders` - Spot trade history
- `/openApi/swap/v2/trade/allOrders` - Futures trade history
- `/openApi/swap/v2/user/positions` - Current positions

### Bybit API
- `/v5/asset/transfer/query-account-coin-balance` - Spot balances
- `/v5/account/wallet-balance` - Futures balances
- `/v5/order/history` - Spot trade history
- `/v5/position/list` - Current positions

## Rate Limiting

The application implements rate limiting to comply with exchange API limits:
- **BingX**: 5 requests per second (configurable)
- **Bybit**: 10 requests per second (configurable)
- **Retry Logic**: Exponential backoff with configurable attempts

## Error Handling

- Comprehensive logging at multiple levels (Information, Warning, Error)
- Automatic retry with exponential backoff for failed requests
- Graceful handling of rate limit responses
- Detailed error messages for troubleshooting

## Logging

The application provides detailed logging:
- Console output for real-time monitoring
- API call logging with success/failure status
- Performance metrics and timing information
- Error details with stack traces when needed

## Security Features

- HMAC-SHA256 signature generation for API authentication
- Secure credential storage options (User Secrets, Environment Variables)
- No credential logging in output
- HTTPS-only API communications
- Input validation for all API parameters

## Scheduling

For automated execution, you can schedule the application using:

### Windows Task Scheduler
Create a scheduled task to run the application at regular intervals.

### Linux Cron
```bash
# Run every hour
0 * * * * /usr/bin/dotnet /path/to/FutureTradesTracker.dll
```

### Docker (Optional)
The application can be containerized for deployment in container environments.

## Troubleshooting

### Common Issues

1. **API Authentication Errors**
   - Verify API credentials are correct
   - Check that API keys have required permissions
   - Ensure system time is synchronized

2. **Rate Limiting**
   - Adjust rate limiting settings in configuration
   - Check exchange API documentation for current limits

3. **Network Issues**
   - Verify internet connectivity
   - Check firewall settings
   - Ensure exchange APIs are accessible

4. **Excel Export Errors**
   - Verify write permissions to output directory
   - Ensure sufficient disk space
   - Check that output directory exists

### Debug Mode

To run in debug mode with additional logging:
```bash
dotnet run --configuration Debug
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

This software is provided for informational purposes only. Always verify trading data independently and use at your own risk. The developers are not responsible for any financial losses resulting from the use of this software.

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review application logs for error details
3. Create an issue in the repository with:
   - Error messages (without API credentials)
   - Configuration details
   - Steps to reproduce the issue

## Future Enhancements

- Additional exchange integrations (Binance, Coinbase, etc.)
- Real-time data streaming
- Portfolio performance analytics
- Custom reporting templates
- Web dashboard interface
- Database storage for historical data
- Automated trading alerts
