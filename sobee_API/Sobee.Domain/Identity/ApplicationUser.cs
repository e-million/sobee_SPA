using Microsoft.AspNetCore.Identity;
using Sobee.Domain.Entities.Cart;

namespace Sobee.Domain.Identity {
    public class ApplicationUser : IdentityUser
    {
        // Keep these NOT NULL in DB by ensuring they are never null in the CLR model.
        public string strShippingAddress { get; set; } = string.Empty;
        public string strBillingAddress { get; set; } = string.Empty;
        public string strFirstName { get; set; } = string.Empty;
        public string strLastName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginDate { get; set; } = DateTime.UtcNow;
    }
}
