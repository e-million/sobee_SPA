using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace sobee_API.Services;

public class RequestIdentityResolver
{
    private readonly GuestSessionService _guestSessionService;

    public RequestIdentityResolver(GuestSessionService guestSessionService)
    {
        _guestSessionService = guestSessionService;
    }

    public async Task<RequestIdentity> ResolveAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        HttpResponse response,
        bool allowCreateGuestSession,
        bool allowAuthenticatedGuestSession)
    {
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RequestIdentity.Error(
                    isAuthenticated: true,
                    ownerType: "user",
                    errorCode: "MissingNameIdentifier",
                    errorMessage: "Authenticated request is missing NameIdentifier claim.");
            }

            if (!allowAuthenticatedGuestSession)
            {
                return new RequestIdentity(
                    IsAuthenticated: true,
                    UserId: userId,
                    GuestSessionId: null,
                    GuestSessionToken: null,
                    OwnerType: "user",
                    GuestSessionValidated: false,
                    ErrorCode: null,
                    ErrorMessage: null);
            }

            var guestSession = await _guestSessionService.ResolveAsync(request, response, allowCreate: false);
            return new RequestIdentity(
                IsAuthenticated: true,
                UserId: userId,
                GuestSessionId: guestSession.WasValidated ? guestSession.SessionId : null,
                GuestSessionToken: guestSession.WasValidated ? guestSession.Secret : null,
                OwnerType: "user",
                GuestSessionValidated: guestSession.WasValidated,
                ErrorCode: null,
                ErrorMessage: null);
        }

        var guestSessionForAnonymous = await _guestSessionService.ResolveAsync(request, response, allowCreateGuestSession);
        if (!allowCreateGuestSession && !guestSessionForAnonymous.WasValidated)
        {
            return RequestIdentity.Error(
                isAuthenticated: false,
                ownerType: "guest",
                errorCode: "MissingOrInvalidGuestSession",
                errorMessage: "Missing or invalid guest session headers.");
        }

        return new RequestIdentity(
            IsAuthenticated: false,
            UserId: null,
            GuestSessionId: guestSessionForAnonymous.SessionId,
            GuestSessionToken: guestSessionForAnonymous.Secret,
            OwnerType: "guest",
            GuestSessionValidated: guestSessionForAnonymous.WasValidated,
            ErrorCode: null,
            ErrorMessage: null);
    }
}

public record RequestIdentity(
    bool IsAuthenticated,
    string? UserId,
    string? GuestSessionId,
    string? GuestSessionToken,
    string OwnerType,
    bool GuestSessionValidated,
    string? ErrorCode,
    string? ErrorMessage)
{
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorCode);

    public static RequestIdentity Error(bool isAuthenticated, string ownerType, string errorCode, string errorMessage)
        => new(isAuthenticated, null, null, null, ownerType, false, errorCode, errorMessage);
}
