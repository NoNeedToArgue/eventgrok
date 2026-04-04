using EventGrok.Models;
using EventGrok.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public ActionResult<PaginatedResultDto<Event>> GetEvents(
        string? title, 
        DateTime? from, 
        DateTime? to, 
        int page = 1, 
        int pageSize = 10)
    {
        return eventService.GetEvents(title, from, to, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Event> GetEventById(Guid id)
    {
        return eventService.GetEventById(id);
    }

    [HttpPost]
    public ActionResult<Event> CreateEvent([FromBody] CreateEventDto dto)
    {
        Event newEvent = MapToEvent(dto);
        
        Event createdEvent = eventService.AddEvent(newEvent);

        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, createdEvent);
    }

    [HttpPut("{id:guid}")]
    public ActionResult UpdateEvent(Guid id, [FromBody] CreateEventDto dto)
    {
        Event eventToUpdate = MapToEvent(dto);
        eventToUpdate.Id = id;

        eventService.UpdateEvent(id, eventToUpdate);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public ActionResult DeleteEvent(Guid id)
    {
        eventService.RemoveEvent(id);

        return NoContent();
    }

    private static Event MapToEvent(CreateEventDto dto)
    {
        return new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt
        };
    }
}