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
        if (eventService.GetEventById(id) is { } resultEvent)
            return resultEvent;

        return NotFound($"Событие с id = {id} не найдено");
    }

    [HttpPost]
    public ActionResult<Event> CreateEvent([FromBody] CreateEventDto dto)
    {
        if (dto.EndAt <= dto.StartAt)
            return BadRequest("Дата окончания события должна быть позже даты начала");

        Event newEvent = MapToEvent(dto);

        Event createdEvent = eventService.AddEvent(newEvent);
        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, createdEvent);
    }

    [HttpPut("{id}")]
    public ActionResult UpdateEvent(int id, [FromBody] CreateEventDto dto)
    {
        if (dto.EndAt <= dto.StartAt)
            return BadRequest("Дата окончания события должна быть позже даты начала");

        Event eventToUpdate = MapToEvent(dto);

        eventToUpdate.Id = id;

        if (!eventService.UpdateEvent(id, eventToUpdate))
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteEvent(int id)
    {
        if (!eventService.RemoveEvent(id))
            return NotFound();

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