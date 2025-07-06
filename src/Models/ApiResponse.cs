using System.Text.Json.Serialization;

namespace FutureTradesTracker.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    public bool IsSuccess => Code == 0;
}

public class ApiResponseData<T>
{
    [JsonPropertyName("rows")]
    public List<T>? Rows { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
