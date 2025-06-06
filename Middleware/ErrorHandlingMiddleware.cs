using System.Net;
using System.Text.Json;
using FluentValidation;

namespace HealthcareApi.Middleware;

/// <summary>
/// Global error handling middleware with comprehensive exception handling
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        string message = "An unexpected error occurred.";
        object? errors = null;

        switch (exception)
        {
            case ValidationException validationEx:
                statusCode = HttpStatusCode.BadRequest;
                message = "Validation failed.";
                errors = validationEx.Errors.Select(e => new
                {
                    Property = e.PropertyName,
                    Error = e.ErrorMessage
                });
                _logger.LogWarning("Validation error: {Errors}",
                    string.Join(", ", validationEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "You are not authorized to perform this action.";
                _logger.LogWarning("Unauthorized access attempt: {Message}", exception.Message);
                break;
            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                _logger.LogWarning("Bad request: {Message}", exception.Message);
                break;
            case ApplicationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                _logger.LogWarning("Application error: {Message}", exception.Message);
                break;
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = "The requested resource was not found.";
                _logger.LogWarning("Resource not found: {Message}", exception.Message);
                break;
            case TimeoutException:
                statusCode = HttpStatusCode.RequestTimeout;
                message = "The request timed out.";
                _logger.LogError(exception, "Request timeout");
                break;
            case InvalidOperationException:
                statusCode = HttpStatusCode.Conflict;
                message = "The operation is not valid in the current state.";
                _logger.LogError(exception, "Invalid operation");
                break;
            default:
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = statusCode.ToString(),
            message = message,
            errors = errors,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value,
            detailedMessage = _env.IsDevelopment() ? exception.ToString() : null,
            traceId = context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
