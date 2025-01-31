using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Application.TokenHandlers;
using SFA.DAS.HmrcMock.Web.Controllers.API;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers.Api;

public partial class OAuthControllerTests
{
    private const string AuthCode = "abcdef12345";
    private TokenRequestModel _requestParams;

    [SetUp]
    public void SetUp()
    {
        _requestParams = new TokenRequestModel
        {
            ClientId = "client-id",
            ClientSecret = "super-secret",
            Code = AuthCode,
            GrantType = "authorization_code",
            RedirectUri = "redirect-uri",
            RefreshToken = "refresh-token"
        };
    }

    [Test, MoqAutoData]
    public async Task AccessToken_ShouldReturnBadRequest_WhenNoMatchingGrantType(
        [Greedy] OAuthController controller)
    {
        // Arrange
        _requestParams.GrantType = "bad-grant-type";
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldReturnBadRequest_WhenAuthorizationCodeGrantType_IsInvalid(
        [Greedy] OAuthController controller)
    {
        // Arrange
        _requestParams.Code = string.Empty;
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid request. Code is missing.");
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldReturnBadRequest_WhenAuthorizationCodeGrantType_AndAuthCodeNotFound(
        [Frozen] Mock<IAuthCodeService> authCodeService,
        [Greedy] OAuthController controller)
    {
        // Arrange
        authCodeService
            .Setup(x => x.Find(AuthCode))
            .ReturnsAsync((AuthCodeRow)null)
            .Verifiable();
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid code.");
        authCodeService.Verify();
        authCodeService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldCreateAccessToken_WhenAuthorizationCodeGrantType_AndAuthCodeFound(
        [Frozen] Mock<IAuthCodeService> authCodeService,
        [Frozen] Mock<ICreateAccessTokenHandler> createAccessTokenHandler,
        [Greedy] OAuthController controller)
    {
        // Arrange
        const string accessTokenString =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ";

        var accessToken = new AccessToken(accessTokenString, "some-refresh-token", "scope", 1000);
        var authCodeRow = new AuthCodeRow
        {
            AuthorizationCode = AuthCode,
            ClientId = _requestParams.ClientId
        };
        
        authCodeService
            .Setup(x => x.Find(AuthCode))
            .ReturnsAsync(authCodeRow)
            .Verifiable();
        
        createAccessTokenHandler
            .Setup(x => x.CreateAccessTokenAsync(authCodeRow))
            .ReturnsAsync(accessToken)
            .Verifiable();
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>();
        okResult.Subject.Value.Should().BeEquivalentTo(accessToken);
        authCodeService.Verify();
        authCodeService.VerifyNoOtherCalls();

        createAccessTokenHandler.Verify();
        createAccessTokenHandler.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldReturnNull_WhenRefreshTokenGrantType_AndInvalidRefreshToken(
        [Frozen] Mock<ICreateAccessTokenHandler> createAccessTokenHandler,
        [Greedy] OAuthController controller)
    {
        // Arrange
        _requestParams.GrantType = "refresh_token";
        _requestParams.RefreshToken = null;
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        var okResult = result.Should().BeNull();
        createAccessTokenHandler.Verify();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldCreateNewAccessToken_WhenRefreshTokenGrantType_AndInvalidRefreshToken(
        [Frozen] Mock<ICreateAccessTokenHandler> createAccessTokenHandler,
        [Greedy] OAuthController controller)
    {
        // Arrange
        _requestParams.GrantType = "refresh_token";
        const string accessTokenString =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ";

        var accessToken = new AccessToken(accessTokenString, "some-refresh-token", "scope", 1000);

        createAccessTokenHandler
            .Setup(x => x.RefreshAccessTokenAsync(_requestParams.RefreshToken))
            .ReturnsAsync(accessToken)
            .Verifiable();
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        var okResult = result.Should().BeAssignableTo<OkObjectResult>();
        okResult.Subject.Value.Should().BeEquivalentTo(accessToken);
        createAccessTokenHandler.Verify();
        createAccessTokenHandler.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldReturnNull_WhenClientCredentialsGrantType_AndClientIdNull(
        [Greedy] OAuthController controller)
    {
        // Arrange
        _requestParams.GrantType = "client_credentials";
        _requestParams.ClientId = null;
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        result.Should().BeNull();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldReturnNull_WhenClientCredentialsGrantType_AndClientSecretNull(
        [Greedy] OAuthController controller)
    {
        // Arrange
        _requestParams.GrantType = "client_credentials";
        _requestParams.ClientSecret = null;
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        result.Should().BeNull();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldReturnNull_WhenClientCredentialsGrantType_AndClientExistsWithoutPrivilegedAccess(
        [Frozen] Mock<IClientService> clientService,
        [Frozen] Mock<ICreateAccessTokenHandler> createAccessTokenHandler,
        [Greedy] OAuthController controller)
    {
        // Arrange
        var application = new Application.Services.Application()
        {
            ClientId = _requestParams.ClientId,
            ClientSecret = _requestParams.ClientSecret,
            PrivilegedAccess = false
        };
        
        _requestParams.GrantType = "client_credentials";
        clientService
            .Setup(x => x.GetById(_requestParams.ClientId))
            .ReturnsAsync(application)
            .Verifiable();
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        result.Should().BeNull();
        clientService.Verify();
        clientService.VerifyNoOtherCalls();
        createAccessTokenHandler.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task AccessToken_ShouldCreateAuthCodeRecord_WhenClientCredentialsGrantType_AndClientExistsWithPrivilegedAccess(
        [Frozen] Mock<IClientService> clientService,
        [Frozen] Mock<ICreateAccessTokenHandler> createAccessTokenHandler,
        [Frozen] Mock<IAuthCodeService> authCodeService,
        [Greedy] OAuthController controller)
    {
        // Arrange
        var application = new Application.Services.Application()
        {
            ClientId = _requestParams.ClientId,
            ClientSecret = _requestParams.ClientSecret,
            PrivilegedAccess = true
        };
        
        const string accessTokenString =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ";
        var accessToken = new AccessToken(accessTokenString, "some-refresh-token", "scope", 1000);
        
        _requestParams.GrantType = "client_credentials";
        clientService
            .Setup(x => x.GetById(_requestParams.ClientId))
            .ReturnsAsync(application)
            .Verifiable();
        
        createAccessTokenHandler
            .Setup(x=>x.CreateAccessTokenAsync(It.Is<AuthCodeRow>(x => x.GatewayUserId == "pa-user")))
            .ReturnsAsync(accessToken)
            .Verifiable();
        
        // Act
        var result = await controller.AccessToken(_requestParams);

        // Assert
        var okResult = result.Should().BeAssignableTo<OkObjectResult>();
        okResult.Subject.Value.Should().BeEquivalentTo(accessToken);
        createAccessTokenHandler.Verify();
        createAccessTokenHandler.VerifyNoOtherCalls();
        
        authCodeService.Verify(x => x.Insert(It.Is<AuthCodeRow>(a => a.GatewayUserId == "pa-user"
        && a.AuthorizationCode == accessToken.Token)), Times.Once);
    }
}
