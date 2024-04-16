using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

[Route("api/apprenticeship-levy")]
[AllowAnonymous]
[ApiController]
public class ApprenticeshipLevyController(
    IGatewayUserService gatewayUserService, 
    IAuthRecordService authRecordService,
    IEmpRefService empRefService,
    ILogger<ApprenticeshipLevyController> logger)
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
        empRef = HttpUtility.UrlDecode(empRef);
        return await TryAuthenticate(async _ =>
        {
            var response = await empRefService.GetByEmpRef(empRef);
            logger.LogInformation($"Emp ref response: {JsonSerializer.Serialize(response)}");
            if (response != null)
            {
                logger.LogInformation($"Sending empref response: {JsonSerializer.Serialize(response)}");
                return Ok(response);
            }
            else
            {
                return NotFound();
            }
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
                var authRecord = await authRecordService.Find(accessToken.Parameter);
                
                logger.LogInformation($"Auth record: {JsonSerializer.Serialize(authRecord)}");
                
                var user = await gatewayUserService.GetByGatewayIdAsync(authRecord?.GatewayId);

                logger.LogInformation($"user: {JsonSerializer.Serialize(user)}");
                
                if (user != null)
                {
                    logger.LogInformation($"Executing action");
                    return await action(user);
                }
                
                logger.LogInformation($"User is null? {JsonSerializer.Serialize(user)}");
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

        logger.LogInformation($"Cannot parse authorization header, {JsonSerializer.Serialize(HttpContext.Request.Headers)}");
        return BadRequest();
    }
}

public class RootResponse
{
    public List<string> Emprefs { get; set; }
}