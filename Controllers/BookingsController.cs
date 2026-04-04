using EventGrok.Models;
using EventGrok.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Booking>> GetBookingById(Guid id)
    {
        Booking booking = await bookingService.GetBookingByIdAsync(id);

        return booking;
    }
}