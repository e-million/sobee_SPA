using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace sobee_API.Tests;

public static class TestSessionHeaders
{
    public const string SessionIdHeader = "X-Session-Id";
    public const string SessionSecretHeader = "X-Session-Secret";

    public static (string sessionId, string sessionSecret) GetSessionHeaders(HttpResponseMessage response)
    {
        var sessionId = response.Headers.GetValues(SessionIdHeader).Single();
        var sessionSecret = response.Headers.GetValues(SessionSecretHeader).Single();
        return (sessionId, sessionSecret);
    }

    public static void AddSessionHeaders(HttpRequestMessage request, string sessionId, string sessionSecret)
    {
        request.Headers.Add(SessionIdHeader, sessionId);
        request.Headers.Add(SessionSecretHeader, sessionSecret);
    }

    public static bool TryGetSessionHeaders(HttpResponseMessage response, out IReadOnlyList<string> values)
    {
        var hasSessionId = response.Headers.TryGetValues(SessionIdHeader, out var sessionIds);
        var hasSecret = response.Headers.TryGetValues(SessionSecretHeader, out var secrets);
        if (hasSessionId && hasSecret)
        {
            values = sessionIds.Concat(secrets).ToArray();
            return true;
        }

        values = new string[0];
        return false;
    }
}
