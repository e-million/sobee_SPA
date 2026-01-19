using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Products;

public partial class TdrinkCategory {
    [Key]
    public int IntDrinkCategoryId { get; set; }

    public string StrName { get; set; } = null!;

    public string StrDescription { get; set; } = null!;

    public virtual ICollection<Tproduct> Tproducts { get; set; } = new List<Tproduct>();
}
