using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpecbAssessment.Application.Products.Dtos;
using PpecbAssessment.Application.Products.Interfaces;
using PpecbAssessment.WebApi.Models;
using System.Globalization;
using System.Security.Claims;

namespace PpecbAssessment.WebApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(IProductService productService, IWebHostEnvironment environment)
        {
            _productService = productService;
            _environment = environment;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var item = await _productService.GetByIdAsync(id, userId);
            if (item == null)
                return NotFound(new { message = "Product not found." });

            return Ok(item);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateProductWithImageRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var auditUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Product name is required." });

            if (!decimal.TryParse(request.Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrice))
                return BadRequest(new { message = "Price is invalid." });

            if (parsedPrice < 0)
                return BadRequest(new { message = "Price must be greater than or equal to zero." });

            var serviceRequest = new CreateProductRequest
            {
                Name = request.Name,
                Description = request.Description,
                Price = parsedPrice,
                CategoryId = request.CategoryId
            };

            var uploadsRootPath = Path.Combine(_environment.WebRootPath, "uploads");

            var result = await _productService.CreateProductAsync(
                serviceRequest,
                userId,
                auditUser,
                uploadsRootPath,
                request.Image);

            if (!result.Succeeded)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                message = result.Message,
                productId = result.Data
            });
        }
        [HttpPut("{id:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateProductWithImageRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var auditUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (!decimal.TryParse(request.Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrice))
            {
                return BadRequest(new { message = "Price is invalid." });
            }

            var uploadsRootPath = Path.Combine(_environment.WebRootPath, "uploads");

            var serviceRequest = new UpdateProductRequest
            {
                Name = request.Name,
                Description = request.Description,
                Price = parsedPrice,
                CategoryId = request.CategoryId,
                RowVersion = request.RowVersion
            };

            var result = await _productService.UpdateProductAsync(
                id,
                serviceRequest,
                userId,
                auditUser,
                uploadsRootPath,
                request.Image);

            if (!result.Succeeded)
            {
                if (result.IsConflict)
                    return Conflict(new { message = result.Message });

                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _productService.GetPagedAsync(userId, pageNumber);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _productService.DeleteProductAsync(id, userId);

            if (!result.Succeeded)
                return NotFound(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var auditUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _productService.ImportProductsAsync(file, userId, auditUser);

            if (!result.Succeeded)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var fileBytes = await _productService.ExportProductsAsync(userId);

            var fileName = $"products-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
