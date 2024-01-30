using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SFA.DAS.HmrcMock.Web.Controllers;

[Route("gg")]
public class HomeController : Controller
{
    [HttpGet]
    [Route("sign-in")]
    public IActionResult SignIn()
    {
        return View();
    }
}
