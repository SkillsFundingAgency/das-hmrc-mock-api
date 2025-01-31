using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.Testing.AutoFixture;
using GrantScopeController = SFA.DAS.HmrcMock.Web.Controllers.API.GrantScopeController;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers.Api;

public partial class GrantScopeControllerTests
{
    [Test, MoqAutoData]
    public async Task GrantScope_ShouldReturnBadRequest_WhenInvalidAuthId(
        [Frozen] Mock<IAuthRequestService> authRequestService,
        [Frozen] Mock<IDistributedCache> distributedCache,
        [Greedy] GrantScopeController controller)
    {
        // Arrange
        const string authId = "b41f5d19-b7c0-4544-b149-e74c5edab4ff";
        authRequestService
            .Setup(x => x.Delete(authId))
            .ReturnsAsync((AuthRequest)null)
            .Verifiable();

        // Act
        var result = await controller.GrantScope(authId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Unknown auth id");
        
        authRequestService.Verify();
        authRequestService.VerifyNoOtherCalls();
        distributedCache.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task GrantScope_ShouldReturnInternalServerError_WhenUserNotPresentInCache(
        [Frozen] Mock<IAuthRequestService> authRequestService,
        [Frozen] Mock<IDistributedCache> distributedCache,
        [Greedy] GrantScopeController controller)
    {
        // Arrange
        const string scope = "good:scope";
        const string authId = "b41f5d19-b7c0-4544-b149-e74c5edab4ff";
        var authRequest = new AuthRequest { Scope = scope };
        authRequestService
            .Setup(x => x.Delete(authId))
            .ReturnsAsync(authRequest)
            .Verifiable();

        distributedCache
            .Setup(x => x.GetAsync("ValidatedUserKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[])null)
            .Verifiable();

        // Act
        var result = await controller.GrantScope(authId);

        // Assert
        var statusResult = result.Should().BeAssignableTo<StatusCodeResult>();
        statusResult.Subject.StatusCode.Should().Be(500);
        distributedCache.Verify(x => x.GetAsync("ValidatedUserKey", It.IsAny<CancellationToken>()), Times.Exactly(3));
        
        authRequestService.Verify();
        authRequestService.VerifyNoOtherCalls();
        distributedCache.Verify();
        distributedCache.VerifyNoOtherCalls();
    }
}
