using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Models;

namespace SFA.DAS.HmrcMock.Web.Controllers;

[Route("gg")]
public class HomeController(
    IGatewayUserService gatewayUserService,
    IFractionService fractionService,
    ILevyDeclarationService levyDeclarationService,
    IEmpRefService empRefService,
    ILogger<HomeController> logger,
    IDistributedCache cache) : Controller
{
    [HttpGet]
    [Route("sign-in")]
    public IActionResult SignIn(
        [FromQuery(Name = "continue")] string? redirectUrl = null,
        [FromQuery] string? origin = null)
    {
        
        logger.LogInformation($"{nameof(SignIn)} - {JsonSerializer.Serialize(new {redirectUrl, origin})}");
        return View(new SigninViewModel { Continue = redirectUrl, Origin = origin });
    }

    [HttpPost]
    [Route("sign-in")]
    public async Task<IActionResult> SignIn(SigninViewModel userData)
    {
        if (!ModelState.IsValid)
        {
            return View("SignIn", userData);
        }

        var splitDetails = userData.UserId.Split("_");
        var shouldCreateDeclarations = splitDetails[0] == "LE";
        _ = int.TryParse(splitDetails[1], out var numberOfDeclarations);
        _ = long.TryParse(splitDetails[2], out var declarationAmount);

        userData.UserId += DateTime.UtcNow.Ticks;
        await gatewayUserService.CreateGatewayUserAsync(userData.UserId!, userData.Password!);
        
        var validationResult = await gatewayUserService.ValidateAsync(userData.UserId!, userData.Password!);
        
        await fractionService.CreateFractionAsync(validationResult.Empref);
        await empRefService.CreateEmpRefAsync(validationResult.Empref);

        if (shouldCreateDeclarations)
        {
            await levyDeclarationService.CreateDeclarationsAsync(
                validationResult.Empref,
                numberOfDeclarations,
                declarationAmount);
        }

        logger.LogInformation($"{nameof(SignIn)} - ValidationResult: {JsonSerializer.Serialize(validationResult)}");

        if (validationResult != null)
        {
            await cache.SetStringAsync("ValidatedUserKey", validationResult.GatewayID);

            logger.LogInformation($"Set Cache entry: {JsonSerializer.Serialize(validationResult.GatewayID)}");
            
            return Redirect(userData.Continue!);
        }
        else
        {
            ModelState.AddModelError("Username", "Bad user name or password");
            return View("SignIn", userData);
        }
    }
}