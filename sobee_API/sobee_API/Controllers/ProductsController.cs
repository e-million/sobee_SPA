using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;

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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Tproducts
            .AsNoTracking()
            .Select(p => new
            {
                Id = p.IntProductId,
                Name = p.StrName,
                Description = p.strDescription,
                Price = p.DecPrice,
                Stock = p.StrStockAmount
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Tproducts
            .AsNoTracking()
            .Where(p => p.IntProductId == id)
            .Select(p => new
            {
                Id = p.IntProductId,
                Name = p.StrName,
                Description = p.strDescription,
                Price = p.DecPrice,
                Stock = p.StrStockAmount
            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound();

        return Ok(product);
    }
}
