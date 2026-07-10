using EventGrok.Bookings.Application.DTOs;
using EventGrok.Bookings.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EventGrok.Bookings.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingDto dto, CancellationToken ct = default)
    {
        Guid userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        BookingDto booking = await bookingService.CreateBookingAsync(dto.EventId, userId, ct);

        string location = $"/bookings/{booking.Id}";

        return Accepted(location, booking);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetBookingById(Guid id, CancellationToken ct = default)
    {
        BookingDto booking = await bookingService.GetBookingByIdAsync(id, ct);

        return booking;
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CancelBooking(Guid id, CancellationToken ct = default)
    {
        Guid userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        bool isAdmin = User.IsInRole("Admin");

        await bookingService.CancelBookingAsync(id, userId, isAdmin, ct);
        
        return NoContent();
    }
}