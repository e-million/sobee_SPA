using sobee_API.Domain;
using sobee_API.DTOs.Cart;
using sobee_API.DTOs.Categories;
using sobee_API.DTOs.Common;

namespace sobee_API.Services.Interfaces;

public interface IAdminCategoryService
{
    Task<ServiceResult<IReadOnlyList<CategoryResponseDto>>> GetCategoriesAsync();
    Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request);
    Task<ServiceResult<MessageResponseDto>> DeleteCategoryAsync(int categoryId);
}
