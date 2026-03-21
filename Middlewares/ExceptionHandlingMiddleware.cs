using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        logger.LogError(ex,
            "Unhandled exception | TraceId: {TraceId} | Method: {Method} | Path: {Path} | Time: {Time}",
            context.TraceIdentifier,
            context.Request.Method,
            context.Request.Path,
            DateTime.UtcNow);

        (string title, int status, string detail) = ex switch
        {
            ArgumentException ae => ("Bad Request", 400, ae.Message),
            KeyNotFoundException nf => ("Not Found", 404, nf.Message),
            _ => ("Internal Server Error", 500, "An unexpected error occurred")
        };

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }
}