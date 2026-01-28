using System.ComponentModel.DataAnnotations;

namespace sobee_API.DTOs.Categories;

public sealed class UpdateCategoryRequest
{
    [MaxLength(120)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
