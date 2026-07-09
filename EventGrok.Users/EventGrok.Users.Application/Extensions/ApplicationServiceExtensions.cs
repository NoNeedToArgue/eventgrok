using EventGrok.Users.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Users.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}