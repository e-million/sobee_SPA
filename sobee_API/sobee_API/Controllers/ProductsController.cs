using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;
using sobee_API.DTOs;

namespace sobee_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly SobeecoredbContext _db;

    public ProductsController(SobeecoredbContext db)
    {
        _db = db;
    }

    // =========================================================
    // 1) GET ALL PRODUCTS
    //    Optional query params:
    //      - search: substring match on name
    //      - minPrice, maxPrice
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        var query = _db.Tproducts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.StrName.Contains(s));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.DecPrice >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.DecPrice <= maxPrice.Value);

        var products = await query
            .OrderBy(p => p.IntProductId)
            .Select(p => new ProductListDto
            {
                Id = p.IntProductId,
                Name = p.StrName,
                Description = p.strDescription,
                Price = p.DecPrice,
                Stock = p.IntStockAmount
            })
            .ToListAsync();

        return Ok(products);
    }

    // =========================================================
    // 2) GET PRODUCT BY ID
    // =========================================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Tproducts
            .AsNoTracking()
            .Where(p => p.IntProductId == id)
            .Select(p => new ProductListDto
            {
                Id = p.IntProductId,
                Name = p.StrName,
                Description = p.strDescription,
                Price = p.DecPrice,
                Stock = p.IntStockAmount
            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound(new { error = "Product not found." });

        return Ok(product);
    }

    // =========================================================
    // 3) CREATE PRODUCT (ADMIN ONLY)
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Request body is required." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { error = "Description is required." });

        if (request.Price < 0)
            return BadRequest(new { error = "Price must be 0 or greater." });

        if (request.Stock < 0)
            return BadRequest(new { error = "Stock must be 0 or greater." });

        var entity = new Tproduct
        {
            StrName = request.Name.Trim(),
            strDescription = request.Description.Trim(),
            DecPrice = request.Price,
            IntStockAmount = request.Stock
        };

        _db.Tproducts.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.IntProductId }, new ProductListDto
        {
            Id = entity.IntProductId,
            Name = entity.StrName,
            Description = entity.strDescription,
            Price = entity.DecPrice,
            Stock = entity.IntStockAmount
        });
    }

    // =========================================================
    // 4) UPDATE PRODUCT (ADMIN ONLY)
    // =========================================================
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Request body is required." });

        var entity = await _db.Tproducts.FirstOrDefaultAsync(p => p.IntProductId == id);
        if (entity == null)
            return NotFound(new { error = "Product not found." });

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Name cannot be empty." });

            entity.StrName = request.Name.Trim();
        }

        if (request.Description != null)
        {
            if (string.IsNullOrWhiteSpace(request.Description))
                return BadRequest(new { error = "Description cannot be empty." });

            entity.strDescription = request.Description.Trim();
        }

        if (request.Price.HasValue)
        {
            if (request.Price.Value < 0)
                return BadRequest(new { error = "Price must be 0 or greater." });

            entity.DecPrice = request.Price.Value;
        }

        if (request.Stock.HasValue)
        {
            if (request.Stock.Value < 0)
                return BadRequest(new { error = "Stock must be 0 or greater." });

            entity.IntStockAmount = request.Stock.Value;
        }

        await _db.SaveChangesAsync();

        return Ok(new ProductListDto
        {
            Id = entity.IntProductId,
            Name = entity.StrName,
            Description = entity.strDescription,
            Price = entity.DecPrice,
            Stock = entity.IntStockAmount
        });
    }

    // =========================================================
    // 5) DELETE PRODUCT (ADMIN ONLY)
    // =========================================================
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Tproducts.FirstOrDefaultAsync(p => p.IntProductId == id);
        if (entity == null)
            return NotFound(new { error = "Product not found." });

        _db.Tproducts.Remove(entity);
        await _db.SaveChangesAsync();

        return Ok(new { deleted = true, id });
    }

    // Request DTOs
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? Stock { get; set; }
    }
}
