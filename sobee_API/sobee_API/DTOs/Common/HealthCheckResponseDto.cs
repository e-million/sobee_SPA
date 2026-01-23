namespace sobee_API.DTOs.Common
{
    public sealed class HealthCheckResponseDto
    {
        public string Status { get; set; } = "ok";
        public bool Db { get; set; }
    }
}
