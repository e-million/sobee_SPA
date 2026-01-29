using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Reviews;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Products;

public partial class Tproduct
{
    [Key]
    public int IntProductId { get; set; }

    public string StrName { get; set; } = null!;

    public string strDescription { get; set; } = null!;

    public decimal DecPrice { get; set; }

    public decimal? DecCost { get; set; }

    public int IntStockAmount { get; set; }

    public int? IntDrinkCategoryId { get; set; }

    public bool BlnIsActive { get; set; } = true;

    public DateTime? DtmDateAdded { get; set; }

    public virtual ICollection<TcartItem> TcartItems { get; set; } = new List<TcartItem>();

    public virtual ICollection<Tfavorite> Tfavorites { get; set; } = new List<Tfavorite>();

    public virtual ICollection<TorderItem> TorderItems { get; set; } = new List<TorderItem>();

    public virtual ICollection<Treview> Treviews { get; set; } = new List<Treview>();

    public virtual TdrinkCategory? IntDrinkCategory { get; set; }

    // NEW: product images
    public virtual ICollection<TproductImage> TproductImages { get; set; } = new List<TproductImage>();
}
