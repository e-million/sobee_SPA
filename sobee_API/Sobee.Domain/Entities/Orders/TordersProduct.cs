using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Orders;

public partial class TordersProduct {

    [Key]
    public int IntOrdersProductId { get; set; }

    public int IntProductId { get; set; }

    public string StrOrdersProduct { get; set; } = null!;
}
