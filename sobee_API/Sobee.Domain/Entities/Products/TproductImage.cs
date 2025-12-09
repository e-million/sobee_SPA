using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Products;

public partial class TproductImage {
    [Key]
    public int IntProductImageId { get; set; }

    public string StrProductImageUrl { get; set; } = null!;
}
