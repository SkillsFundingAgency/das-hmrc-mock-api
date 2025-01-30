using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Polly;
using SFA.DAS.HmrcMock.Application.Helpers;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

    [Route("oauth/[controller]")]
    [ApiController]
    public class GrantScopeController(
        IAuthRequestService authRequestService,
        IAuthCodeService authCodeService,
        IScopeService scopeService,
        ILogger<GrantScopeController> logger,
        IDistributedCache cache)
        : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Show([FromQuery(Name = "auth_id")]string authId)
        {
            logger.LogInformation("{ActionName} - {SerializedRequest}", nameof(Show), JsonSerializer.Serialize(authId));
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
            logger.LogInformation($"{nameof(Cancel)} - {JsonSerializer.Serialize(authId)}");
            var auth = await authRequestService.Delete(authId);
            if (auth == null)
                return BadRequest("Unknown auth id");

            return Redirect($"{auth.RedirectUri}?error=access_denied&error_description=user+denied+the+authorization&error_code=USER_DENIED_AUTHORIZATION");
        }

        [HttpPost]
        public async Task<IActionResult> GrantScope(string authId)
        {
            logger.LogInformation($"{nameof(GrantScope)} - {JsonSerializer.Serialize(authId)}");
            var auth = await authRequestService.Delete(authId);
            if (auth == null)
                return BadRequest("Unknown auth id");

            var validatedUser = await GetValidatedUserFromCacheAsync();

            if (validatedUser == null)
            {
                logger.LogInformation("Unable to retrieve validated user from cache after retries.");
                return StatusCode(500); // Or handle the failure according to your application's requirements
            }

            logger.LogInformation($"{nameof(GrantScope)} validated user - {JsonSerializer.Serialize(validatedUser)}");

            var token = TokenService.GenerateToken();

            var authCode = new AuthCodeRow
            {
                AuthorizationCode = token,
                GatewayUserId = validatedUser,
                ClientId = auth.ClientId,
                IssueDateTime = DateTime.UtcNow,
                Scope = auth.Scope,
                ExpirationSeconds = 4 * 60 * 60,
                RedirectUri = ""
            };

            logger.LogInformation($"{nameof(GrantScope)} inserting authcode- {JsonSerializer.Serialize(authCode)}");

            await authCodeService.Insert(authCode);

            var uri = $"{auth.RedirectUri}?code={authCode.AuthorizationCode}";

            logger.LogInformation($"{nameof(GrantScope)} removing cache item");
            await cache.RemoveAsync("ValidatedUserKey");
            return Redirect(uri);
        }

        private async Task<string> GetValidatedUserFromCacheAsync()
        {
            var retryPolicy = Policy<string>
                .HandleResult(result => result == null)
                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt));

            return await retryPolicy.ExecuteAsync(async () =>
            {
                var validatedUser = await cache.GetStringAsync("ValidatedUserKey");
              
                return validatedUser;
            });
        }
    }