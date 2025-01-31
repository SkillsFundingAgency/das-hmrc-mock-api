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
    public async Task<IActionResult> LevyDeclarations([FromRoute] string empRef, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        logger.LogInformation("{LevyDeclarationsName}:- {Serialize}", nameof(LevyDeclarations), JsonSerializer.Serialize(new { empRef, fromDate, toDate }));
        empRef = HttpUtility.UrlDecode(empRef);
        return await TryAuthenticate(async _ =>
        {
            var response = await levyDeclarationService.GetByEmpRef(empRef);
            if (response == null) return NotFound();
            logger.LogInformation("{LevyDeclarationsName}:- {Serialize}", nameof(LevyDeclarations), JsonSerializer.Serialize(response));
            IEnumerable<DeclarationResponse> filteredDeclarations = response.Declarations;

            if (fromDate != null)
            {
                filteredDeclarations = filteredDeclarations.Where(x => x.SubmissionTime > fromDate);
            }

            if (toDate != null)
            {
                filteredDeclarations = filteredDeclarations.Where(x => x.SubmissionTime < toDate);
            }

            filteredDeclarations = filteredDeclarations.OrderByDescending(o => o.Id);
            response.Declarations = filteredDeclarations.ToList();
            
            logger.LogInformation("{LevyDeclarationsName}:filtered:- {Serialize}", nameof(LevyDeclarations), JsonSerializer.Serialize(response.Declarations));
            return Ok(response);

        });
    }
    
    private async Task<IActionResult> TryAuthenticate(Func<GatewayUserResponse, Task<IActionResult>> action)
    {   
        if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            logger.LogInformation("Auth header: {Serialize}", JsonSerializer.Serialize(authHeader));
            if (AuthenticationHeaderValue.TryParse(authHeader, out var accessToken))
            {
                logger.LogInformation("AccessToken: {Serialize}", JsonSerializer.Serialize(accessToken));
                var authRecord = await authRecordService.Find(accessToken.Parameter);
                
                logger.LogInformation("Auth record: {Serialize}", JsonSerializer.Serialize(authRecord));
                
                var user = await gatewayUserService.GetByGatewayIdAsync(authRecord?.GatewayId);

                logger.LogInformation("user: {Serialize}", JsonSerializer.Serialize(user));
                
                if (user != null)
                {
                    logger.LogInformation($"Executing action");
                    return await action(user);
                }
                
                logger.LogInformation("User is null {Serialize}", JsonSerializer.Serialize(user));
            }
            else
            {
                logger.LogInformation("Cannot parse authorization header");
            }
        }
        else
        {
            logger.LogInformation("No authorization header in the request");
        }

        logger.LogInformation("Cannot parse authorization header, {Serialize}", JsonSerializer.Serialize(HttpContext.Request.Headers));
        return BadRequest();
    }
}