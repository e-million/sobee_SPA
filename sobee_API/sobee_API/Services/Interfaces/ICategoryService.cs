using sobee_API.Domain;

namespace sobee_API.Services.Interfaces;

public interface ICategoryService
{
    Task<ServiceResult<IReadOnlyList<string>>> GetCategoriesAsync();
}
