using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sobee.Domain.Entities.Payments;

namespace Sobee.Domain.Entities.Orders;

public partial class Torder {
    [Key]
    public int IntOrderId { get; set; }

    public DateTime? DtmOrderDate { get; set; }

    public DateTime? DtmShippedDate { get; set; }

    public DateTime? DtmDeliveredDate { get; set; }

    public decimal? DecTotalAmount { get; set; }

    public decimal? DecTaxAmount { get; set; }

    public decimal? DecTaxRate { get; set; }

    public int? IntShippingStatusId { get; set; }

    public string? StrShippingAddress { get; set; }

    public string? StrBillingAddress { get; set; }

    public string? StrTrackingNumber { get; set; }

    public int? IntPaymentMethodId { get; set; }

    public string? StrOrderStatus { get; set; }

    public string? UserId { get; set; }

    public string? SessionId { get; set; }

    public virtual TpaymentMethod? IntPaymentMethod { get; set; }

    public virtual TshippingStatus? IntShippingStatus { get; set; }

    public virtual ICollection<TorderItem> TorderItems { get; set; } = new List<TorderItem>();


    public string? StrPromoCode { get; set; }
    public decimal? DecDiscountPercentage { get; set; }
    public decimal? DecDiscountAmount { get; set; }
    public decimal? DecSubtotalAmount { get; set; }


}
