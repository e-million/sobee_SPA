using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Orders;

public partial class TshippingStatus {
    [Key]
    public int IntShippingStatusId { get; set; }

    public string StrShippingStatus { get; set; } = null!;

    public virtual ICollection<Torder> Torders { get; set; } = new List<Torder>();
}
