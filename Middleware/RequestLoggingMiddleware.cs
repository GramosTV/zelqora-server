using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace HealthcareApi.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();        // Add request ID to headers for tracking
        context.Response.Headers["X-Request-ID"] = requestId;

        // Log request
        await LogRequestAsync(context, requestId);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        try
        {
            var request = context.Request;
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";

            var requestLog = new
            {
                RequestId = requestId,
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                Headers = GetSafeHeaders(request.Headers),
                UserAgent = request.Headers.UserAgent.ToString(),
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("HTTP Request: {RequestLog}", System.Text.Json.JsonSerializer.Serialize(requestLog));

            // Log request body for POST/PUT requests (excluding sensitive endpoints)
            if ((request.Method == "POST" || request.Method == "PUT") &&
                !IsSensitiveEndpoint(request.Path) &&
                request.ContentLength > 0)
            {
                request.EnableBuffering();
                var body = await ReadStreamAsync(request.Body);
                request.Body.Position = 0;

                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogDebug("Request Body for {RequestId}: {Body}", requestId, body);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging request for {RequestId}", requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMilliseconds)
    {
        try
        {
            var response = context.Response;

            var responseLog = new
            {
                RequestId = requestId,
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength,
                ElapsedMilliseconds = elapsedMilliseconds,
                Timestamp = DateTime.UtcNow
            };

            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(logLevel, "HTTP Response: {ResponseLog}", System.Text.Json.JsonSerializer.Serialize(responseLog));

            // Log response body for errors
            if (response.StatusCode >= 400)
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                var responseBody = await ReadStreamAsync(response.Body);
                response.Body.Seek(0, SeekOrigin.Begin);

                if (!string.IsNullOrEmpty(responseBody))
                {
                    _logger.LogWarning("Error Response Body for {RequestId}: {Body}", requestId, responseBody);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging response for {RequestId}", requestId);
        }
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        stream.Seek(0, SeekOrigin.Begin);
        return content;
    }

    private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();
        var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-API-Key" };

        foreach (var header in headers)
        {
            if (sensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                safeHeaders[header.Key] = "***REDACTED***";
            }
            else
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }

        return safeHeaders;
    }

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var sensitiveEndpoints = new[] { "/auth/login", "/auth/register", "/auth/reset-password" };
        return sensitiveEndpoints.Any(endpoint => path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}
