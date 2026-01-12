using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;
using sobee_API.DTOs.Products;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly SobeecoredbContext _db;

        public ProductsController(SobeecoredbContext db)
        {
            _db = db;
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
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sort = null)
        {
            if (page <= 0)
                return BadRequest(new { error = "page must be >= 1" });

            if (pageSize <= 0 || pageSize > 100)
                return BadRequest(new { error = "pageSize must be between 1 and 100" });

            bool admin = IsAdmin();

            IQueryable<Tproduct> query = _db.Tproducts
                .AsNoTracking()
                .Include(p => p.TproductImages);

            // Search (name + description)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p =>
                    p.StrName.Contains(term) ||
                    p.strDescription.Contains(term));
            }

            // Sorting
            query = sort switch
            {
                "priceAsc" => query.OrderBy(p => p.DecPrice),
                "priceDesc" => query.OrderByDescending(p => p.DecPrice),
                _ => query.OrderBy(p => p.IntProductId)
            };

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = products.Select(p => new ProductListDto
            {
                Id = p.IntProductId,
                Name = p.StrName,
                Description = p.strDescription,
                Price = p.DecPrice,

                InStock = p.IntStockAmount > 0,
                PrimaryImageUrl = GetPrimaryImageUrl(p),

                StockAmount = admin ? p.IntStockAmount : null
            }).ToList();

            return Ok(new
            {
                page,
                pageSize,
                totalCount,
                items
            });
        }

        /// <summary>
        /// Get a single product + all images.
        /// Admin additionally sees StockAmount.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            bool admin = IsAdmin();

            var product = await _db.Tproducts
                .AsNoTracking()
                .Include(p => p.TproductImages)
                .FirstOrDefaultAsync(p => p.IntProductId == id);

            if (product == null)
                return NotFound(new { error = "Product not found." });

            var response = new
            {
                id = product.IntProductId,
                name = product.StrName,
                description = product.strDescription,
                price = product.DecPrice,

                inStock = product.IntStockAmount > 0,
                stockAmount = admin ? product.IntStockAmount : (int?)null,

                images = (product.TproductImages ?? new List<TproductImage>())
                    .OrderBy(i => i.IntProductImageId)
                    .Select(i => new
                    {
                        id = i.IntProductImageId,
                        url = i.StrProductImageUrl
                    })
                    .ToList()
            };

            return Ok(response);
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
            if (request == null)
                return BadRequest(new { error = "Missing request body." });

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Name is required." });

            if (request.Price < 0)
                return BadRequest(new { error = "Price cannot be negative." });

            if (request.StockAmount < 0)
                return BadRequest(new { error = "StockAmount cannot be negative." });

            var product = new Tproduct
            {
                StrName = request.Name.Trim(),
                strDescription = request.Description?.Trim() ?? "",
                DecPrice = request.Price,
                IntStockAmount = request.StockAmount
            };

            _db.Tproducts.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.IntProductId }, new
            {
                id = product.IntProductId,
                name = product.StrName,
                description = product.strDescription,
                price = product.DecPrice,
                stockAmount = product.IntStockAmount
            });
        }

        /// <summary>
        /// Admin-only: update an existing product.
        /// </summary>
        // ----------------------------
        // Admin Product Images
        // ----------------------------

        /// <summary>
        /// Admin-only: add an image to a product.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/images")]
        public async Task<IActionResult> AddProductImage(int id, [FromBody] AddProductImageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
                return BadRequest(new { error = "Missing required field: url" });

            var productExists = await _db.Tproducts.AnyAsync(p => p.IntProductId == id);
            if (!productExists)
                return NotFound(new { error = "Product not found." });

            var image = new TproductImage
            {
                IntProductId = id,
                StrProductImageUrl = request.Url.Trim()
            };

            _db.TproductImages.Add(image);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = image.IntProductImageId,
                productId = image.IntProductId,
                url = image.StrProductImageUrl
            });
        }

        /// <summary>
        /// Admin-only: update an existing product.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Missing request body." });

            var product = await _db.Tproducts.FirstOrDefaultAsync(p => p.IntProductId == id);
            if (product == null)
                return NotFound(new { error = "Product not found." });

            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { error = "Name cannot be empty." });

                product.StrName = request.Name.Trim();
            }

            if (request.Description != null)
            {
                product.strDescription = request.Description.Trim();
            }

            if (request.Price.HasValue)
            {
                if (request.Price.Value < 0)
                    return BadRequest(new { error = "Price cannot be negative." });

                product.DecPrice = request.Price.Value;
            }

            if (request.StockAmount.HasValue)
            {
                if (request.StockAmount.Value < 0)
                    return BadRequest(new { error = "StockAmount cannot be negative." });

                product.IntStockAmount = request.StockAmount.Value;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = product.IntProductId,
                name = product.StrName,
                description = product.strDescription,
                price = product.DecPrice,
                stockAmount = product.IntStockAmount
            });
        }

        /// <summary>
        /// Admin-only: delete a product and its images.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Tproducts
                .Include(p => p.TproductImages)
                .FirstOrDefaultAsync(p => p.IntProductId == id);

            if (product == null)
                return NotFound(new { error = "Product not found." });

            if (product.TproductImages != null && product.TproductImages.Count > 0)
                _db.TproductImages.RemoveRange(product.TproductImages);

            _db.Tproducts.Remove(product);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Product deleted." });
        }

        /// <summary>
        /// Admin-only: delete a product image.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{productId:int}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteProductImage(int productId, int imageId)
        {
            var image = await _db.TproductImages
                .FirstOrDefaultAsync(i => i.IntProductImageId == imageId && i.IntProductId == productId);

            if (image == null)
                return NotFound(new { error = "Image not found for that product." });

            _db.TproductImages.Remove(image);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Image deleted." });
        }

        // ----------------------------
        // Helpers
        // ----------------------------

        private bool IsAdmin()
        {
            return User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
        }

        private static string? GetPrimaryImageUrl(Tproduct product)
        {
            if (product.TproductImages == null || product.TproductImages.Count == 0)
                return null;

            // Deterministic rule: lowest image ID
            return product.TproductImages
                .OrderBy(i => i.IntProductImageId)
                .Select(i => i.StrProductImageUrl)
                .FirstOrDefault();
        }
    }
}
