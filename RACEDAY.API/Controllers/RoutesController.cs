
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/routes")]
    [Authorize]
    public class RoutesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public RoutesController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/routes
        // Authenticated users can view routes.
        [HttpGet]
        public async Task<IActionResult> GetRoutes()
        {
            var routes = await _context.Routes
                .AsNoTracking()
                .Select(r => new
                {
                    r.RouteID,
                    r.EventID,
                    r.RouteName,
                    r.DistanceKm,
                    r.ElevationGain,
                    r.RouteDescription,
                    r.MapUrl
                })
                .ToListAsync();

            return Ok(routes);
        }

        // GET: api/routes/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRoute(int id)
        {
            var route = await _context.Routes
                .AsNoTracking()
                .Where(r => r.RouteID == id)
                .Select(r => new
                {
                    r.RouteID,
                    r.EventID,
                    r.RouteName,
                    r.DistanceKm,
                    r.ElevationGain,
                    r.RouteDescription,
                    r.MapUrl
                })
                .FirstOrDefaultAsync();

            if (route == null)
                return NotFound(new { message = "Route not found." });

            return Ok(route);
        }

        // GET: api/events/1/route
        // View the route for a particular event.
        [HttpGet("/api/events/{eventId:int}/route")]
        public async Task<IActionResult> GetEventRoute(int eventId)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventID == eventId);

            if (!eventExists)
                return NotFound(new { message = "Event not found." });

            var route = await _context.Routes
                .AsNoTracking()
                .Where(r => r.EventID == eventId)
                .Select(r => new
                {
                    r.RouteID,
                    r.EventID,
                    r.RouteName,
                    r.DistanceKm,
                    r.ElevationGain,
                    r.RouteDescription,
                    r.MapUrl
                })
                .FirstOrDefaultAsync();

            if (route == null)
                return NotFound(new { message = "No route has been added to this event." });

            return Ok(route);
        }

        // POST: api/events/1/route
        // Only the organiser who owns the event may create its route.
        [HttpPost("/api/events/{eventId:int}/route")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateRoute(
            int eventId,
            [FromBody] RouteRequest request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (!ValidRoute(request))
                return BadRequest(new
                {
                    message = "Route name is required and distances must be positive."
                });

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            if (eventItem.OrganiserID != userId.Value)
                return Forbid();

            bool routeExists = await _context.Routes
                .AnyAsync(r => r.EventID == eventId);

            if (routeExists)
                return Conflict(new
                {
                    message = "A route already exists for this event. Use PUT to update it."
                });

            var route = new RaceDay.Api.Models.Route
            {
                EventID = eventId,
                RouteName = request.RouteName.Trim(),
                DistanceKm = request.DistanceKm,
                ElevationGain = request.ElevationGain,
                RouteDescription = request.RouteDescription,
                MapUrl = request.MapUrl
            };

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Route created successfully.",
                route.RouteID,
                route.EventID,
                route.RouteName,
                route.DistanceKm,
                route.ElevationGain,
                route.RouteDescription,
                route.MapUrl
            });
        }

        // PUT: api/events/1/route
        // Only the organiser who owns the event may update its route.
        [HttpPut("/api/events/{eventId:int}/route")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEventRoute(
            int eventId,
            [FromBody] RouteRequest request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (!ValidRoute(request))
                return BadRequest(new
                {
                    message = "Route name is required and distances must be positive."
                });

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            if (eventItem.OrganiserID != userId.Value)
                return Forbid();

            var route = await _context.Routes
                .FirstOrDefaultAsync(r => r.EventID == eventId);

            if (route == null)
                return NotFound(new { message = "No route exists for this event yet." });

            route.RouteName = request.RouteName.Trim();
            route.DistanceKm = request.DistanceKm;
            route.ElevationGain = request.ElevationGain;
            route.RouteDescription = request.RouteDescription;
            route.MapUrl = request.MapUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Route updated successfully.",
                route.RouteID,
                route.EventID,
                route.RouteName,
                route.DistanceKm,
                route.ElevationGain,
                route.RouteDescription,
                route.MapUrl
            });
        }

        // DELETE: api/events/1/route
        // Only the organiser who owns the event may delete its route.
        [HttpDelete("/api/events/{eventId:int}/route")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEventRoute(int eventId)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            if (eventItem.OrganiserID != userId.Value)
                return Forbid();

            var route = await _context.Routes
                .FirstOrDefaultAsync(r => r.EventID == eventId);

            if (route == null)
                return NotFound(new { message = "No route exists for this event." });

            _context.Routes.Remove(route);
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

        private static bool ValidRoute(RouteRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.RouteName)
                && request.DistanceKm > 0
                && (request.ElevationGain == null
                    || request.ElevationGain >= 0);
        }
    }

    public class RouteRequest
    {
        public string RouteName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal? ElevationGain { get; set; }
        public string? RouteDescription { get; set; }
        public string? MapUrl { get; set; }
    }
}
