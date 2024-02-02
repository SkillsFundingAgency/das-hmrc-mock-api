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
    public IActionResult SignIn([FromQuery] string continueUrl, [FromQuery] string origin)
    {
        ViewData["Continue"] = continueUrl;
        ViewData["Origin"] = origin;
        return View(new SigninViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> HandleSignIn(string continueUrl, string origin, SigninViewModel userData)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Continue"] = continueUrl;
            ViewData["Origin"] = origin;
            return View("SignIn", userData);
        }

        var validationResult = await gatewayUserService.Validate(userData.UserId, userData.Password);

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
                return Redirect(continueUrl);
            // }
        }
        else
        {
            ModelState.AddModelError("Username", "Bad user name or password");
            ViewData["Continue"] = continueUrl;
            ViewData["Origin"] = origin;
            return View("SignIn", userData);
        }
    }
}
