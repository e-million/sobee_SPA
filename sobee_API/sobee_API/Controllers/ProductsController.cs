using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.DTOs.Common;
using sobee_API.DTOs.Products;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // ----------------------------
        // Public Endpoints
        // ----------------------------

        /// <summary>
        /// List products with paging, search, and sorting.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sort = null)
        {
            var result = await _productService.GetProductsAsync(
                q,
                category,
                page,
                pageSize,
                sort,
                IsAdmin());
            return FromServiceResult(result);
        }

        /// <summary>
        /// Get a single product + all images.
        /// Admin additionally sees StockAmount.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var result = await _productService.GetProductAsync(id, IsAdmin());
            return FromServiceResult(result);
        }

        // ----------------------------
        // Admin Product CRUD
        // ----------------------------

        /// <summary>
        /// Admin-only: create a new product.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var result = await _productService.CreateProductAsync(request);
            if (!result.Success)
            {
                return FromServiceResult(result);
            }

            return CreatedAtAction(nameof(GetProduct), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Admin-only: add an image to a product.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/images")]
        public async Task<IActionResult> AddProductImage(int id, [FromBody] AddProductImageRequest request)
        {
            var result = await _productService.AddProductImageAsync(id, request);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Admin-only: update an existing product.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            var result = await _productService.UpdateProductAsync(id, request);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Admin-only: delete a product and its images.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Admin-only: delete a product image.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{productId:int}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteProductImage(int productId, int imageId)
        {
            var result = await _productService.DeleteProductImageAsync(productId, imageId);
            return FromServiceResult(result);
        }

        // ----------------------------
        // Helpers
        // ----------------------------

        private bool IsAdmin()
        {
            return User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
        }

        private IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
            {
                return Ok(result.Value);
            }

            var code = result.ErrorCode ?? "ServerError";
            var message = result.ErrorMessage ?? "An unexpected error occurred.";

            return code switch
            {
                "NotFound" => NotFound(new ApiErrorResponse(message, code, result.ErrorData)),
                "ValidationError" => BadRequest(new ApiErrorResponse(message, code, result.ErrorData)),
                "Unauthorized" => Unauthorized(new ApiErrorResponse(message, code, result.ErrorData)),
                "Forbidden" => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(message, code, result.ErrorData)),
                "Conflict" => Conflict(new ApiErrorResponse(message, code, result.ErrorData)),
                "InsufficientStock" => Conflict(new ApiErrorResponse(message, code, result.ErrorData)),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse(message, code, result.ErrorData))
            };
        }
    }
}
