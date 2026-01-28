using System.Linq;
using sobee_API.Domain;
using sobee_API.DTOs.Categories;
using sobee_API.DTOs.Common;
using sobee_API.Mapping;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;
using sobee_API.DTOs.Cart;

namespace sobee_API.Services;

public sealed class AdminCategoryService : IAdminCategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public AdminCategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryResponseDto>>> GetCategoriesAsync()
    {
        var categories = await _categoryRepository.GetCategoriesAsync();
        var response = categories
            .Select(category => category.ToCategoryResponseDto())
            .ToList();

        return ServiceResult<IReadOnlyList<CategoryResponseDto>>.Ok(response);
    }

    public async Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var name = NormalizeName(request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return Validation<CategoryResponseDto>("Category name is required.", null);
        }

        if (await _categoryRepository.ExistsByNameAsync(name))
        {
            return Conflict<CategoryResponseDto>("Category name already exists.", null);
        }

        var category = new TdrinkCategory
        {
            StrName = name,
            StrDescription = NormalizeDescription(request.Description)
        };

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return ServiceResult<CategoryResponseDto>.Ok(category.ToCategoryResponseDto());
    }

    public async Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.FindByIdAsync(categoryId, track: true);
        if (category == null)
        {
            return NotFound<CategoryResponseDto>("Category not found.", null);
        }

        if (request.Name != null)
        {
            var name = NormalizeName(request.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                return Validation<CategoryResponseDto>("Category name is required.", null);
            }

            if (await _categoryRepository.ExistsByNameAsync(name, categoryId))
            {
                return Conflict<CategoryResponseDto>("Category name already exists.", null);
            }

            category.StrName = name;
        }

        if (request.Description != null)
        {
            category.StrDescription = NormalizeDescription(request.Description);
        }

        await _categoryRepository.SaveChangesAsync();

        return ServiceResult<CategoryResponseDto>.Ok(category.ToCategoryResponseDto());
    }

    public async Task<ServiceResult<MessageResponseDto>> DeleteCategoryAsync(int categoryId)
    {
        var category = await _categoryRepository.FindByIdAsync(categoryId, track: true);
        if (category == null)
        {
            return NotFound<MessageResponseDto>("Category not found.", null);
        }

        if (await _categoryRepository.HasProductsAsync(categoryId))
        {
            return Conflict<MessageResponseDto>("Category has products and cannot be deleted.", null);
        }

        await _categoryRepository.RemoveAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return ServiceResult<MessageResponseDto>.Ok(new MessageResponseDto
        {
            Message = "Category deleted."
        });
    }

    private static string NormalizeName(string name)
        => name.Trim();

    private static string NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);

    private static ServiceResult<T> Conflict<T>(string message, object? data)
        => ServiceResult<T>.Fail("Conflict", message, data);
}
