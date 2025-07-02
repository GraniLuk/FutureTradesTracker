<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a C# .NET console application for fetching cryptocurrency trading data from multiple exchanges (BingX and Bybit) and exporting it to Excel files for portfolio analysis.

## Key Technologies
- .NET 9 Console Application
- EPPlus for Excel file operations
- System.Text.Json for JSON serialization
- Microsoft.Extensions.Configuration for settings management
- Microsoft.Extensions.Logging for comprehensive logging
- HMAC-SHA256 for API authentication

## Architecture Patterns
- **Strategy Pattern**: Used for handling different exchange APIs
- **Service Layer**: Separated concerns with dedicated services for configuration, API clients, and Excel export
- **Repository Pattern**: Each exchange has its own API client with consistent interfaces
- **Error Handling**: Comprehensive try-catch blocks with retry mechanisms and exponential backoff

## API Authentication
- **BingX**: Uses HMAC-SHA256 signature with X-BX-APIKEY header
- **Bybit**: Uses HMAC-SHA256 signature with X-BAPI-* headers
- All timestamps are Unix milliseconds
- Rate limiting is implemented per exchange specifications

## Data Models
- **Balance**: Spot trading account balances
- **FuturesBalance**: Futures/derivatives account balances  
- **Trade**: Spot trading history
- **FuturesTrade**: Futures trading history
- **Position**: Current open futures positions
- **ApiResponse**: Generic API response wrapper

## Configuration
- Settings stored in `appsettings.json`
- Support for user secrets and environment variables
- Rate limiting configuration per exchange
- Excel export settings (output directory, filename format)

## Excel Export Features
- Multiple worksheets for different data types
- Professional formatting with colors for profit/loss
- Summary sheet with portfolio statistics
- Auto-fit columns and frozen headers
- Timestamped filenames for historical tracking

## Code Style Guidelines
- Use async/await for all I/O operations
- Implement proper disposal patterns (IDisposable)
- Log all API calls, successes, and failures
- Use descriptive variable names and method names
- Include comprehensive error handling with meaningful messages
- Follow Microsoft C# coding conventions

## Security Considerations
- Never log API credentials
- Use secure string handling for sensitive data
- Validate all API responses before processing
- Implement proper HTTP timeout handling
- Use HTTPS for all API communications

## Performance Optimizations
- Connection pooling for HTTP clients
- Rate limiting to respect exchange API limits
- Batch processing for large datasets
- Memory-efficient Excel generation with EPPlus
- Asynchronous operations throughout
