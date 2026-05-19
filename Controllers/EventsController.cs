using EventGrok.Models;
using EventGrok.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Controllers;

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
        PaginatedResultDto<Event> serviceResult = await eventService.GetEventsAsync(title, from, to, page, pageSize, ct);

        List<EventInfoDto> mappedItems = [.. serviceResult.Items.Select(MapToInfoDto)];

        return new PaginatedResultDto<EventInfoDto>(
            mappedItems,
            serviceResult.TotalCount,
            serviceResult.Page,
            serviceResult.PageSize,
            serviceResult.TotalPages
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventInfoDto>> GetEventById(Guid id, CancellationToken ct = default)
    {
        Event eventById = await eventService.GetEventByIdAsync(id, ct);
        return MapToInfoDto(eventById);
    }

    [HttpPost]
    public async Task<ActionResult<EventInfoDto>> CreateEvent([FromBody] CreateEventDto dto, CancellationToken ct = default)
    {
        Event newEvent = Event.Create(
            dto.Title,
            dto.Description,
            dto.StartAt,
            dto.EndAt,
            dto.TotalSeats
        );

        Event createdEvent = await eventService.AddEventAsync(newEvent, ct);

        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, MapToInfoDto(createdEvent));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateEvent(Guid id, [FromBody] CreateEventDto dto, CancellationToken ct = default)
    {
        Event existingEvent = await eventService.GetEventByIdAsync(id, ct);

        Event eventToUpdate = MapToEvent(dto);
        eventToUpdate.Id = id;
        eventToUpdate.AvailableSeats = existingEvent.AvailableSeats;

        await eventService.UpdateEventAsync(id, eventToUpdate, ct);

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
    public async Task<ActionResult<Booking>> BookEvent(Guid id, CancellationToken ct = default)
    {
        Booking booking = await bookingService.CreateBookingAsync(id, ct);

        string location = $"/bookings/{booking.Id}";

        return Accepted(location, booking);
    }

    private static Event MapToEvent(CreateEventDto dto) => new()
    {
        Title = dto.Title,
        Description = dto.Description,
        StartAt = dto.StartAt,
        EndAt = dto.EndAt,
        TotalSeats = dto.TotalSeats
    };

    private static EventInfoDto MapToInfoDto(Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.StartAt,
        e.EndAt,
        e.TotalSeats,
        e.AvailableSeats
    );
}