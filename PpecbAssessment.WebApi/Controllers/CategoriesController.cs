using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpecbAssessment.Application.Categories.Dtos;
using PpecbAssessment.Application.Categories.Interfaces;
using System.Security.Claims;

namespace PpecbAssessment.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var items = await _categoryService.GetAllAsync(userId);
            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var item = await _categoryService.GetByIdAsync(id, userId);
            if (item == null)
                return NotFound(new { message = "Category not found." });

            return Ok(item);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var items = await _categoryService.GetLookupAsync(userId);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var auditUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _categoryService.CreateCategoryAsync(request, userId, auditUser);

            if (!result.Succeeded)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                message = result.Message,
                categoryId = result.Data
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var auditUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _categoryService.UpdateCategoryAsync(id, request, userId, auditUser);

            if (!result.Succeeded)
            {
                if (result.IsConflict)
                    return Conflict(new { message = result.Message });

                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
