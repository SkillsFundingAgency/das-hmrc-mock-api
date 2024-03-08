using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Helpers;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Controllers;

    [Route("oauth/[controller]")]
    [ApiController]
    public class GrantScopeController(IAuthRequestService authRequestService, IAuthCodeService authCodeService, IScopeService scopeService)
        : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Show([FromQuery(Name = "auth_id")]string authId)
        {
            var auth = await authRequestService.Get(authId);
            if (auth == null)
                return BadRequest("Unknown auth id");

            var scope = await scopeService.GetByName(auth.Scope);
            if (scope == null)
                return BadRequest("Unknown scope");
            
            return await GrantScope(auth.Id.ToString());
        }

        [HttpGet("cancel")]
        public async Task<IActionResult> Cancel(string authId)
        {
            var auth = await authRequestService.Delete(authId);
            if (auth == null)
                return BadRequest("Unknown auth id");

            return Redirect($"{auth.RedirectUri}?error=access_denied&error_description=user+denied+the+authorization&error_code=USER_DENIED_AUTHORIZATION");
        }

        [HttpPost]
        public async Task<IActionResult> GrantScope(string authId)
        {
            var auth = await authRequestService.Delete(authId);
            if (auth == null)
                return BadRequest("Unknown auth id");

            var validatedUser = HttpContext.Session.GetString("ValidatedUserKey");
            var token = TokenService.GenerateToken();
            var authCode = new AuthCodeRow{
                AuthorizationCode = token, 
                GatewayUserId = validatedUser,
                ClientId = auth.ClientId,
                IssueDateTime = DateTime.UtcNow, 
                Scope = auth.Scope,
                ExpirationSeconds = 4 * 60 * 60,
                RedirectUri = ""
            };
            
            await authCodeService.Insert(authCode);

            var uri = $"{auth.RedirectUri}?code={authCode.AuthorizationCode}";

            HttpContext.Session.Clear();
            return Redirect(uri);
        }
    }