using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Controllers.API;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers.Api;

public partial class OAuthControllerTests
{
    [Test, MoqAutoData]
    public async Task Authorize_ShouldReturnBadRequest_WhenNoMatchingClient(
        [Frozen] Mock<IClientService> clientService,
        [Frozen] Mock<IScopeService> scopeService,
        [Greedy] OAuthController controller)
    {
        // Arrange
        const string scopeName = "Apprenticeship:Levy";
        const string clientId = "bad-client-id";
        
        clientService
            .Setup(x => x.GetById(clientId))
            .ReturnsAsync((Application.Services.Application)null)
            .Verifiable();
        
        // Act
        var result = await controller.Authorize(scopeName, clientId, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        clientService.Verify();
        clientService.VerifyNoOtherCalls();
        scopeService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Authorize_ShouldReturnBadRequest_WhenNoMatchingScope(
        [Frozen] Mock<IClientService> clientService,
        [Frozen] Mock<IScopeService> scopeService,
        [Greedy] OAuthController controller)
    {
        // Arrange
        const string scopeName = "bad-scope";
        const string clientId = "good-client-id";
        var application = new Application.Services.Application
        {
            ApplicationId = "good-application-id",
            ClientId = clientId,
            ClientSecret = "super-secret",
            PrivilegedAccess = true
        };
        
        clientService
            .Setup(x => x.GetById(clientId))
            .ReturnsAsync(application)
            .Verifiable();

        scopeService
            .Setup(x => x.GetByName(scopeName))
            .ReturnsAsync((Scope)null)
            .Verifiable();
        
        // Act
        var result = await controller.Authorize(scopeName, clientId, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        clientService.Verify();
        clientService.VerifyNoOtherCalls();
        scopeService.Verify();
        scopeService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Authorize_ShouldCreateAuthRequest_WhenMatchingScopeAndClient(
        [Frozen] Mock<IClientService> clientService,
        [Frozen] Mock<IScopeService> scopeService,
        [Frozen] Mock<IAuthRequestService> authRequestService,
        [Greedy] OAuthController controller)
    {
        // Arrange
        const string scopeName = "good-scope";
        const string clientId = "good-client-id";
        const string redirectUri = "redirect/to/here";
        var application = new Application.Services.Application
        {
            ApplicationId = "good-application-id",
            ClientId = clientId,
            ClientSecret = "super-secret",
            PrivilegedAccess = true
        };

        var scope = new Scope
        {
            Name = scopeName
        };
        
        clientService
            .Setup(x => x.GetById(clientId))
            .ReturnsAsync(application)
            .Verifiable();

        scopeService
            .Setup(x => x.GetByName(scopeName))
            .ReturnsAsync(scope)
            .Verifiable();
        
        // Act
        var result = await controller.Authorize(scopeName, clientId, redirectUri);

        // Assert
        result.Should().BeOfType<RedirectResult>();
        authRequestService.Verify(x => x.Save(It.Is<AuthRequest>(y => y.Scope == scopeName 
                                                                      && y.ClientId == clientId
                                                                      && y.RedirectUri == redirectUri)));
    }
}
