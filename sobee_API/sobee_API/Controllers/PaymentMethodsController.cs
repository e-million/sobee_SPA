using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodsController : ApiControllerBase
    {
        private readonly IPaymentMethodService _paymentMethodService;

        public PaymentMethodsController(IPaymentMethodService paymentMethodService)
        {
            _paymentMethodService = paymentMethodService;
        }

        /// <summary>
        /// Lists available payment methods.
        /// IMPORTANT: Do NOT return card details or billing addresses.
        /// </summary>
        [HttpGet]
        [Authorize] // keep it auth-only for now; easy to relax later
        public async Task<IActionResult> GetPaymentMethods()
        {
            var result = await _paymentMethodService.GetPaymentMethodsAsync();
            return FromServiceResult(result);
        }

    }
}
