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

        private AuthRecord BuildAuthRecord(AuthCodeRow authInfo, bool privileged)
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

        private AccessToken BuildAccessToken(AuthRecord authRecord)
        {
            var expiresIn = TimeSpan.FromSeconds(authRecord.ExpiresIn);
            var refreshedAt = authRecord.RefreshedAt ?? DateTime.UtcNow;
            return new AccessToken(authRecord.AccessToken, authRecord.RefreshToken, authRecord.Scope, (int)expiresIn.TotalSeconds, refreshedAt);
        }

        private string GenerateToken()
        {
            return TokenService.GenerateToken();
        }
    }

    public class AccessToken(
        string token,
        string refreshToken,
        string scope,
        int expiresInTotalSeconds,
        DateTime refreshedAt)
    {
        public string Token { get; } = token;
        public string RefreshToken { get; } = refreshToken;
        public string Scope { get; } = scope;
        public int ExpiresInTotalSeconds { get; } = expiresInTotalSeconds;
        public DateTime RefreshedAt { get; } = refreshedAt;
    }