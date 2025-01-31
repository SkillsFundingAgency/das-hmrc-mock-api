using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Controllers.API;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers.Api;

public class DeclarationsControllerTests
{
    private const string TestEmpRef = "123/ABC";
    
    [Test, MoqAutoData]
    public async Task LevyDeclarations_ShouldReturnBadRequest_WhenNoAuthorizationHeader(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] DeclarationsController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.LevyDeclarations(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task LevyDeclarations_ShouldReturnBadRequest_WhenEmpty_AuthorizationHeader(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] DeclarationsController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        httpContext.Request.Headers.Authorization = string.Empty;
        
        // Act
        var result = await controller.LevyDeclarations(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task LevyDeclarations_ShouldReturnBadRequest_WhenUserIsNotFound(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] DeclarationsController controller)
    {
        // Arrange
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService);

        // Act
        var result = await controller.LevyDeclarations(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.Verify();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.Verify();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task LevyDeclarations_ShouldReturnNotFound_WhenNoDeclarationsForEmpRef(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Frozen] Mock<ILevyDeclarationService> levyDeclarationsService,
        [Greedy] DeclarationsController controller)
    {
        // Arrange
        var testUser = new GatewayUserResponse { Empref = TestEmpRef };
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService, testUser);
        levyDeclarationsService
            .Setup(x => x.GetByEmpRef(TestEmpRef))
            .ReturnsAsync((LevyDeclarationResponse)null)
            .Verifiable();

        // Act
        var result = await controller.LevyDeclarations(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        levyDeclarationsService.Verify();
        levyDeclarationsService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task LevyDeclarations_ShouldReturnOrderedResponse_WhenDeclarationsForEmpRef(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Frozen] Mock<ILevyDeclarationService> levyDeclarationsService,
        [Greedy] DeclarationsController controller)
    {
        // Arrange
        var testDeclarations = new LevyDeclarationResponse
        {
            Declarations =
            [
                new DeclarationResponse
                {
                    DeclarationId = 1,
                    LevyAllowanceForFullYear = 15000,
                    LevyDueYTD = 100,
                    PayrollPeriod = new()
                    {
                        Year = "24-25",
                        Month = 9
                    }
                }
            ]
        };
        
        var testUser = new GatewayUserResponse { Empref = TestEmpRef };
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService, testUser);
        levyDeclarationsService
            .Setup(x => x.GetByEmpRef(TestEmpRef))
            .ReturnsAsync(testDeclarations)
            .Verifiable();

        // Act
        var result = await controller.LevyDeclarations(TestEmpRef, new DateTime(2024, 1, 1), new DateTime(2025, 1, 1));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>();
        okResult.Subject.Value.Should().BeEquivalentTo(testDeclarations);
        
        levyDeclarationsService.Verify();
        levyDeclarationsService.VerifyNoOtherCalls();
    }
    
    private static void SetUpControllerContextWithAuthHeader(
        DeclarationsController controller,
        Mock<IAuthRecordService> authRecordService,
        Mock<IGatewayUserService> gatewayUserService,
        GatewayUserResponse? gatewayUserResponse = null)
    {
        const string testAuthHeader = "Bearer testToken";
        const string testGatewayId = "testGatewayId";

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        controller.HttpContext.Request.Headers.Authorization = testAuthHeader;
        
        authRecordService
            .Setup(service => service.Find("testToken"))
            .ReturnsAsync(new AuthRecord { GatewayId = testGatewayId })
            .Verifiable();

        gatewayUserService
            .Setup(service => service.GetByGatewayIdAsync(testGatewayId))
            .ReturnsAsync(gatewayUserResponse)
            .Verifiable();
    }
}
