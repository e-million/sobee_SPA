using sobee_API.Domain;
using sobee_API.DTOs.Common;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class HomeService : IHomeService
{
    private readonly ISystemHealthRepository _healthRepository;

    public HomeService(ISystemHealthRepository healthRepository)
    {
        _healthRepository = healthRepository;
    }

    public async Task<ServiceResult<HealthCheckResponseDto>> GetPingAsync()
    {
        var canConnect = await _healthRepository.CanConnectAsync();
        return ServiceResult<HealthCheckResponseDto>.Ok(new HealthCheckResponseDto
        {
            Status = "ok",
            Db = canConnect
        });
    }
}
