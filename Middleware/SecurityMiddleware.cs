using System.Security.Claims;

namespace HealthcareApi.Middleware;

/// <summary>
/// Security headers middleware for enhanced security
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        var headers = context.Response.Headers;

        // Prevent clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Enable XSS protection
        headers["X-XSS-Protection"] = "1; mode=block";

        // Enforce HTTPS
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Content Security Policy
        headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';";

        // Referrer Policy
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Feature Policy / Permissions Policy
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // Remove server information
        headers.Remove("Server");

        await _next(context);
    }
}

/// <summary>
/// Audit logging middleware for tracking user actions
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only audit specific actions
        if (ShouldAudit(context.Request))
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value;
            var action = GetAction(context.Request);
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("AUDIT: User {UserId} ({Email}) performed {Action} from {IpAddress} at {Timestamp}",
                userId ?? "Anonymous",
                userEmail ?? "Unknown",
                action,
                ipAddress,
                DateTime.UtcNow);
        }

        await _next(context);
    }

    private static bool ShouldAudit(HttpRequest request)
    {
        var auditablePaths = new[]
        {
            "/auth/login",
            "/auth/register",
            "/users",
            "/appointments",
            "/messages"
        };

        var auditableMethods = new[] { "POST", "PUT", "DELETE" };

        return auditablePaths.Any(path => request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)) &&
               auditableMethods.Contains(request.Method, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetAction(HttpRequest request)
    {
        return $"{request.Method} {request.Path}";
    }
}
