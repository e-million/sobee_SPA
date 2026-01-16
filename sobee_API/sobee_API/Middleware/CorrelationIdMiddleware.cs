using Microsoft.Extensions.Primitives;

namespace sobee_API.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string ItemKey = "CorrelationId";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context.Request.Headers);
            context.Items[ItemKey] = correlationId;

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderName))
                {
                    context.Response.Headers[HeaderName] = correlationId;
                }

                return Task.CompletedTask;
            });

            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                [ItemKey] = correlationId
            });

            await _next(context);
        }

        private static string ResolveCorrelationId(IHeaderDictionary headers)
        {
            if (headers.TryGetValue(HeaderName, out StringValues values))
            {
                var candidate = values.ToString();
                if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 64)
                {
                    return candidate;
                }
            }

            return Guid.NewGuid().ToString();
        }
    }
}
