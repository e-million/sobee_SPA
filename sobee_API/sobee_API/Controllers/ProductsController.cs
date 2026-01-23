using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;
using sobee_API.DTOs.Common;
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
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sort = null)
        {
            if (page <= 0)
                return BadRequest(new ApiErrorResponse("page must be >= 1", "ValidationError"));

            if (pageSize <= 0 || pageSize > 100)
                return BadRequest(new ApiErrorResponse("pageSize must be between 1 and 100", "ValidationError"));

            bool admin = IsAdmin();

            IQueryable<Tproduct> query = _db.Tproducts
                .AsNoTracking()
                .Include(p => p.TproductImages)
                .Include(p => p.IntDrinkCategory);

            // Search (name + description)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p =>
                    p.StrName.Contains(term) ||
                    p.strDescription.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var categoryTerm = category.Trim();
                query = query.Where(p =>
                    p.IntDrinkCategory != null &&
                    p.IntDrinkCategory.StrName == categoryTerm);
            }

            var totalCount = await query.CountAsync();

            var isSqlite = _db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
            var requiresClientSort = isSqlite && (sort == "priceAsc" || sort == "priceDesc");
            List<Tproduct> products;

            if (requiresClientSort)
            {
                // SQLite cannot order by decimal columns; sort in memory when needed.
                var allProducts = await query.ToListAsync();
                var ordered = sort == "priceAsc"
                    ? allProducts.OrderBy(p => p.DecPrice).ThenBy(p => p.IntProductId)
                    : allProducts.OrderByDescending(p => p.DecPrice).ThenBy(p => p.IntProductId);
                products = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                query = sort switch
                {
                    "priceAsc" => query.OrderBy(p => p.DecPrice),
                    "priceDesc" => query.OrderByDescending(p => p.DecPrice),
                    _ => query.OrderBy(p => p.IntProductId)
                };

                products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            var items = products.Select(p => new ProductListDto
            {
                Id = p.IntProductId,
                Name = p.StrName,
                Description = p.strDescription,
                Price = p.DecPrice,
                Cost = admin ? p.DecCost : null,

                InStock = p.IntStockAmount > 0,
                PrimaryImageUrl = GetPrimaryImageUrl(p),

                StockAmount = admin ? p.IntStockAmount : null,
                Category = p.IntDrinkCategory?.StrName,
                CategoryId = p.IntDrinkCategoryId
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
                .Include(p => p.IntDrinkCategory)
                .FirstOrDefaultAsync(p => p.IntProductId == id);

            if (product == null)
                return NotFound(new ApiErrorResponse("Product not found.", "NotFound"));

            var response = new
            {
                id = product.IntProductId,
                name = product.StrName,
                description = product.strDescription,
                price = product.DecPrice,

                inStock = product.IntStockAmount > 0,
                stockAmount = admin ? product.IntStockAmount : (int?)null,
                category = product.IntDrinkCategory?.StrName,
                categoryId = product.IntDrinkCategoryId,
                cost = admin ? product.DecCost : null,

                images = (product.TproductImages ?? [])
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
            var product = new Tproduct
            {
                StrName = request.Name.Trim(),
                strDescription = request.Description?.Trim() ?? "",
                DecPrice = request.Price,
                DecCost = request.Cost,
                IntStockAmount = request.StockAmount,
                IntDrinkCategoryId = request.CategoryId
            };

            _db.Tproducts.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.IntProductId }, new
            {
                id = product.IntProductId,
                name = product.StrName,
                description = product.strDescription,
                price = product.DecPrice,
                stockAmount = product.IntStockAmount,
                cost = product.DecCost,
                categoryId = product.IntDrinkCategoryId
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
            var productExists = await _db.Tproducts.AnyAsync(p => p.IntProductId == id);
            if (!productExists)
                return NotFound(new ApiErrorResponse("Product not found.", "NotFound"));

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
            var product = await _db.Tproducts.FirstOrDefaultAsync(p => p.IntProductId == id);
            if (product == null)
                return NotFound(new ApiErrorResponse("Product not found.", "NotFound"));

            if (request.Name != null)
            {
                product.StrName = request.Name.Trim();
            }

            if (request.Description != null)
            {
                product.strDescription = request.Description.Trim();
            }

            if (request.Price.HasValue)
            {
                product.DecPrice = request.Price.Value;
            }

            if (request.Cost.HasValue)
            {
                product.DecCost = request.Cost.Value;
            }

            if (request.StockAmount.HasValue)
            {
                product.IntStockAmount = request.StockAmount.Value;
            }

            if (request.CategoryId.HasValue)
            {
                product.IntDrinkCategoryId = request.CategoryId.Value;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = product.IntProductId,
                name = product.StrName,
                description = product.strDescription,
                price = product.DecPrice,
                stockAmount = product.IntStockAmount,
                cost = product.DecCost,
                categoryId = product.IntDrinkCategoryId
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
                return NotFound(new ApiErrorResponse("Product not found.", "NotFound"));

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
                return NotFound(new ApiErrorResponse("Image not found for that product.", "NotFound"));

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
