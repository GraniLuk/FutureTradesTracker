using Microsoft.Extensions.Configuration;

namespace FutureTradesTracker.Services;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<ConfigurationService>(optional: true)
            .AddEnvironmentVariables();

        _configuration = builder.Build();
    }

    public IConfiguration Configuration => _configuration;

    public BingXApiSettings GetBingXApiSettings()
    {
        return _configuration.GetSection("BingXApi").Get<BingXApiSettings>() ?? new BingXApiSettings();
    }

    public BybitApiSettings GetBybitApiSettings()
    {
        return _configuration.GetSection("BybitApi").Get<BybitApiSettings>() ?? new BybitApiSettings();
    }

    public ExcelSettings GetExcelSettings()
    {
        return _configuration.GetSection("ExcelSettings").Get<ExcelSettings>() ?? new ExcelSettings();
    }

    public RateLimitingSettings GetRateLimitingSettings()
    {
        return _configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();
    }
}

public class BingXApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://open-api.bingx.com";
}

public class BybitApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.bybit.com";
}

public class ExcelSettings
{
    public string OutputDirectory { get; set; } = "./ExcelReports/";
    public string FileNamePrefix { get; set; } = "CryptoPortfolio_";
    public string DateFormat { get; set; } = "yyyy-MM-dd_HH-mm-ss";
}

public class RateLimitingSettings
{
    public int BingXRequestsPerSecond { get; set; } = 5;
    public int BybitRequestsPerSecond { get; set; } = 10;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
}
