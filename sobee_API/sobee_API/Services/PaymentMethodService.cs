using sobee_API.Domain;
using sobee_API.DTOs.Payments;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class PaymentMethodService : IPaymentMethodService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentMethodService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<PaymentMethodResponseDto>>> GetPaymentMethodsAsync()
    {
        var methods = await _paymentRepository.GetMethodsAsync();
        var response = methods
            .Select(method => new PaymentMethodResponseDto
            {
                PaymentMethodId = method.IntPaymentMethodId,
                Description = method.StrDescription
            })
            .ToList();

        return ServiceResult<IReadOnlyList<PaymentMethodResponseDto>>.Ok(response);
    }
}
