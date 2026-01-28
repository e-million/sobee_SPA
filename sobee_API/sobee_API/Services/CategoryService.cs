using System.Linq;
using sobee_API.Domain;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<string>>> GetCategoriesAsync()
    {
        var categories = await _categoryRepository.GetCategoriesAsync();
        var names = categories
            .Select(c => c.StrName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        return ServiceResult<IReadOnlyList<string>>.Ok(names);
    }
}
