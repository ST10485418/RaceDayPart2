using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/Events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EventsController(RaceDayContext context)
    {
        _context = context;
    }

    // GET: api/Events
    // Public: view active events.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventID,
                e.OrganiserID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.IsActive,
                e.CreatedAt
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET: api/Events/5
    // Public: view one event.
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventById(int id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(e => new
            {
                e.EventID,
                e.OrganiserID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.IsActive,
                e.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (eventItem == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        return Ok(eventItem);
    }

    // POST: api/Events
    // Organisers only.
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(
        [FromBody] EventRequest request)
    {
        if (!IsValidEvent(request, out string error))
        {
            return BadRequest(new { message = error });
        }

        int? organiserId = GetCurrentUserId();

        if (organiserId == null)
        {
            return Unauthorized(new
            {
                message = "The token does not contain a valid user ID."
            });
        }

        var newEvent = new Event
        {
            OrganiserID = organiserId.Value,
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEventById),
            new { id = newEvent.EventID },
            new
            {
                message = "Event created successfully.",
                eventItem = ToResponse(newEvent)
            });
    }

    // PUT: api/Events/5
    // Only the organiser who owns the event can update it.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] EventRequest request)
    {
        if (!IsValidEvent(request, out string error))
        {
            return BadRequest(new { message = error });
        }

        int? organiserId = GetCurrentUserId();

        if (organiserId == null)
        {
            return Unauthorized();
        }

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (eventItem == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (eventItem.OrganiserID != organiserId.Value)
        {
            return Forbid();
        }

        eventItem.EventName = request.EventName.Trim();
        eventItem.Description = request.Description.Trim();
        eventItem.EventDate = request.EventDate;
        eventItem.Location = request.Location.Trim();
        eventItem.DistanceKm = request.DistanceKm;
        eventItem.EventType = request.EventType.Trim();
        eventItem.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Event updated successfully.",
            eventItem = ToResponse(eventItem)
        });
    }

    // DELETE: api/Events/5
    // Only the organiser who owns the event can delete it.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        int? organiserId = GetCurrentUserId();

        if (organiserId == null)
        {
            return Unauthorized();
        }

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (eventItem == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (eventItem.OrganiserID != organiserId.Value)
        {
            return Forbid();
        }

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        string? id = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(id, out int userId)
            ? userId
            : null;
    }

    private static bool IsValidEvent(
        EventRequest request,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(request.EventName))
        {
            error = "Event name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            error = "Event description is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            error = "Event location is required.";
            return false;
        }

        if (request.DistanceKm <= 0)
        {
            error = "Distance must be greater than zero.";
            return false;
        }

        string type = request.EventType.Trim();

        if (!new[] { "Run", "Walk", "Cycle" }
            .Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            error = "Event type must be Run, Walk or Cycle.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static object ToResponse(Event e) => new
    {
        e.EventID,
        e.OrganiserID,
        e.EventName,
        e.Description,
        e.EventDate,
        e.Location,
        e.DistanceKm,
        e.EventType,
        e.IsActive,
        e.CreatedAt
    };
}

public class EventRequest
{
    [Required]
    [StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal DistanceKm { get; set; }

    [Required]
    public string EventType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}