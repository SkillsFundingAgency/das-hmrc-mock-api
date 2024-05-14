using System.Security.Cryptography;

namespace SFA.DAS.HmrcMock.Application.Helpers;

public static class TokenService
{
    private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

    public static string GenerateToken()
    {
        byte[] bytes = new byte[12];
        Random.GetBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}