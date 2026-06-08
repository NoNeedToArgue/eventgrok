using EventGrok.Models;
using EventGrok.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Booking>> GetBookingById(Guid id, CancellationToken ct = default)
    {
        Booking booking = await bookingService.GetBookingByIdAsync(id, ct);

        return booking;
    }
}