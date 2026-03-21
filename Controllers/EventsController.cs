using EventGrok.Models;
using EventGrok.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public ActionResult<List<Event>> GetEvents()
    {
        return eventService.GetEvents();
    }

    [HttpGet("{id}")]
    public ActionResult<Event> GetEventById(int id)
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

    [HttpPut("{id}")]
    public ActionResult UpdateEvent(int id, [FromBody] CreateEventDto dto)
    {
        Event eventToUpdate = MapToEvent(dto);
        eventToUpdate.Id = id;

        eventService.UpdateEvent(id, eventToUpdate);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteEvent(int id)
    {
        eventService.RemoveEvent(id);

        return NoContent();
    }

    private Event MapToEvent(CreateEventDto dto)
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