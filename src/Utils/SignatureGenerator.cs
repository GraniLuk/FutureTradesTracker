using System.Security.Cryptography;
using System.Text;

namespace FutureTradesTracker.Utils;

public static class SignatureGenerator
{
    /// <summary>
    /// Generates HMAC-SHA256 signature for BingX API requests
    /// </summary>
    /// <param name="queryString">The query string to be signed</param>
    /// <param name="secretKey">The secret key for signing</param>
    /// <returns>The HMAC-SHA256 signature in lowercase hexadecimal format</returns>
    public static string GenerateBingXSignature(string queryString, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var queryBytes = Encoding.UTF8.GetBytes(queryString);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(queryBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Generates HMAC-SHA256 signature for Bybit API requests
    /// </summary>
    /// <param name="timestamp">The timestamp parameter</param>
    /// <param name="apiKey">The API key</param>
    /// <param name="recvWindow">The receive window parameter</param>
    /// <param name="queryString">The query string</param>
    /// <param name="secretKey">The secret key for signing</param>
    /// <returns>The HMAC-SHA256 signature in lowercase hexadecimal format</returns>
    public static string GenerateBybitSignature(string timestamp, string apiKey, string recvWindow, string queryString, string secretKey)
    {
        var signaturePayload = $"{timestamp}{apiKey}{recvWindow}{queryString}";
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(signaturePayload);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Generates Unix timestamp in milliseconds
    /// </summary>
    /// <returns>Current Unix timestamp in milliseconds</returns>
    public static long GetUnixTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Generates Unix timestamp in milliseconds with clock skew compensation
    /// </summary>
    /// <param name="clockSkewMs">Clock skew compensation in milliseconds (negative to compensate for fast local clock)</param>
    /// <returns>Current Unix timestamp in milliseconds adjusted for clock skew</returns>
    public static long GetUnixTimestampWithSkewCompensation(int clockSkewMs = 0)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + clockSkewMs;
    }

    /// <summary>
    /// Generates Unix timestamp in seconds
    /// </summary>
    /// <returns>Current Unix timestamp in seconds</returns>
    public static long GetUnixTimestampSeconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
