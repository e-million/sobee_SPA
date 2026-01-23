using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.DTOs.Admin;
using sobee_API.Services;
using Sobee.Domain.Entities.Promotions;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class AdminPromoServiceTests
{
    [Fact]
    public async Task GetPromosAsync_InvalidPage_ReturnsValidationError()
    {
        var service = new AdminPromoService(new FakeAdminPromoRepository());

        var result = await service.GetPromosAsync(null, includeExpired: false, page: 0, pageSize: 20);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetPromosAsync_ReturnsPagedResultsWithUsageCounts()
    {
        var repo = new FakeAdminPromoRepository();
        repo.AddPromo(new Tpromotion
        {
            IntPromotionId = 1,
            StrPromoCode = "SAVE10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = DateTime.Today.AddDays(5)
        }, usageCount: 2);
        repo.AddPromo(new Tpromotion
        {
            IntPromotionId = 2,
            StrPromoCode = "OLD",
            DecDiscountPercentage = 5m,
            DtmExpirationDate = DateTime.Today.AddDays(-1)
        }, usageCount: 1);

        var service = new AdminPromoService(repo);

        var result = await service.GetPromosAsync(null, includeExpired: true, page: 1, pageSize: 10);

        result.Success.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().ContainSingle(p => p.Code == "SAVE10" && p.UsageCount == 2);
    }

    [Fact]
    public async Task CreatePromoAsync_DuplicateCode_ReturnsConflict()
    {
        var repo = new FakeAdminPromoRepository();
        repo.AddPromo(new Tpromotion
        {
            IntPromotionId = 1,
            StrPromoCode = "SAVE10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = DateTime.Today.AddDays(5)
        });

        var service = new AdminPromoService(repo);

        var result = await service.CreatePromoAsync(new CreatePromoRequest
        {
            Code = "save10",
            DiscountPercentage = 10m,
            ExpirationDate = DateTime.Today.AddDays(1)
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("Conflict");
    }

    [Fact]
    public async Task CreatePromoAsync_ValidData_CreatesPromo()
    {
        var repo = new FakeAdminPromoRepository();
        var service = new AdminPromoService(repo);

        var result = await service.CreatePromoAsync(new CreatePromoRequest
        {
            Code = "new10",
            DiscountPercentage = 10m,
            ExpirationDate = DateTime.Today.AddDays(1)
        });

        result.Success.Should().BeTrue();
        result.Value!.Code.Should().Be("NEW10");
        repo.Promos.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdatePromoAsync_NotFound_ReturnsNotFound()
    {
        var service = new AdminPromoService(new FakeAdminPromoRepository());

        var result = await service.UpdatePromoAsync(999, new UpdatePromoRequest { Code = "NOPE" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdatePromoAsync_DuplicateCode_ReturnsConflict()
    {
        var repo = new FakeAdminPromoRepository();
        repo.AddPromo(new Tpromotion
        {
            IntPromotionId = 1,
            StrPromoCode = "SAVE10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = DateTime.Today.AddDays(5)
        });
        repo.AddPromo(new Tpromotion
        {
            IntPromotionId = 2,
            StrPromoCode = "SAVE20",
            DecDiscountPercentage = 20m,
            DtmExpirationDate = DateTime.Today.AddDays(5)
        });
        var service = new AdminPromoService(repo);

        var result = await service.UpdatePromoAsync(2, new UpdatePromoRequest { Code = "SAVE10" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeletePromoAsync_Existing_RemovesPromo()
    {
        var repo = new FakeAdminPromoRepository();
        repo.AddPromo(new Tpromotion
        {
            IntPromotionId = 1,
            StrPromoCode = "SAVE10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = DateTime.Today.AddDays(5)
        });
        var service = new AdminPromoService(repo);

        var result = await service.DeletePromoAsync(1);

        result.Success.Should().BeTrue();
        repo.Promos.Should().BeEmpty();
    }

    private sealed class FakeAdminPromoRepository : IAdminPromoRepository
    {
        private readonly List<Tpromotion> _promos = new();
        private readonly Dictionary<string, int> _usageCounts = new(StringComparer.OrdinalIgnoreCase);
        private int _nextId = 1;

        public IReadOnlyList<Tpromotion> Promos => _promos;

        public void AddPromo(Tpromotion promo, int usageCount = 0)
        {
            if (promo.IntPromotionId == 0)
            {
                promo.IntPromotionId = _nextId++;
            }

            if (string.IsNullOrWhiteSpace(promo.StrPromoCode))
            {
                promo.StrPromoCode = $"CODE{promo.IntPromotionId}";
            }

            _promos.Add(promo);
            _usageCounts[promo.StrPromoCode] = usageCount;
        }

        public Task<(IReadOnlyList<Tpromotion> Items, int TotalCount)> GetPromosAsync(
            string? search,
            bool includeExpired,
            int page,
            int pageSize)
        {
            IEnumerable<Tpromotion> query = _promos;

            if (!includeExpired)
            {
                var today = DateTime.Today;
                query = query.Where(p => p.DtmExpirationDate >= today);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.StrPromoCode.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var total = query.Count();
            query = query
                .OrderByDescending(p => p.DtmExpirationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return Task.FromResult(((IReadOnlyList<Tpromotion>)query.ToList(), total));
        }

        public Task<IReadOnlyList<(string Code, int Count)>> GetUsageCountsAsync(IReadOnlyList<string> codes)
        {
            IReadOnlyList<(string Code, int Count)> results = codes
                .Select(code => (code, _usageCounts.TryGetValue(code, out var count) ? count : 0))
                .ToList();
            return Task.FromResult(results);
        }

        public Task<int> CountUsageAsync(string promoCode)
            => Task.FromResult(_usageCounts.TryGetValue(promoCode, out var count) ? count : 0);

        public Task<Tpromotion?> FindByIdAsync(int promoId, bool track = true)
            => Task.FromResult(_promos.FirstOrDefault(p => p.IntPromotionId == promoId));

        public Task<bool> ExistsByCodeAsync(string code, int? excludePromoId = null)
        {
            var query = _promos.AsEnumerable();
            if (excludePromoId.HasValue)
            {
                query = query.Where(p => p.IntPromotionId != excludePromoId.Value);
            }

            var exists = query.Any(p => string.Equals(p.StrPromoCode, code, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task AddAsync(Tpromotion promo)
        {
            AddPromo(promo);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Tpromotion promo)
        {
            _promos.Remove(promo);
            _usageCounts.Remove(promo.StrPromoCode);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }
}
