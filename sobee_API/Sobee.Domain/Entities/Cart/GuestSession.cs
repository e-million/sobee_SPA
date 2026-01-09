using System;
using System.ComponentModel.DataAnnotations;

namespace Sobee.Domain.Entities.Cart;

public class GuestSession
{
    [Key]
    public string SessionId { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastSeenAtUtc { get; set; }
}
