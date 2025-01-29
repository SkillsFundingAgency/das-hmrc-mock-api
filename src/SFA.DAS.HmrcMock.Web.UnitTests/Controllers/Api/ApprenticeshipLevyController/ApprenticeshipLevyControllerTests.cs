using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Controllers.API;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers.Api;

public class ApprenticeshipLevyControllerTests
{
    private const string TestEmpRef = "123/ABC";
    
    [Test, MoqAutoData]
    public async Task Root_ShouldReturnBadRequest_WhenNoAuthorizationHeader(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] ApprenticeshipLevyController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.Root();

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Root_ShouldReturnBadRequest_WhenEmpty_AuthorizationHeader(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] ApprenticeshipLevyController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        httpContext.Request.Headers.Authorization = string.Empty;
        
        // Act
        var result = await controller.Root();

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Root_ShouldReturnBadRequest_WhenUserIsNotFound(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] ApprenticeshipLevyController controller)
    {
        // Arrange
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService);

        // Act
        var result = await controller.Root();

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.Verify();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.Verify();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Root_ShouldReturnEmprefs_WhenUserIsAuthenticated(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] ApprenticeshipLevyController controller)
    {
        // Arrange
        var testUser = new GatewayUserResponse { Empref = TestEmpRef };
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService, testUser);

        // Act
        var result = await controller.Root();

        // Assert
        var okResult = result.Should().BeAssignableTo<OkObjectResult>();
        var response = okResult.Subject.Value.Should().BeAssignableTo<RootResponse>();
        response.Subject.Emprefs.Should().BeEquivalentTo(TestEmpRef);

        authRecordService.Verify();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.Verify();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Epaye_ShouldReturnBadRequest_WhenNoAuthorizationHeader(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] ApprenticeshipLevyController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.EPaye(TestEmpRef);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task GetEpayes_ShouldReturnNotFound_WhenNoMatchingEpaye(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Frozen] Mock<IEmpRefService> empRefService,
        [Greedy] ApprenticeshipLevyController controller)
    {
        // Arrange
        const string testEpaye = "123/AB45678";
        var testUser = new GatewayUserResponse { Empref = TestEmpRef };
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService, testUser);
        empRefService
            .Setup(x => x.GetByEmpRef(testEpaye)).ReturnsAsync((EmpRefResponse)null)
            .Verifiable();

        // Act
        var result = await controller.EPaye(testEpaye);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        empRefService.Verify();
        empRefService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task GetEpayes_ShouldOkResult_WhenMatchingEpaye(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Frozen] Mock<IEmpRefService> empRefService,
        [Greedy] ApprenticeshipLevyController controller)
    {
        // Arrange
        const string testEpaye = "123/AB45678";
        var expectedEmpRefResponse = new EmpRefResponse
        {
            Id = new ObjectId("8ff6d6f67393464c87d3abd05682c489"),
            EmpRef = testEpaye
        };
        
        var testUser = new GatewayUserResponse { Empref = testEpaye };
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService, testUser);
        empRefService
            .Setup(x => x.GetByEmpRef(testEpaye)).ReturnsAsync(expectedEmpRefResponse)
            .Verifiable();

        // Act
        var result = await controller.EPaye(testEpaye);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>();
        okResult.Subject.Value.Should().BeAssignableTo<EmpRefResponse>().And.Subject.Should().BeEquivalentTo(expectedEmpRefResponse);
        empRefService.Verify();
        empRefService.VerifyNoOtherCalls();
    }

    private static void SetUpControllerContextWithAuthHeader(
        ApprenticeshipLevyController controller,
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
