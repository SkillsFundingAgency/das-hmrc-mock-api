using System.Net.Http.Headers;
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
    IFractionService fractionService) : ControllerBase
{
    [HttpGet("epaye/{empRef}/fractions")]
    public async Task<IActionResult> Fractions([FromRoute]string empRef, [FromQuery]DateTime? fromDate, [FromQuery]DateTime? toDate)
    {
        empRef = HttpUtility.UrlDecode(empRef);
        return await TryAuthenticate(async _ =>
        {
            var response = await fractionService.GetByEmpRef(empRef);
            if (response != null)
            {
                var filteredFractions = response.FractionCalcResponses
                    .Where(x => x.CalculatedAt > fromDate 
                                && x.CalculatedAt < toDate)
                    .OrderBy(o => o.CalculatedAt);

                response.FractionCalcResponses = filteredFractions.ToList();
                return Ok(response);
            }

            return NotFound();
        });
    }

    [HttpGet("fraction-calculation-date")]
    public async Task<IActionResult> CalculationDate()
    {
        var lastCalculationDate = await fractionCalcService.LastCalculationDate();
        if (lastCalculationDate != null)
        {
            return Ok(lastCalculationDate);
        }
        else
        {
            return NotFound();
        }
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