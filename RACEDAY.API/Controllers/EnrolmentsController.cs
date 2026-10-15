using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/enrolments")]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/enrolments
        // Participants see their own enrolments.
        // Organisers see enrolments for their own events.
        [HttpGet]
        public async Task<IActionResult> GetEnrolments()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            bool isOrganiser = User.IsInRole("Organiser");

            var query = _context.Enrolments.AsNoTracking();

            if (isOrganiser)
            {
                query = query.Where(e =>
                    e.EventCategory!.Event!.OrganiserID == userId.Value);
            }
            else
            {
                query = query.Where(e => e.UserID == userId.Value);
            }

            var enrolments = await query
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.UserID,
                    e.EventCategoryID,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET: api/enrolments/5
        // A participant can view their own record.
        // An organiser can view records for their own events.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var enrolment = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.EnrolmentID == id)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.UserID,
                    e.EventCategoryID,
                    e.EnrolmentDate,
                    e.Status,
                    EventID = e.EventCategory!.EventID,
                    OrganiserID = e.EventCategory.Event!.OrganiserID
                })
                .FirstOrDefaultAsync();

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "Enrolment not found."
                });
            }

            bool isOwner = enrolment.UserID == userId.Value;
            bool isOrganiserOfEvent =
                User.IsInRole("Organiser") &&
                enrolment.OrganiserID == userId.Value;

            if (!isOwner && !isOrganiserOfEvent)
                return Forbid();

            return Ok(enrolment);
        }

        // POST: api/enrolments
        // Participants can enrol in an event category.
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] CreateEnrolmentRequest request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var eventCategory = await _context.EventCategories
                .FirstOrDefaultAsync(ec =>
                    ec.EventCategoryID == request.EventCategoryID);

            if (eventCategory == null)
            {
                return NotFound(new
                {
                    message = "Event category not found."
                });
            }

            if (!eventCategory.IsActive)
            {
                return BadRequest(new
                {
                    message = "This event category is not active."
                });
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.EventID == eventCategory.EventID);

            if (eventItem == null || !eventItem.IsActive)
            {
                return BadRequest(new
                {
                    message = "This event is not available for enrolment."
                });
            }

            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.UserID == userId.Value &&
                    e.EventCategoryID == request.EventCategoryID);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You already have an enrolment for this category."
                });
            }

            int currentEntries = await _context.Enrolments
                .CountAsync(e =>
                    e.EventCategoryID == request.EventCategoryID &&
                    e.Status != "Cancelled");

            if (currentEntries >= eventCategory.MaxEntries)
            {
                return Conflict(new
                {
                    message = "This event category has reached its maximum capacity."
                });
            }

            var enrolment = new Enrolment
            {
                UserID = userId.Value,
                EventCategoryID = request.EventCategoryID,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Enrolment created successfully.",
                enrolment = new
                {
                    enrolment.EnrolmentID,
                    enrolment.UserID,
                    enrolment.EventCategoryID,
                    enrolment.EnrolmentDate,
                    enrolment.Status
                }
            });
        }

        // GET: api/enrolments/me
        // Participants see only their own enrolments.
        [HttpGet("me")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.UserID == userId.Value)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.UserID,
                    e.EventCategoryID,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // DELETE: api/enrolments/5
        // Participants can cancel only their own enrolments.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentID == id &&
                    e.UserID == userId.Value);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "Your enrolment was not found."
                });
            }

            if (enrolment.Status == "Cancelled")
            {
                return Conflict(new
                {
                    message = "This enrolment is already cancelled."
                });
            }

            // Keep the record for history and preserve the database relationship.
            enrolment.Status = "Cancelled";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/events/5/enrolments
        // Organisers can view enrolments for their own events.
        [HttpGet("/api/events/{eventId:int}/enrolments")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (eventItem.OrganiserID != userId.Value)
                return Forbid();

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.EventCategory!.EventID == eventId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.UserID,
                    e.EventCategoryID,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        private int? GetCurrentUserId()
        {
            string? id = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(id, out int userId)
                ? userId
                : null;
        }
    }

    public class CreateEnrolmentRequest
    {
        public int EventCategoryID { get; set; }
    }
}