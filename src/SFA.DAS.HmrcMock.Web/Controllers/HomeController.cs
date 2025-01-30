using System.Text.Json;
using System.Text.RegularExpressions;
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
    private const string LevyAccountIdentifier = "LE";
    
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

        var validationResult = await CheckOrCreate(userData.UserId!, userData.Password!); 

        logger.LogInformation($"{nameof(SignIn)} - ValidationResult: {JsonSerializer.Serialize(validationResult)}");

        if (validationResult != null)
        {
            await cache.SetStringAsync("ValidatedUserKey", validationResult.GatewayID);

            logger.LogInformation($"Set Cache entry: {JsonSerializer.Serialize(validationResult.GatewayID)}");

            var redirectUrl = userData.Continue!;
            
            return Redirect(redirectUrl);
        }
        else
        {
            ModelState.AddModelError("Username", "Bad user name or password");
            return View("SignIn", userData);
        }
    }

    private async Task<GatewayUserResponse> CheckOrCreate(string userId, string userPassword)
    {
        var validUser = await gatewayUserService.ValidateAsync(userId, userPassword);
        if(validUser != null) return validUser;

        const string userIdPattern = @"^(NL|LE)_[1-9][0-9]?_[0-9]{1,9}$";
        if (!Regex.IsMatch(userId, userIdPattern, RegexOptions.None, TimeSpan.FromSeconds(10)))
        {
            return null;
        }
        
        var splitDetails = userId.Split("_");
        var shouldCreateDeclarations = splitDetails[0] == LevyAccountIdentifier;
        _ = int.TryParse(splitDetails[1], out var numberOfDeclarations);
        _ = long.TryParse(splitDetails[2], out var declarationAmount);

        userId += DateTime.UtcNow.Ticks;
        await gatewayUserService.CreateGatewayUserAsync(userId, userPassword);
        
        validUser = await gatewayUserService.ValidateAsync(userId, userPassword);
        
        await fractionService.CreateFractionAsync(validUser.Empref);
        await empRefService.CreateEmpRefAsync(validUser.Empref);

        if (shouldCreateDeclarations)
        {
            await levyDeclarationService.CreateDeclarationsAsync(
                validUser.Empref,
                numberOfDeclarations,
                declarationAmount);
        }

        return validUser;
    }
}