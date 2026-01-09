using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;

namespace sobee_API.Services;

public class GuestSessionService
{
    public const string SessionIdHeaderName = "X-Session-Id";
    public const string SessionSecretHeaderName = "X-Session-Secret";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    private readonly SobeecoredbContext _db;
    private readonly ILogger<GuestSessionService> _logger;

    public GuestSessionService(SobeecoredbContext db, ILogger<GuestSessionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<GuestSessionResolution> ResolveAsync(HttpRequest request, HttpResponse response, bool allowCreate)
    {
        var sessionId = GetHeaderValue(request, SessionIdHeaderName);
        var secret = GetHeaderValue(request, SessionSecretHeaderName);

        if (!string.IsNullOrWhiteSpace(sessionId) && !Guid.TryParse(sessionId, out _))
        {
            _logger.LogWarning("Ignoring guest session id with invalid format.");
            sessionId = null;
            secret = null;
        }

        var now = DateTime.UtcNow;

        // Only accept a guest session when both session id + secret match a live record.
        if (!string.IsNullOrWhiteSpace(sessionId) && !string.IsNullOrWhiteSpace(secret))
        {
            var existing = await _db.GuestSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId
                                          && s.Secret == secret
                                          && s.ExpiresAtUtc > now);

            if (existing != null)
            {
                existing.LastSeenAtUtc = now;
                await _db.SaveChangesAsync();

                return GuestSessionResolution.Validated(existing.SessionId, existing.Secret);
            }
        }

        if (!allowCreate)
        {
            return GuestSessionResolution.Invalid(sessionId);
        }

        var newSessionId = Guid.NewGuid().ToString();
        var newSecret = GenerateSecret();
        var newSession = new GuestSession
        {
            SessionId = newSessionId,
            Secret = newSecret,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now.Add(SessionLifetime)
        };

        _db.GuestSessions.Add(newSession);
        await _db.SaveChangesAsync();

        response.Headers[SessionIdHeaderName] = newSessionId;
        response.Headers[SessionSecretHeaderName] = newSecret;

        return GuestSessionResolution.New(newSessionId, newSecret);
    }

    public async Task InvalidateAsync(string sessionId)
    {
        var existing = await _db.GuestSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (existing == null)
        {
            return;
        }

        _db.GuestSessions.Remove(existing);
        await _db.SaveChangesAsync();
    }

    private static string? GetHeaderValue(HttpRequest request, string headerName)
    {
        if (!request.Headers.TryGetValue(headerName, out var values))
        {
            return null;
        }

        var raw = values.ToString();
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}

public record GuestSessionResolution(
    string? SessionId,
    string? Secret,
    bool WasValidated,
    bool WasIssued)
{
    public bool HasSession => !string.IsNullOrWhiteSpace(SessionId);

    public static GuestSessionResolution Validated(string sessionId, string secret)
        => new(sessionId, secret, WasValidated: true, WasIssued: false);

    public static GuestSessionResolution New(string sessionId, string secret)
        => new(sessionId, secret, WasValidated: false, WasIssued: true);

    public static GuestSessionResolution Invalid(string? sessionId)
        => new(sessionId, null, WasValidated: false, WasIssued: false);
}
