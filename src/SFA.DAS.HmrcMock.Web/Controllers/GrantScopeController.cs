using Microsoft.AspNetCore.Mvc;

namespace SFA.DAS.HmrcMock.Web.Controllers;

    [Route("api/[controller]")]
    public class GrantScopeController(ILogger<GrantScopeController> logger) : Controller
    {
        [HttpGet("{authId}")]
        public IActionResult Show([FromRoute] string authId)
        {
            var redirectUri = $"/gg/sign-in?continue=/oauth/grantscope?auth_id={authId}&origin=oauth-frontend";
         
            logger.LogInformation($"API_{nameof(Show)} - {redirectUri}");
            return Redirect(redirectUri);
        }
    }