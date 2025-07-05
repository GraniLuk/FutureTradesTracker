using Microsoft.Extensions.Logging;

namespace CryptoPositionAnalysis.Utils;

public static class LoggerFactory
{
    private static ILoggerFactory? _factory;

    public static void Initialize(ILoggerFactory factory)
    {
        _factory = factory;
    }

    public static ILogger<T> CreateLogger<T>()
    {
        if (_factory == null)
        {
            throw new InvalidOperationException("Logger factory not initialized. Call Initialize() first.");
        }
        return _factory.CreateLogger<T>();
    }

    public static ILogger CreateLogger(string categoryName)
    {
        if (_factory == null)
        {
            throw new InvalidOperationException("Logger factory not initialized. Call Initialize() first.");
        }
        return _factory.CreateLogger(categoryName);
    }
}

public static class LoggerExtensions
{
    public static void LogApiCall(this ILogger logger, string exchange, string endpoint, string method = "GET")
    {
        logger.LogInformation("Making {Method} request to {Exchange} API: {Endpoint}", method, exchange, endpoint);
    }

    public static void LogApiSuccess(this ILogger logger, string exchange, string endpoint, int dataCount = 0)
    {
        logger.LogInformation("Successfully retrieved data from {Exchange} API: {Endpoint}, Items: {Count}", exchange, endpoint, dataCount);
    }

    public static void LogApiError(this ILogger logger, string exchange, string endpoint, Exception exception)
    {
        logger.LogError(exception, "Error calling {Exchange} API: {Endpoint}", exchange, endpoint);
    }

    public static void LogRateLimit(this ILogger logger, string exchange, int delayMs)
    {
        logger.LogDebug("Rate limiting {Exchange} API request, waiting {DelayMs}ms", exchange, delayMs);
    }

    public static void LogRetry(this ILogger logger, string exchange, string endpoint, int attempt, int maxAttempts)
    {
        logger.LogWarning("Retrying {Exchange} API call to {Endpoint}, attempt {Attempt}/{MaxAttempts}", exchange, endpoint, attempt, maxAttempts);
    }

    public static void LogExcelExport(this ILogger logger, string filePath, int sheetsCount)
    {
        logger.LogInformation("Excel file exported successfully: {FilePath}, Sheets: {SheetsCount}", filePath, sheetsCount);
    }
}
