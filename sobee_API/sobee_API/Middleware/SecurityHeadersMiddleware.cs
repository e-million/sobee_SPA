namespace sobee_API.Middleware
{
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                TryAddHeader(headers, "X-Content-Type-Options", "nosniff");
                TryAddHeader(headers, "X-Frame-Options", "DENY");
                TryAddHeader(headers, "Referrer-Policy", "no-referrer");
                TryAddHeader(headers, "Permissions-Policy", "geolocation=(), microphone=(), camera=()");

                return Task.CompletedTask;
            });

            return _next(context);
        }

        private static void TryAddHeader(IHeaderDictionary headers, string name, string value)
        {
            if (!headers.ContainsKey(name))
            {
                headers[name] = value;
            }
        }
    }
}
