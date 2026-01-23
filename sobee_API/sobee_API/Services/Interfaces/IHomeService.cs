using sobee_API.Domain;
using sobee_API.DTOs.Common;

namespace sobee_API.Services.Interfaces;

public interface IHomeService
{
    Task<ServiceResult<HealthCheckResponseDto>> GetPingAsync();
}
