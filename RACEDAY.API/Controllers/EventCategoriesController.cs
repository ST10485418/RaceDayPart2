using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventCategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventCategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/EventCategories
        // Public: view all event-category links.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventCategories()
        {
            var items = await _context.EventCategories
                .AsNoTracking()
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/EventCategories/1
        // Public: view one event-category link.
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventCategory(int id)
        {
            var item = await _context.EventCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(ec =>
                    ec.EventCategoryID == id);

            if (item == null)
            {
                return NotFound(new
                {
                    message = "Event category not found."
                });
            }

            return Ok(item);
        }

        // GET: api/events/1/categories
        // Public: view categories linked to a specific event.
        [HttpGet("/api/events/{eventId:int}/categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            bool eventExists = await _context.Events
                .AnyAsync(e => e.EventID == eventId);

            if (!eventExists)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            var items = await _context.EventCategories
                .AsNoTracking()
                .Where(ec => ec.EventID == eventId)
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/events/1/categories
        // Organisers only: link a category to an event.
        [HttpPost("/api/events/{eventId:int}/categories")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEventCategory(
            int eventId,
            [FromBody] CreateEventCategoryRequest request)
        {
            if (!IsValid(request.EntryFee, request.MaxEntries,
                    request.StartTime, out string error))
            {
                return BadRequest(new { message = error });
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }

            if (eventItem.OrganiserID != organiserId.Value)
            {
                return Forbid();
            }

            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryID == request.CategoryID);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The selected category does not exist."
                });
            }

            bool duplicate = await _context.EventCategories
                .AnyAsync(ec =>
                    ec.EventID == eventId &&
                    ec.CategoryID == request.CategoryID);

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "This category is already linked to the event."
                });
            }

            var item = new EventCategory
            {
                EventID = eventId,
                CategoryID = request.CategoryID,
                EntryFee = request.EntryFee,
                MaxEntries = request.MaxEntries,
                StartTime = request.StartTime,
                IsActive = request.IsActive
            };

            _context.EventCategories.Add(item);
            await _context.SaveChangesAsync();

            return Created(
                $"/api/EventCategories/{item.EventCategoryID}",
                item);
        }

        // PUT: api/events/1/categories/2
        // Organisers only: update an event-category link.
        [HttpPut("/api/events/{eventId:int}/categories/{eventCategoryId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEventCategory(
            int eventId,
            int eventCategoryId,
            [FromBody] UpdateEventCategoryRequest request)
        {
            if (!IsValid(request.EntryFee, request.MaxEntries,
                    request.StartTime, out string error))
            {
                return BadRequest(new { message = error });
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }

            if (eventItem.OrganiserID != organiserId.Value)
            {
                return Forbid();
            }

            var item = await _context.EventCategories
                .FirstOrDefaultAsync(ec =>
                    ec.EventCategoryID == eventCategoryId &&
                    ec.EventID == eventId);

            if (item == null)
            {
                return NotFound(new
                {
                    message = "Event-category link not found."
                });
            }

            item.EntryFee = request.EntryFee;
            item.MaxEntries = request.MaxEntries;
            item.StartTime = request.StartTime;
            item.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(item);
        }

        // DELETE: api/events/1/categories/2
        // Organisers only: remove a category link.
        [HttpDelete("/api/events/{eventId:int}/categories/{eventCategoryId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEventCategory(
            int eventId,
            int eventCategoryId)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }

            if (eventItem.OrganiserID != organiserId.Value)
            {
                return Forbid();
            }

            var item = await _context.EventCategories
                .FirstOrDefaultAsync(ec =>
                    ec.EventCategoryID == eventCategoryId &&
                    ec.EventID == eventId);

            if (item == null)
            {
                return NotFound(new
                {
                    message = "Event-category link not found."
                });
            }

            bool hasEnrolments = await _context.Enrolments
                .AnyAsync(e => e.EventCategoryID == eventCategoryId);

            if (hasEnrolments)
            {
                return Conflict(new
                {
                    message = "This category has enrolments and cannot be deleted."
                });
            }

            _context.EventCategories.Remove(item);
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

        private static bool IsValid(
            decimal entryFee,
            int maxEntries,
            TimeSpan startTime,
            out string error)
        {
            if (entryFee < 0)
            {
                error = "Entry fee cannot be negative.";
                return false;
            }

            if (maxEntries <= 0)
            {
                error = "Maximum entries must be greater than zero.";
                return false;
            }

            if (startTime < TimeSpan.Zero ||
                startTime >= TimeSpan.FromDays(1))
            {
                error = "Enter a valid start time.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }

    public class CreateEventCategoryRequest
    {
        public int CategoryID { get; set; }

        public decimal EntryFee { get; set; }

        public int MaxEntries { get; set; }

        public TimeSpan StartTime { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateEventCategoryRequest
    {
        public decimal EntryFee { get; set; }

        public int MaxEntries { get; set; }

        public TimeSpan StartTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}