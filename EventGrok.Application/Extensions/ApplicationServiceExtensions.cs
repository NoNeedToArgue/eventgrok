using EventGrok.Application.Services;
using EventGrok.Application.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IUserService, UserService>();

        services.AddHostedService<BookingProcessingBackgroundService>();

        return services;
    }
}