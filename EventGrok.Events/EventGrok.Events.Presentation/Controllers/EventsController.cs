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
        PaginatedResultDto<EventInfoDto> events = await eventService.GetEventsAsync(
            title,
            from,
            to,
            page,
            pageSize,
            ct);

        return Ok(events);
    }

    [HttpGet("top")]
    public async Task<ActionResult<IReadOnlyList<EventInfoDto>>> GetTopEvents(CancellationToken ct = default)
    {
        IReadOnlyList<EventInfoDto> topEvents = await eventService.GetTopEventsAsync(ct);

        return Ok(topEvents);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventInfoDto>> GetEventById(Guid id, CancellationToken ct = default)
    {
        EventInfoDto eventById = await eventService.GetEventByIdAsync(id, ct);

        return Ok(eventById);
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
}