using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Controllers.Api;

[Route("api/apprenticeship-levy")]
[ApiController]
public class ApprenticeshipLevyController(IGatewayUserService gatewayUserService, IAuthRecordService authRecordService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Root()
    {
        return await TryAuthenticate(async user =>
        {
            var empRefs = user.Empref != null ? new List<string> { user.Empref } : new List<string>();
            var response = new RootResponse
            {
                Emprefs = empRefs
            };

            return Ok(response);
        });
    }

    [HttpGet("epaye/{empRef}")]
    public async Task<IActionResult> EPaye([FromRoute]string empRef)
    {
        return await TryAuthenticate(async user =>
        {
            var empRefs = user.Empref != null ? new List<string> { user.Empref } : new List<string>();
            var response = new RootResponse
            {
                Emprefs = empRefs
            };

            return Ok(response);
        });
    }

    private async Task<IActionResult> TryAuthenticate(Func<GatewayUserResponse, Task<IActionResult>> action)
    {
        if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            if (AuthenticationHeaderValue.TryParse(authHeader, out var accessToken))
            {
                var authRecord = await authRecordService.Find(accessToken.Parameter);
                var user = await gatewayUserService.GetByGatewayIdAsync(authRecord?.GatewayId);

                if (user != null)
                {
                    return await action(user);
                }
            }
        }

        return Forbid();
    }
}

public class RootResponse
{
    public List<string> Emprefs { get; set; }
}