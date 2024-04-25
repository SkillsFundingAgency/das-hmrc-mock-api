using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

[Route("api/apprenticeship-levy/epaye/{empRef}")]
[ApiController]
public class DeclarationsController(
    IGatewayUserService gatewayUserService, 
    IAuthRecordService authRecordService,
    ILevyDeclarationService levyDeclarationService,
        ILogger<DeclarationsController> logger) : ControllerBase
{
    [HttpGet("declarations")]
    public async Task<IActionResult> LevyDeclarations([FromRoute] string empRef, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        logger.LogInformation($"{nameof(LevyDeclarations)}:- {JsonSerializer.Serialize(new{ empRef, fromDate, toDate })}");
        empRef = HttpUtility.UrlDecode(empRef);
        return await TryAuthenticate(async _ =>
        {
            var response = await levyDeclarationService.GetByEmpRef(empRef);
            if (response != null)
            {
                logger.LogInformation($"{nameof(LevyDeclarations)}:- {JsonSerializer.Serialize(response)}");
                var filteredDeclarations = response.Declarations
                    .Where(x => x.SubmissionTime > fromDate 
                                && x.SubmissionTime < toDate)
                    .OrderByDescending(o => o.Id);

                response.Declarations = filteredDeclarations.ToList();
                
                logger.LogInformation($"{nameof(LevyDeclarations)}:filtered:- {JsonSerializer.Serialize(response.Declarations)}");
                return Ok(response);
            }

            return NotFound();
        });
    }
    
    private async Task<IActionResult> TryAuthenticate(Func<GatewayUserResponse, Task<IActionResult>> action)
    {
        if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            logger.LogInformation($"Auth header: {JsonSerializer.Serialize(authHeader)}");
            if (AuthenticationHeaderValue.TryParse(authHeader, out var accessToken))
            {
                logger.LogInformation($"AccessToken: {JsonSerializer.Serialize(accessToken)}");
                var authRecord = await authRecordService.Find(accessToken.Parameter!);
                var user = await gatewayUserService.GetByGatewayIdAsync(authRecord.GatewayId!);

                if (user != null)
                {
                    return await action(user);
                }
            }
        }

        return Forbid();
    }
}