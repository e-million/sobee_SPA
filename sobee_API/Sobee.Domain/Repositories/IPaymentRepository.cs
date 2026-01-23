using Sobee.Domain.Entities.Payments;

namespace Sobee.Domain.Repositories;

public interface IPaymentRepository
{
    Task<TpaymentMethod?> FindMethodAsync(int paymentMethodId);
    Task AddAsync(Tpayment payment);
    Task SaveChangesAsync();
}
