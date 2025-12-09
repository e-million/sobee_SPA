using Sobee.Domain.Entities.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Cart;

public partial class TcartItem {
    [Key]
    public int IntCartItemId { get; set; }

    public int? IntShoppingCartId { get; set; }

    public int? IntProductId { get; set; }

    public int? IntQuantity { get; set; }

    public DateTime? DtmDateAdded { get; set; }

    public virtual Tproduct? IntProduct { get; set; }

    public virtual TshoppingCart? IntShoppingCart { get; set; }
}
