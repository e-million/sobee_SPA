using System.ComponentModel.DataAnnotations;

namespace sobee_API.DTOs.Categories;

public sealed class CreateCategoryRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
