using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Payments;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class PaymentRepositoryTests
{
    [Fact]
    public async Task FindMethodAsync_ReturnsMethod()
    {
        using var context = new SqliteTestContext();
        context.AddMethod(CreateMethod(1, "Visa"));

        var result = await context.Repository.FindMethodAsync(1);

        result.Should().NotBeNull();
        result!.StrDescription.Should().Be("Visa");
    }

    [Fact]
    public async Task GetMethodsAsync_ReturnsOrderedMethods()
    {
        using var context = new SqliteTestContext();
        context.AddMethod(CreateMethod(2, "Mastercard"));
        context.AddMethod(CreateMethod(1, "Visa"));

        var result = await context.Repository.GetMethodsAsync();

        result.Should().HaveCount(2);
        result[0].IntPaymentMethodId.Should().Be(1);
        result[1].IntPaymentMethodId.Should().Be(2);
    }

    private static TpaymentMethod CreateMethod(int id, string description)
        => new()
        {
            IntPaymentMethodId = id,
            StrDescription = description,
            StrBillingAddress = "123 Main St",
            StrCreditCardDetails = "4111111111111111"
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public PaymentRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new PaymentRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public TpaymentMethod AddMethod(TpaymentMethod method)
        {
            DbContext.TpaymentMethods.Add(method);
            DbContext.SaveChanges();
            return method;
        }
    }
}
