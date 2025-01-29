using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Application.TokenHandlers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

    [Route("api/[controller]")]
    [ApiController]
    public class OAuthController(
        ICreateAccessTokenHandler createAccessTokenHandler, 
        IAuthCodeService authCodeService,
        IScopeService scopeService, 
        IAuthRequestService authRequestService, 
        IClientService clientService,
        ILogger<OAuthController> logger)
        : Controller
    {
        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize(
            [FromQuery(Name = "scope")]string scopeName, 
            [FromQuery(Name = "client_id")]string? clientId, 
            [FromQuery(Name = "redirect_uri")]string redirectUri)
        {
            logger.LogInformation("{MethodName} - {SerializedRequest}", nameof(Authorize), JsonSerializer.Serialize(new { scopeName, clientId, redirectUri }));
            return await HandleAuth(scopeName, clientId, redirectUri);
        }

        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizePost([FromBody]AuthorizePostParams parameters)
        {
            logger.LogInformation("{MethodName} - {SerializedRequest}", nameof(AuthorizePost), JsonSerializer.Serialize(parameters));
            return await HandleAuth(parameters.ScopeName, parameters.ClientId, parameters.RedirectUri);
        }
        
        [HttpPost("token")]
        public async Task<IActionResult> AccessToken([FromBody] TokenRequestModel tokenRequest)
        {
            logger.LogInformation("{MethodName} - {SerializedRequest}", nameof(AccessToken), JsonSerializer.Serialize(tokenRequest));

            return tokenRequest.GrantType switch
            {
                "authorization_code" => await HandleAuthorizationCodeGrantAsync(tokenRequest),
                "refresh_token" => await HandleRefreshTokenGrantAsync(tokenRequest),
                "client_credentials" => await HandleClientCredentialsGrantAsync(tokenRequest),
                _ => BadRequest(new { error = "unsupported_grant_type" })
            };
        }

        private async Task<IActionResult> HandleRefreshTokenGrantAsync(TokenRequestModel tokenRequest)
        {
            if (tokenRequest.RefreshToken == null) return null;
            var accessToken = await createAccessTokenHandler.RefreshAccessTokenAsync(tokenRequest.RefreshToken);

            return Ok(accessToken);
        }

        private async Task<IActionResult> HandleClientCredentialsGrantAsync(TokenRequestModel tokenRequest)
        {
            if (tokenRequest.ClientId == null || tokenRequest.ClientSecret == null) return null;
            var scope = "read:apprenticeship-levy";
            var client = await clientService.GetById(tokenRequest.ClientId);

            if (client is not { PrivilegedAccess: true }) return null;

            const string paUserId = "pa-user";
            var authCodeRecord = new AuthCodeRow
            {
                ClientId = client.ClientId,
                Scope = scope,
                GatewayUserId = paUserId
            };
            
            var accessToken = await createAccessTokenHandler.CreateAccessTokenAsync(authCodeRecord);
           
            logger.LogInformation($"Creating auth code {accessToken.Token}");
            var authCode = new AuthCodeRow
            {
                AuthorizationCode = accessToken.Token,
                GatewayUserId = paUserId,
                ClientId = client.ClientId,
                IssueDateTime = DateTime.UtcNow,
                Scope = scope,
                ExpirationSeconds = 4 * 60 * 60,
                RedirectUri = ""
            };

            await authCodeService.Insert(authCode);
            return Ok(accessToken);
        }

        private async Task<IActionResult> HandleAuthorizationCodeGrantAsync(TokenRequestModel tokenRequest)
        {
            if (string.IsNullOrEmpty(tokenRequest.Code))
            {
                return BadRequest("Invalid request. Code is missing.");
            }
            var authCodeRecord = await authCodeService.Find(tokenRequest.Code);
            if (authCodeRecord == null)
            {
                return BadRequest("Invalid code.");
            }

            var accessToken = await createAccessTokenHandler.CreateAccessTokenAsync(authCodeRecord);
            return Ok(accessToken);
        }
        
        private async Task<IActionResult> HandleAuth(string scopeName, string? clientId, string redirectUri)
        {
            var client = await clientService.GetById(clientId);
            if (client == null)
                return BadRequest("Unknown client id");

            var scope = await scopeService.GetByName(scopeName);
            if (scope == null)
                return BadRequest("Unknown scope");

            var authRequest = new AuthRequest
            {
                Scope = scopeName,
                ClientId = clientId,
                RedirectUri = redirectUri,
                CreationDate = DateTime.Now
            };

            var id = await authRequestService.Save(authRequest);
            return Redirect($"/api/GrantScope/{id}");
        }
    }
    
    public class TokenRequestModel
    {
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
        
        [JsonProperty("client_id")]
        public string ClientId { get; set; }
        
        [JsonProperty("grant_type")]
        public string GrantType { get; set; }
        
        [JsonProperty("redirect_uri")]
        public string? RedirectUri { get; set; }
        
        [JsonProperty("code")]
        public string? Code { get; set; }
        
        [JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }
    }
    
    public class AuthorizePostParams
    {
        public string ScopeName { get; set; }
        public string? ClientId { get; set; }
        public string RedirectUri { get; set; }
    }