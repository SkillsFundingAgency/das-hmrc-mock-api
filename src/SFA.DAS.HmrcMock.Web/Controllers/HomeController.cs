using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Models;

namespace SFA.DAS.HmrcMock.Web.Controllers;

[Route("gg")]
public class HomeController(
    IGatewayUserService gatewayUserService,
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

        var validationResult = await gatewayUserService.ValidateAsync(userData.UserId, userData.Password);

        logger.LogInformation($"{nameof(SignIn)} - ValidationResult: {JsonSerializer.Serialize(validationResult)}");

        if (validationResult != null)
        {
            await cache.SetStringAsync("ValidatedUserKey", validationResult.GatewayID);

            logger.LogInformation($"Set Cache entry: {JsonSerializer.Serialize(validationResult.GatewayID)}");
            
            return Redirect(userData.Continue);
        }
        else
        {
            ModelState.AddModelError("Username", "Bad user name or password");
            return View("SignIn", userData);
        }
    }
}