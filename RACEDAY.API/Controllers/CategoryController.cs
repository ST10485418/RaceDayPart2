using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        // Public: view all categories.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Categories/5
        // Public: view one category.
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            return Ok(category);
        }

        // POST: api/Categories
        // Organisers only: create a category.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory(
            [FromBody] CategoryRequest request)
        {
            if (!IsValid(request, out string error))
            {
                return BadRequest(new { message = error });
            }

            string name = request.CategoryName.Trim();
            string type = request.CategoryType.Trim();

            bool exists = await _context.Categories.AnyAsync(c =>
                c.CategoryName == name &&
                c.CategoryType == type);

            if (exists)
            {
                return Conflict(new
                {
                    message = "A category with this name and type already exists."
                });
            }

            var category = new Category
            {
                CategoryName = name,
                CategoryType = type,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoryID },
                category);
        }

        // PUT: api/Categories/5
        // Organisers only: update a category.
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CategoryRequest request)
        {
            if (!IsValid(request, out string error))
            {
                return BadRequest(new { message = error });
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            string name = request.CategoryName.Trim();
            string type = request.CategoryType.Trim();

            bool duplicate = await _context.Categories.AnyAsync(c =>
                c.CategoryID != id &&
                c.CategoryName == name &&
                c.CategoryType == type);

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Another category with this name and type already exists."
                });
            }

            category.CategoryName = name;
            category.CategoryType = type;
            category.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // DELETE: api/Categories/5
        // Organisers only: delete a category.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            bool isInUse = await _context.EventCategories
                .AnyAsync(ec => ec.CategoryID == id);

            if (isInUse)
            {
                return Conflict(new
                {
                    message = "This category is linked to an event and cannot be deleted."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static bool IsValid(
            CategoryRequest request,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
            {
                error = "Category name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.CategoryType))
            {
                error = "Category type is required.";
                return false;
            }

            if (request.CategoryName.Trim().Length > 100)
            {
                error = "Category name cannot exceed 100 characters.";
                return false;
            }

            if (request.CategoryType.Trim().Length > 50)
            {
                error = "Category type cannot exceed 50 characters.";
                return false;
            }

            if (request.Description?.Length > 1000)
            {
                error = "Description cannot exceed 1000 characters.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }

    public class CategoryRequest
    {
        public string CategoryName { get; set; } = string.Empty;

        public string CategoryType { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}