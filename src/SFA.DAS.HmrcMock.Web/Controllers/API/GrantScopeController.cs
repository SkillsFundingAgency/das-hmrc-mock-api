using Microsoft.AspNetCore.Mvc;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

    [Route("api/[controller]")]
    [ApiController]
    public class GrantScopeController : ControllerBase
    {
        [HttpGet("{authId}")]
        public IActionResult Show([FromRoute] string authId)
        {
            var redirectUri = $"/gg/sign-in?continue=/oauth/grantscope?auth_id={authId}&origin=oauth-frontend";
            return Redirect(redirectUri);
        }
    }