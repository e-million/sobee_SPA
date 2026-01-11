using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodsController : ControllerBase
    {
        private readonly SobeecoredbContext _db;

        public PaymentMethodsController(SobeecoredbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lists available payment methods.
        /// IMPORTANT: Do NOT return card details or billing addresses.
        /// </summary>
        [HttpGet]
        [Authorize] // keep it auth-only for now; easy to relax later
        public async Task<IActionResult> GetPaymentMethods()
        {
            var methods = await _db.TpaymentMethods
                .AsNoTracking()
                .Select(pm => new
                {
                    paymentMethodId = pm.IntPaymentMethodId,
                    description = pm.StrDescription
                })
                .ToListAsync();

            return Ok(methods);
        }
    }
}
