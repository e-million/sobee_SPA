using Sobee.Domain.Entities.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Reviews;

public partial class Tfavorite {
    [Key]
    public int IntFavoriteId { get; set; }

    public int IntProductId { get; set; }

    public DateTime DtmDateAdded { get; set; }

    public string? UserId { get; set; }

    public virtual Tproduct IntProduct { get; set; } = null!;
}
