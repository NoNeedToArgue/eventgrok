using EventGrok.Events.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Events.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}