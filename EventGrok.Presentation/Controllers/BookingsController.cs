using EventGrok.Application.DTOs;
using EventGrok.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Presentation.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetBookingById(Guid id, CancellationToken ct = default)
    {
        BookingDto booking = await bookingService.GetBookingByIdAsync(id, ct);

        return booking;
    }
}