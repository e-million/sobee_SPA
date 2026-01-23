using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using sobee_API.Services;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class GuestSessionServiceTests
{
    [Fact]
    public async Task ResolveAsync_InvalidSessionId_NoCreate_ReturnsInvalid()
    {
        var repository = new FakeGuestSessionRepository();
        var service = new GuestSessionService(repository, NullLogger<GuestSessionService>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers[GuestSessionService.SessionIdHeaderName] = "not-a-guid";
        context.Request.Headers[GuestSessionService.SessionSecretHeaderName] = "secret";

        var result = await service.ResolveAsync(context.Request, context.Response, allowCreate: false);

        result.WasValidated.Should().BeFalse();
        result.WasIssued.Should().BeFalse();
        result.SessionId.Should().BeNull();
        repository.FindValidCalls.Should().Be(0);
    }

    [Fact]
    public async Task ResolveAsync_ValidSession_ReturnsValidatedAndUpdatesLastSeen()
    {
        var repository = new FakeGuestSessionRepository();
        var sessionId = Guid.NewGuid().ToString();
        var secret = "secret";
        var existing = new GuestSession
        {
            SessionId = sessionId,
            Secret = secret,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            LastSeenAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
        repository.Add(existing);
        var service = new GuestSessionService(repository, NullLogger<GuestSessionService>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers[GuestSessionService.SessionIdHeaderName] = sessionId;
        context.Request.Headers[GuestSessionService.SessionSecretHeaderName] = secret;

        var result = await service.ResolveAsync(context.Request, context.Response, allowCreate: false);

        result.WasValidated.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        existing.LastSeenAtUtc.Should().BeAfter(DateTime.UtcNow.AddHours(-1));
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task ResolveAsync_AllowCreate_IssuesNewSessionAndWritesHeaders()
    {
        var repository = new FakeGuestSessionRepository();
        var service = new GuestSessionService(repository, NullLogger<GuestSessionService>.Instance);
        var context = new DefaultHttpContext();

        var result = await service.ResolveAsync(context.Request, context.Response, allowCreate: true);

        result.WasIssued.Should().BeTrue();
        result.SessionId.Should().NotBeNullOrWhiteSpace();
        context.Response.Headers[GuestSessionService.SessionIdHeaderName].ToString().Should().NotBeEmpty();
        context.Response.Headers[GuestSessionService.SessionSecretHeaderName].ToString().Should().NotBeEmpty();
        repository.AddCalls.Should().Be(1);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesSession()
    {
        var repository = new FakeGuestSessionRepository();
        var sessionId = Guid.NewGuid().ToString();
        repository.Add(new GuestSession
        {
            SessionId = sessionId,
            Secret = "secret",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            LastSeenAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });

        var service = new GuestSessionService(repository, NullLogger<GuestSessionService>.Instance);

        await service.InvalidateAsync(sessionId);

        repository.Sessions.Should().BeEmpty();
        repository.SaveChangesCalls.Should().Be(1);
    }

    private sealed class FakeGuestSessionRepository : IGuestSessionRepository
    {
        private readonly Dictionary<string, GuestSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

        public int FindValidCalls { get; private set; }
        public int AddCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public IReadOnlyCollection<GuestSession> Sessions => _sessions.Values;

        public void Add(GuestSession session)
        {
            _sessions[session.SessionId] = session;
        }

        public Task<GuestSession?> FindValidAsync(string sessionId, string secret, DateTime utcNow, bool track = true)
        {
            FindValidCalls++;
            if (_sessions.TryGetValue(sessionId, out var session)
                && session.Secret == secret
                && session.ExpiresAtUtc > utcNow)
            {
                return Task.FromResult<GuestSession?>(session);
            }

            return Task.FromResult<GuestSession?>(null);
        }

        public Task<GuestSession?> FindByIdAsync(string sessionId, bool track = true)
            => Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? session : null);

        public Task AddAsync(GuestSession session)
        {
            AddCalls++;
            _sessions[session.SessionId] = session;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(GuestSession session)
        {
            _sessions.Remove(session.SessionId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
