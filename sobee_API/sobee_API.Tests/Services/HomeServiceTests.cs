using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.Services;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class HomeServiceTests
{
    [Fact]
    public async Task GetPingAsync_ReturnsStatusAndDbFlag()
    {
        var service = new HomeService(new FakeHealthRepository(canConnect: true));

        var result = await service.GetPingAsync();

        result.Success.Should().BeTrue();
        result.Value!.Status.Should().Be("ok");
        result.Value.Db.Should().BeTrue();
    }

    private sealed class FakeHealthRepository : ISystemHealthRepository
    {
        private readonly bool _canConnect;

        public FakeHealthRepository(bool canConnect)
        {
            _canConnect = canConnect;
        }

        public Task<bool> CanConnectAsync()
            => Task.FromResult(_canConnect);
    }
}
