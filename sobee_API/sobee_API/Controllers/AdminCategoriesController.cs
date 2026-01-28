using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.DTOs.Categories;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public sealed class AdminCategoriesController : ApiControllerBase
{
    private readonly IAdminCategoryService _categoryService;

    public AdminCategoriesController(IAdminCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _categoryService.GetCategoriesAsync();
        return FromServiceResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var result = await _categoryService.CreateCategoryAsync(request);
        return FromServiceResult(result);
    }

    [HttpPut("{categoryId:int}")]
    public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateCategoryAsync(categoryId, request);
        return FromServiceResult(result);
    }

    [HttpDelete("{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var result = await _categoryService.DeleteCategoryAsync(categoryId);
        return FromServiceResult(result);
    }

    [HttpDelete("{categoryId:int}/force")]
    public async Task<IActionResult> DeleteCategoryForce(int categoryId)
    {
        var result = await _categoryService.DeleteCategoryAndReassignAsync(categoryId);
        return FromServiceResult(result);
    }
}
