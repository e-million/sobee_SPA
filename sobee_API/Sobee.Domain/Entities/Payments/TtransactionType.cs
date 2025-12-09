using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Payments;

public partial class TtransactionType {
    [Key]
    public int IntTransactionTypeId { get; set; }

    public string StrTransactionType { get; set; } = null!;

}
