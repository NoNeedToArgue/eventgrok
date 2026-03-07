using EventGrok.Models;
using EventGrok.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Controllers;

[ApiController]
[Route("[controller]")]
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
}