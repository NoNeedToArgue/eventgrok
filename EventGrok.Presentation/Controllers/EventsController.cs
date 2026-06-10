using EventGrok.Application.Services;
using EventGrok.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Presentation.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IEventService eventService, IBookingService bookingService) : ControllerBase
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

    [HttpPost]
    public async Task<ActionResult<EventInfoDto>> CreateEvent([FromBody] CreateEventDto dto, CancellationToken ct = default)
    {
        EventInfoDto createdEvent = await eventService.CreateEventAsync(dto, ct);

        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, createdEvent);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateEvent(Guid id, [FromBody] CreateEventDto dto, CancellationToken ct = default)
    {
        await eventService.UpdateEventAsync(id, dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteEvent(Guid id, CancellationToken ct = default)
    {
        await eventService.RemoveEventAsync(id, ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/book")]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> BookEvent(Guid id, CancellationToken ct = default)
    {
        BookingDto booking = await bookingService.CreateBookingAsync(id, ct);

        string location = $"/bookings/{booking.Id}";

        return Accepted(location, booking);
    }
}