namespace SFA.DAS.HmrcMock.Application.Services;

public interface IGatewayUserService
{
    Task<GatewayUserResponse> Validate(string userId, string password);
}

public class GatewayUserService : IGatewayUserService
{
    public Task<GatewayUserResponse> Validate(string userId, string password)
    {
        throw new NotImplementedException();
    }
}

public abstract class GatewayUserResponse
{
    public string GatewayID { get; set; }
    public string Password { get; set; }
    public string Empref { get; set; }
    public string Name { get; set; }
    public bool? Require2SV { get; set; }
}