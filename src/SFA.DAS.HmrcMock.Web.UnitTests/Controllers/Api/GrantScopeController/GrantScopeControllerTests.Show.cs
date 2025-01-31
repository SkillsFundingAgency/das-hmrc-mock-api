using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.Testing.AutoFixture;
using GrantScopeController = SFA.DAS.HmrcMock.Web.Controllers.API.GrantScopeController;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers.Api;

public partial class GrantScopeControllerTests
{
    [Test, MoqAutoData]
    public async Task Show_ShouldReturnBadRequest_WhenInvalidAuthId(
        [Frozen] Mock<IAuthRequestService> authRequestService,
        [Frozen] Mock<IScopeService> scopeService,
        [Greedy] GrantScopeController controller)
    {
        // Arrange
        const string authId = "b41f5d19-b7c0-4544-b149-e74c5edab4ff";
        authRequestService
            .Setup(x => x.Get(authId))
            .ReturnsAsync((AuthRequest)null)
            .Verifiable();

        // Act
        var result = await controller.Show(authId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Unknown auth id");
        
        authRequestService.Verify();
        authRequestService.VerifyNoOtherCalls();
        scopeService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Show_ShouldReturnBadRequest_WhenInvalidScope(
        [Frozen] Mock<IAuthRequestService> authRequestService,
        [Frozen] Mock<IScopeService> scopeService,
        [Greedy] GrantScopeController controller)
    {
        // Arrange
        const string scope = "bad:scope";
        const string authId = "b41f5d19-b7c0-4544-b149-e74c5edab4ff";
        var authRequest = new AuthRequest { Scope = scope };
        authRequestService
            .Setup(x => x.Get(authId))
            .ReturnsAsync(authRequest)
            .Verifiable();
        
        scopeService.Setup(x => x.GetByName(scope))
            .ReturnsAsync((Scope)null)
            .Verifiable();

        // Act
        var result = await controller.Show(authId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Unknown scope");
        scopeService.Verify();
        scopeService.VerifyNoOtherCalls();
    }
}
