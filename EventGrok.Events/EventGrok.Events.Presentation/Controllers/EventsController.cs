using EventGrok.Events.Application.Services;
using EventGrok.Events.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EventGrok.Events.Presentation.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResultDto<EventInfoDto>>> GetEvents(
        string? title,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        return await eventService.GetEventsAsync(title, from, to, page, pageSize, ct);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventInfoDto>> GetEventById(Guid id, CancellationToken ct = default)
    {
        return await eventService.GetEventByIdAsync(id, ct);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventInfoDto>> CreateEvent([FromBody] CreateEventDto dto, CancellationToken ct = default)
    {
        EventInfoDto createdEvent = await eventService.CreateEventAsync(dto, ct);

        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, createdEvent);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateEvent(Guid id, [FromBody] CreateEventDto dto, CancellationToken ct = default)
    {
        await eventService.UpdateEventAsync(id, dto, ct);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteEvent(Guid id, CancellationToken ct = default)
    {
        await eventService.RemoveEventAsync(id, ct);

        return NoContent();
    }

    // [Authorize]
    // [HttpPost("{id:guid}/book")]
    // [ProducesResponseType(StatusCodes.Status409Conflict)]
    // public async Task<ActionResult<BookingDto>> BookEvent(Guid id, CancellationToken ct = default)
    // {
    //     Guid userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    //     BookingDto booking = await bookingService.CreateBookingAsync(id, userId, ct);

    //     string location = $"/bookings/{booking.Id}";

    //     return Accepted(location, booking);
    // }
}