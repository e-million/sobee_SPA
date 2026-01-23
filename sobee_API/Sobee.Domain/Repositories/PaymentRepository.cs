using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Payments;

namespace Sobee.Domain.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly SobeecoredbContext _db;

    public PaymentRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<TpaymentMethod?> FindMethodAsync(int paymentMethodId)
    {
        return await _db.TpaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.IntPaymentMethodId == paymentMethodId);
    }

    public async Task AddAsync(Tpayment payment)
    {
        _db.Tpayments.Add(payment);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
