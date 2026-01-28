using Microsoft.AspNetCore.Mvc;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// List all product categories.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _categoryService.GetCategoriesAsync();
        return FromServiceResult(result);
    }
}
