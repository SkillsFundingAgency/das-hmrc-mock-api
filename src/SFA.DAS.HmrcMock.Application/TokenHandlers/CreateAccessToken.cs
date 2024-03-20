using Newtonsoft.Json;
using SFA.DAS.HmrcMock.Application.Helpers;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Application.TokenHandlers;

public interface ICreateAccessTokenHandler
{
    Task<AccessToken> CreateAccessTokenAsync(AuthCodeRow authInfo);
}

public class CreateAccessTokenHandler(
    IClientService clientService,
    IAuthRecordService authRecordService)
    : ICreateAccessTokenHandler
{
    public async Task<AccessToken> CreateAccessTokenAsync(AuthCodeRow authInfo)
    {
        var privileged = await GetPrivilegedAsync(authInfo.ClientId);
        var authRecord = BuildAuthRecord(authInfo, privileged);
        await authRecordService.Insert(authRecord);
        return BuildAccessToken(authRecord);
    }

    private async Task<bool> GetPrivilegedAsync(string? clientId)
    {
        if (clientId == null)
            return false;

        var app = await clientService.GetById(clientId);
        return app?.PrivilegedAccess ?? false;
    }

    private static AuthRecord BuildAuthRecord(AuthCodeRow authInfo, bool privileged)
    {
        var accessToken = GenerateToken();
        var refreshToken = GenerateToken();
        var now = DateTime.UtcNow;
        var expiresIn = TimeSpan.FromHours(4);
        var authRecord = new AuthRecord
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Scope = authInfo.Scope,
            GatewayId = authInfo.GatewayUserId,
            ExpiresIn = (int)expiresIn.TotalSeconds,
            CreatedAt = now,
            ClientId = authInfo.ClientId,
            Privileged = privileged
        };

        return authRecord;
    }

    private static AccessToken BuildAccessToken(AuthRecord authRecord)
    {
        var expiresIn = TimeSpan.FromSeconds(authRecord.ExpiresIn);
        return new AccessToken(authRecord.AccessToken, authRecord.RefreshToken, authRecord.Scope, (int)expiresIn.TotalSeconds);
    }

    private static string GenerateToken()
    {
        return TokenService.GenerateToken();
    }
}

public class AccessToken(
    string token,
    string refreshToken,
    string scope,
    int expiresInTotalSeconds)
{
    [JsonProperty("access_token")] public string Token { get; } = token;
    [JsonProperty("refresh_token")] public string RefreshToken { get; } = refreshToken;
    [JsonProperty("scope")] public string Scope { get; } = scope;
    [JsonProperty("expires_in")] public int ExpiresInTotalSeconds { get; } = expiresInTotalSeconds;
}