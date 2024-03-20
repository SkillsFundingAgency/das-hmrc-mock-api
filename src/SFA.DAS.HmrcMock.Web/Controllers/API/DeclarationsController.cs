using System.Net.Http.Headers;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

[Route("api/apprenticeship-levy/epaye/{empRef}")]
[ApiController]
public class DeclarationsController(
    IGatewayUserService gatewayUserService, 
    IAuthRecordService authRecordService,
    ILevyDeclarationService levyDeclarationService) : ControllerBase
{
    [HttpGet("declarations")]
    public async Task<IActionResult> LevyDeclarations([FromRoute] string empRef, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        empRef = HttpUtility.UrlDecode(empRef);
        return await TryAuthenticate(async _ =>
        {
            var response = await levyDeclarationService.GetByEmpRef(empRef);
            if (response != null)
            {
                var filteredDeclarations = response.Declarations
                    .Where(x => x.SubmissionTime > fromDate 
                                && x.SubmissionTime < toDate)
                    .OrderByDescending(o => o.Id);

                response.Declarations = filteredDeclarations.ToList();
                return Ok(response);
            }

            return NotFound();
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