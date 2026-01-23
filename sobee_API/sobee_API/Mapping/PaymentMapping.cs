using sobee_API.DTOs.Payments;
using Sobee.Domain.Entities.Payments;

namespace sobee_API.Mapping;

public static class PaymentMapping
{
    public static PaymentMethodResponseDto ToPaymentMethodResponseDto(this TpaymentMethod method)
    {
        return new PaymentMethodResponseDto
        {
            PaymentMethodId = method.IntPaymentMethodId,
            Description = method.StrDescription
        };
    }
}
