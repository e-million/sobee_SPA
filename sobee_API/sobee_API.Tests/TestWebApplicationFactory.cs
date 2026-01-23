using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
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
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });

            services.PostConfigure<RateLimiterOptions>(options =>
            {
                // Disable rate limiting for tests to avoid 429s in fast suites.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("tests"));
            });

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
        if (!db.TdrinkCategories.Any())
        {
            db.TdrinkCategories.Add(new TdrinkCategory
            {
                IntDrinkCategoryId = 1,
                StrName = "Tea",
                StrDescription = "Tea"
            });
        }

        if (!db.Tproducts.Any())
        {
            db.Tproducts.AddRange(
                new Tproduct
                {
                    IntProductId = 1,
                    StrName = "Test Product",
                    strDescription = "Test Description",
                    DecPrice = 2.50m,
                    DecCost = 1.00m,
                    IntDrinkCategoryId = 1,
                    IntStockAmount = 50
                },
                new Tproduct
                {
                    IntProductId = 2,
                    StrName = "Low Stock Product",
                    strDescription = "Low Stock",
                    DecPrice = 1.25m,
                    IntStockAmount = 1
                },
                new Tproduct
                {
                    IntProductId = 3,
                    StrName = "Out Of Stock Product",
                    strDescription = "Out Of Stock",
                    DecPrice = 3.00m,
                    IntStockAmount = 0
                });
        }

        if (!db.TproductImages.Any())
        {
            db.TproductImages.Add(new TproductImage
            {
                IntProductImageId = 1,
                IntProductId = 1,
                StrProductImageUrl = "https://example.com/image-1.jpg"
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
            db.Tpromotions.AddRange(
                new Tpromotion
                {
                    IntPromotionId = 1,
                    StrPromoCode = "PROMO10",
                    StrDiscountPercentage = "10%",
                    DecDiscountPercentage = 10m,
                    DtmExpirationDate = DateTime.UtcNow.AddYears(10)
                },
                new Tpromotion
                {
                    IntPromotionId = 2,
                    StrPromoCode = "EXPIRED10",
                    StrDiscountPercentage = "10%",
                    DecDiscountPercentage = 10m,
                    DtmExpirationDate = DateTime.UtcNow.AddDays(-1)
                });
        }

        db.SaveChanges();
    }
}
