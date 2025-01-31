using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace SFA.DAS.HmrcMock.Application.Helpers;

[ExcludeFromCodeCoverage]
public static class TokenService
{
    private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

    public static string GenerateToken()
    {
        var bytes = new byte[12];
        Random.GetBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}