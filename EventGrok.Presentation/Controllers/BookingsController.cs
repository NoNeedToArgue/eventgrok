using EventGrok.Application.DTOs;
using EventGrok.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EventGrok.Presentation.Controllers;

[Authorize]
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

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CancelBooking(Guid id, CancellationToken ct = default)
    {
        Guid userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        bool isAdmin = User.IsInRole("Admin");

        await bookingService.CancelBookingAsync(id, userId, isAdmin, ct);
        
        return NoContent();
    }
}