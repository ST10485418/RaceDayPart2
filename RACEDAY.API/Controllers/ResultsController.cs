
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/results")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/results
        // Organisers see results for their own events.
        // Participants see only their own results.
        [HttpGet]
        public async Task<IActionResult> GetResults()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var query = _context.Results
                .AsNoTracking()
                .AsQueryable();

            if (User.IsInRole("Organiser"))
            {
                query = query.Where(r =>
                    r.Enrolment!.EventCategory!.Event!.OrganiserID
                    == userId.Value);
            }
            else if (User.IsInRole("Participant"))
            {
                query = query.Where(r =>
                    r.Enrolment!.UserID == userId.Value);
            }
            else
            {
                return Forbid();
            }

            var results = await query
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    r.ChipTime,
                    r.GunTime,
                    r.PositionOverall,
                    r.PositionCategory,
                    r.IsCompleted,
                    r.Notes,
                    r.RecordedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/me
        // Participants view their own results.
        [HttpGet("me")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var results = await _context.Results
                .AsNoTracking()
                .Where(r => r.Enrolment!.UserID == userId.Value)
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    r.ChipTime,
                    r.GunTime,
                    r.PositionOverall,
                    r.PositionCategory,
                    r.IsCompleted,
                    r.Notes,
                    r.RecordedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/1
        // Only the participant who owns the result or the event's
        // organiser may view it.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetResult(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var result = await _context.Results
                .AsNoTracking()
                .Where(r => r.ResultID == id)
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    r.ChipTime,
                    r.GunTime,
                    r.PositionOverall,
                    r.PositionCategory,
                    r.IsCompleted,
                    r.Notes,
                    r.RecordedAt,
                    ParticipantID = r.Enrolment!.UserID,
                    OrganiserID = r.Enrolment!
                        .EventCategory!.Event!.OrganiserID
                })
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound(new { message = "Result not found." });

            bool isParticipantOwner =
                User.IsInRole("Participant")
                && result.ParticipantID == userId.Value;

            bool isEventOrganiser =
                User.IsInRole("Organiser")
                && result.OrganiserID == userId.Value;

            if (!isParticipantOwner && !isEventOrganiser)
                return Forbid();

            return Ok(new
            {
                result.ResultID,
                result.EnrolmentID,
                result.ChipTime,
                result.GunTime,
                result.PositionOverall,
                result.PositionCategory,
                result.IsCompleted,
                result.Notes,
                result.RecordedAt
            });
        }

        // POST: api/results
        // Organisers record results for enrolments in their own events.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateResult(
            [FromBody] ResultRequest request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (!ValidResult(request))
                return BadRequest(new
                {
                    message = "Times and positions must be valid and non-negative."
                });

            var enrolment = await _context.Enrolments
                .Include(e => e.EventCategory)
                .ThenInclude(ec => ec!.Event)
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentID == request.EnrolmentID);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.EventCategory?.Event?.OrganiserID != userId.Value)
                return Forbid();

            bool resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentID == request.EnrolmentID);

            if (resultExists)
                return Conflict(new
                {
                    message = "A result already exists for this enrolment."
                });

            var result = new Result
            {
                EnrolmentID = request.EnrolmentID,
                ChipTime = request.ChipTime,
                GunTime = request.GunTime,
                PositionOverall = request.PositionOverall,
                PositionCategory = request.PositionCategory,
                IsCompleted = request.IsCompleted,
                Notes = request.Notes,
                RecordedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Result created successfully.",
                result.ResultID,
                result.EnrolmentID,
                result.ChipTime,
                result.GunTime,
                result.PositionOverall,
                result.PositionCategory,
                result.IsCompleted,
                result.Notes,
                result.RecordedAt
            });
        }

        // PUT: api/results/1
        // Organisers may update results for their own events.
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateResult(
            int id,
            [FromBody] ResultRequest request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (!ValidResult(request))
                return BadRequest(new
                {
                    message = "Times and positions must be valid and non-negative."
                });

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.EventCategory)
                        .ThenInclude(ec => ec!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound(new { message = "Result not found." });

            if (result.Enrolment?.EventCategory?.Event?.OrganiserID
                != userId.Value)
                return Forbid();

            if (request.EnrolmentID != result.EnrolmentID)
                return BadRequest(new
                {
                    message = "The enrolment linked to a result cannot be changed."
                });

            result.ChipTime = request.ChipTime;
            result.GunTime = request.GunTime;
            result.PositionOverall = request.PositionOverall;
            result.PositionCategory = request.PositionCategory;
            result.IsCompleted = request.IsCompleted;
            result.Notes = request.Notes;
            result.RecordedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result updated successfully.",
                result.ResultID,
                result.EnrolmentID,
                result.ChipTime,
                result.GunTime,
                result.PositionOverall,
                result.PositionCategory,
                result.IsCompleted,
                result.Notes,
                result.RecordedAt
            });
        }

        // DELETE: api/results/1
        // Organisers may delete results for their own events.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.EventCategory)
                        .ThenInclude(ec => ec!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound(new { message = "Result not found." });

            if (result.Enrolment?.EventCategory?.Event?.OrganiserID
                != userId.Value)
                return Forbid();

            _context.Results.Remove(result);
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

        private static bool ValidResult(ResultRequest request)
        {
            return
                (request.ChipTime == null || request.ChipTime >= TimeSpan.Zero)
                && (request.GunTime == null || request.GunTime >= TimeSpan.Zero)
                && (request.PositionOverall == null || request.PositionOverall > 0)
                && (request.PositionCategory == null || request.PositionCategory > 0);
        }
    }

    public class ResultRequest
    {
        public int EnrolmentID { get; set; }
        public TimeSpan? ChipTime { get; set; }
        public TimeSpan? GunTime { get; set; }
        public int? PositionOverall { get; set; }
        public int? PositionCategory { get; set; }
        public bool IsCompleted { get; set; }
        public string? Notes { get; set; }
    }
}
