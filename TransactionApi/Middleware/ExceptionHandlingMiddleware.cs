using System.Net;
using System.Text.Json;
using FluentValidation;
using TransactionApi.Domain.Exceptions;

namespace TransactionApi.Middleware;

/// <summary>Handles application exceptions and converts them into consistent JSON error responses.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    /// <summary>Initialises the middleware with the next pipeline delegate and logger.</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Executes the middleware for the current HTTP request.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException validationException => CreateValidationResponse(validationException),
            NotFoundException notFoundException => CreateResponse(HttpStatusCode.NotFound, "not_found", notFoundException.Message),
            DuplicateTransactionException duplicateException => CreateResponse(HttpStatusCode.Conflict, "duplicate_transaction", duplicateException.Message),
            ArgumentException argumentException => CreateResponse(HttpStatusCode.BadRequest, "bad_request", argumentException.Message),
            _ => CreateUnhandledResponse(exception)
        };

        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response.Payload));
    }

    private (HttpStatusCode StatusCode, object Payload) CreateUnhandledResponse(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred while processing the request.");
        return CreateResponse(HttpStatusCode.InternalServerError, "internal_server_error", "An unexpected error occurred.");
    }

    private static (HttpStatusCode StatusCode, object Payload) CreateValidationResponse(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return (
            HttpStatusCode.UnprocessableEntity,
            new
            {
                type = "validation_error",
                message = "One or more validation errors occurred.",
                errors
            });
    }

    private static (HttpStatusCode StatusCode, object Payload) CreateResponse(HttpStatusCode statusCode, string type, string message) =>
        (statusCode, new { type, message, errors = new Dictionary<string, string[]>() });
}
