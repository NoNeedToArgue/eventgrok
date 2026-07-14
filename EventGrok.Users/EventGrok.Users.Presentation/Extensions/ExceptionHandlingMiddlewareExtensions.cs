using EventGrok.Users.Presentation.Middlewares;

namespace EventGrok.Users.Presentation.Extensions;

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}