using EventGrok.Bookings.Application.Services;
using EventGrok.Bookings.Application.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Bookings.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();

        services.AddHostedService<BookingProcessingBackgroundService>();

        return services;
    }
}