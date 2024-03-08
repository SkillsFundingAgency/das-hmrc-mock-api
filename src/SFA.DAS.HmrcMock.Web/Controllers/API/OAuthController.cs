using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Application.TokenHandlers;

namespace SFA.DAS.HmrcMock.Controllers.Api;

    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class OAuthController(
        ICreateAccessTokenHandler createAccessTokenHandler, 
        IAuthCodeService authCodeService,
        IScopeService scopeService, 
        IAuthRequestService authRequestService, 
        IClientService clientService)
        : ControllerBase
    {
        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize(
            [FromQuery(Name = "scope")]string scopeName, 
            [FromQuery(Name = "client_id")]string? clientId, 
            [FromQuery(Name = "redirect_uri")]string redirectUri)
        {
            return await HandleAuth(scopeName, clientId, redirectUri);
        }

        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizePost([FromBody]AuthorizePostParams parameters)
        {
            return await HandleAuth(parameters.ScopeName, parameters.ClientId, parameters.RedirectUri);
        }
        
        [HttpPost("token")]
        public async Task<IActionResult> AccessToken([FromBody] TokenRequestModel tokenRequest)
        {
            if (tokenRequest == null || string.IsNullOrEmpty(tokenRequest.Code))
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
        public string RedirectUri { get; set; }
        
        [JsonProperty("code")]
        public string Code { get; set; }
    }
    
    public class AuthorizePostParams
    {
        public string ScopeName { get; set; }
        public string? ClientId { get; set; }
        public string RedirectUri { get; set; }
    }