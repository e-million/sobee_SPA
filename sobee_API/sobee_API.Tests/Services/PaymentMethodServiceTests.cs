using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.Services;
using Sobee.Domain.Entities.Payments;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class PaymentMethodServiceTests
{
    [Fact]
    public async Task GetPaymentMethodsAsync_ReturnsMappedMethods()
    {
        var repo = new FakePaymentRepository();
        repo.AddMethod(new TpaymentMethod { IntPaymentMethodId = 1, StrDescription = "Card" });
        repo.AddMethod(new TpaymentMethod { IntPaymentMethodId = 2, StrDescription = "Cash" });
        var service = new PaymentMethodService(repo);

        var result = await service.GetPaymentMethodsAsync();

        result.Success.Should().BeTrue();
        result.Value!.Select(m => m.Description).Should().Contain(new[] { "Card", "Cash" });
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        private readonly List<TpaymentMethod> _methods = new();

        public void AddMethod(TpaymentMethod method)
        {
            _methods.Add(method);
        }

        public Task<TpaymentMethod?> FindMethodAsync(int paymentMethodId)
            => Task.FromResult(_methods.FirstOrDefault(m => m.IntPaymentMethodId == paymentMethodId));

        public Task<IReadOnlyList<TpaymentMethod>> GetMethodsAsync()
            => Task.FromResult((IReadOnlyList<TpaymentMethod>)_methods);

        public Task AddAsync(Tpayment payment)
            => Task.CompletedTask;

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }
}
