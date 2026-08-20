using System.Net;
using AkironSeo.Application.Common.Exceptions;
using AkironSeo.Application.Common.Security;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AkironSeo.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}", correlationId, context.Request.Path);
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/problem+json";
        
        var statusCode = exception switch
        {
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ConflictException => (int)HttpStatusCode.Conflict,
            QuotaExceededException => (int)HttpStatusCode.PaymentRequired,
            ValidationException => (int)HttpStatusCode.BadRequest,
            UnsafeOutboundUrlException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                400 => "Validation failed",
                401 => "Authentication failed",
                402 => "Monthly quota exceeded",
                404 => "Resource not found",
                409 => "Resource conflict",
                500 => "An unexpected server error occurred.",
                _ => "Request failed"
            },
            Detail = statusCode == 500 ? "Please contact support if the issue persists." : exception.Message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            }
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => char.ToLowerInvariant(group.Key[0]) + group.Key[1..],
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
        }

        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
