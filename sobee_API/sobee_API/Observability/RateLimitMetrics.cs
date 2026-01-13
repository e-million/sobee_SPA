using System.Diagnostics.Metrics;

namespace sobee_API.Observability
{
    public sealed class RateLimitMetrics
    {
        public const string MeterName = "sobee_API.RateLimiting";
        private readonly Counter<long> _rejectionCounter;

        public RateLimitMetrics(Meter meter)
        {
            _rejectionCounter = meter.CreateCounter<long>("rate_limit_rejections");
        }

        public void RecordRejection(HttpContext context)
        {
            _rejectionCounter.Add(
                1,
                new KeyValuePair<string, object?>("method", context.Request.Method),
                new KeyValuePair<string, object?>("path", context.Request.Path.Value ?? string.Empty));
        }
    }
}
