using sobee_API.Domain;
using sobee_API.DTOs.Payments;

namespace sobee_API.Services.Interfaces;

public interface IPaymentMethodService
{
    Task<ServiceResult<IReadOnlyList<PaymentMethodResponseDto>>> GetPaymentMethodsAsync();
}
