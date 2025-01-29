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

public class FractionsControllerTests
{
    private const string TestEmpRef = "123/ABC";
    
    [Test, MoqAutoData]
    public async Task Fractions_ShouldReturnBadRequest_WhenNoAuthorizationHeader(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] FractionsController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.Fractions(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Fractions_ShouldReturnBadRequest_WhenEmpty_AuthorizationHeader(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] FractionsController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        httpContext.Request.Headers.Authorization = string.Empty;
        
        // Act
        var result = await controller.Fractions(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Fractions_ShouldReturnBadRequest_WhenUserIsNotFound(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Greedy] FractionsController controller)
    {
        // Arrange
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService);

        // Act
        var result = await controller.Fractions(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        authRecordService.Verify();
        authRecordService.VerifyNoOtherCalls();
        gatewayUserService.Verify();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Fractions_ShouldReturnNotFound_WhenNoDeclarationsForEmpRef(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Frozen] Mock<IFractionService> fractionService,
        [Greedy] FractionsController controller)
    {
        // Arrange
        var testUser = new GatewayUserResponse { Empref = TestEmpRef };
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService, testUser);
        fractionService
            .Setup(x => x.GetByEmpRef(TestEmpRef))
            .ReturnsAsync((EnglishFractionDeclarationsResponse)null)
            .Verifiable();

        // Act
        var result = await controller.Fractions(TestEmpRef, null, null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        fractionService.Verify();
        fractionService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task Fractions_ShouldReturnOrderedResponse_WhenFractionsExistForEmpRef(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [Frozen] Mock<IFractionService> fractionService,
        [Greedy] FractionsController controller)
    {
        // Arrange
        var testDeclarations = new EnglishFractionDeclarationsResponse
        {
            FractionCalcResponses = 
            [
                new FractionCalcResponse
                {
                    CalculatedAt = DateTime.Today.AddMonths(-2),
                   Fractions =
                   [
                       new FractionResponse
                       {
                           Region = "England",
                           Value = "0.99"
                       }
                   ]
                }
            ]
        };
        
        var testUser = new GatewayUserResponse { Empref = TestEmpRef };
        SetUpControllerContextWithAuthHeader(controller, authRecordService, gatewayUserService, testUser);
        fractionService
            .Setup(x => x.GetByEmpRef(TestEmpRef))
            .ReturnsAsync(testDeclarations)
            .Verifiable();

        // Act
        var result = await controller.Fractions(TestEmpRef, new DateTime(2024, 1, 1), new DateTime(2025, 1, 1));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>();
        okResult.Subject.Value.Should().BeEquivalentTo(testDeclarations);
        
        fractionService.Verify();
        fractionService.VerifyNoOtherCalls();
    }

    [Test, MoqAutoData]
    public async Task CalculationDate_ShouldReturnNotFound_WhenNotExists(
        [Frozen] Mock<IFractionCalcService> fractionCalcService,
        [Greedy] FractionsController controller)
    {
        // Arrange
        fractionCalcService
            .Setup(x => x.LastCalculationDate())
            .ReturnsAsync((FractionCalculationResponse?)null)
            .Verifiable();
        
        // Act
        var result = await controller.CalculationDate();
        
        // Assert
        result.Should().BeOfType<NotFoundResult>();
        fractionCalcService.Verify();
        fractionCalcService.VerifyNoOtherCalls();
    }

    [Test, MoqAutoData]
    public async Task CalculationDate_ShouldReturnLastCalculationDate_WhenExists(
        [Frozen] Mock<IFractionCalcService> fractionCalcService,
        [Greedy] FractionsController controller)
    {
        // Arrange
        var lastCalculationDate = new FractionCalculationResponse
        {
            Id = new ObjectId("0af74e60cf0143b987c36f601dc0110c"),
            LastCalculationDate = DateTime.Today.AddMonths(-1)
        };

        fractionCalcService
            .Setup(x => x.LastCalculationDate())
            .ReturnsAsync(lastCalculationDate)
            .Verifiable();

        // Act
        var result = await controller.CalculationDate();

        // Assert
        var okResult = result.Should().BeAssignableTo<OkObjectResult>();
        okResult.Subject.Value.Should().BeEquivalentTo(lastCalculationDate.LastCalculationDate);
        fractionCalcService.Verify();
        fractionCalcService.VerifyNoOtherCalls();
    }

    private static void SetUpControllerContextWithAuthHeader(
        FractionsController controller,
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
