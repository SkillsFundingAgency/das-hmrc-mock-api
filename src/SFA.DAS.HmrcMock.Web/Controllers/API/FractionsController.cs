using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

[Route("api/apprenticeship-levy")]
[ApiController]
public class FractionsController(
    IGatewayUserService gatewayUserService, 
    IAuthRecordService authRecordService,
    IFractionCalcService fractionCalcService,
    IFractionService fractionService,
    ILogger<FractionsController> logger) : Controller
{
    [HttpGet("epaye/{empRef}/fractions")]
    public async Task<IActionResult> Fractions([FromRoute] string empRef, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        empRef = HttpUtility.UrlDecode(empRef);

        return await TryAuthenticate(async _ =>
        {
            if (empRef == "666/X6666")
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                    "The service is temporarily unavailable for this scheme.");
            }
            
            var response = await fractionService.GetByEmpRef(empRef);
            if (response == null) return NotFound();

            IEnumerable<FractionCalcResponse> filteredFractions = response.FractionCalcResponses;

            if (fromDate != null)
            {
                filteredFractions = filteredFractions
                    .Where(x => x.CalculatedAt.Date >= fromDate);
            }

            if (toDate != null)
            {
                filteredFractions = filteredFractions
                    .Where(x => x.CalculatedAt <= toDate);
            }

            // Order by date and return the response
            response.FractionCalcResponses = filteredFractions.OrderBy(x => x.CalculatedAt).ToList();
            return Ok(response);
        });
    }

    [HttpGet("fraction-calculation-date")]
    public async Task<IActionResult> CalculationDate()
    {
        var lastCalculationDate = await fractionCalcService.LastCalculationDate();
        if (lastCalculationDate != null)
        {
            return Ok(lastCalculationDate.LastCalculationDate);
        }

        return NotFound();
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