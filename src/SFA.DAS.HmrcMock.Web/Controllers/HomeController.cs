using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Models;

namespace SFA.DAS.HmrcMock.Web.Controllers;

[Route("gg")]
public class HomeController(IGatewayUserService gatewayUserService) : Controller
{
    private readonly IGatewayUserService _gatewayUserService = gatewayUserService;

    [HttpGet]
    [Route("sign-in")]
    public IActionResult SignIn(
        [FromQuery(Name = "continue")] string? redirectUrl = null,
        [FromQuery] string? origin = null)
    {
        return View(new SigninViewModel() with { Continue = redirectUrl, Origin = origin });
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

        if (validationResult != null)
        {
            // always seems to be false for us
            // if (validationResult.Require2SV.GetValueOrDefault(false))
            // {
            //     HttpContext.Session.SetString("ValidatedUserKey", validationResult.GatewayID);
            //     return RedirectToAction("Show", "AccessCode", new { continueUrl, origin });
            // }
            // else
            // {
            HttpContext.Session.SetString("ValidatedUserKey", validationResult.GatewayID);
            return Redirect(userData.Continue);
            // }
        }
        else
        {
            ModelState.AddModelError("Username", "Bad user name or password");
            return View("SignIn", userData);
        }
    }
}