using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Payments;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Entities.Promotions;

namespace sobee_API.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _coreConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _identityConnection = new("Data Source=:memory:");

    public TestWebApplicationFactory()
    {
        _coreConnection.Open();
        _identityConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var coreDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SobeecoredbContext>));
            if (coreDescriptor != null)
            {
                services.Remove(coreDescriptor);
            }

            var identityDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (identityDescriptor != null)
            {
                services.Remove(identityDescriptor);
            }

            services.AddDbContext<SobeecoredbContext>(options =>
                options.UseSqlite(_coreConnection));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_identityConnection));

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var coreDb = scope.ServiceProvider.GetRequiredService<SobeecoredbContext>();
            var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            coreDb.Database.EnsureCreated();
            identityDb.Database.EnsureCreated();

            SeedCoreData(coreDb);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _coreConnection.Dispose();
            _identityConnection.Dispose();
        }
    }

    private static void SeedCoreData(SobeecoredbContext db)
    {
        if (!db.Tproducts.Any())
        {
            db.Tproducts.Add(new Tproduct
            {
                IntProductId = 1,
                StrName = "Test Product",
                strDescription = "Test Description",
                DecPrice = 2.50m,
                IntStockAmount = 50
            });
        }

        if (!db.TpaymentMethods.Any())
        {
            db.TpaymentMethods.Add(new TpaymentMethod
            {
                IntPaymentMethodId = 1,
                StrCreditCardDetails = "TEST-4111",
                StrBillingAddress = "123 Test Lane",
                StrDescription = "Test Card"
            });
        }

        if (!db.Tpromotions.Any())
        {
            db.Tpromotions.Add(new Tpromotion
            {
                IntPromotionId = 1,
                StrPromoCode = "PROMO10",
                StrDiscountPercentage = "10%",
                DecDiscountPercentage = 10m,
                DtmExpirationDate = DateTime.UtcNow.AddYears(10)
            });
        }

        db.SaveChanges();
    }
}
