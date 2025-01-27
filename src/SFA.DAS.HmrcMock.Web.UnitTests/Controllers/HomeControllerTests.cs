using AutoFixture.NUnit3;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Controllers;
using SFA.DAS.HmrcMock.Web.Models;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers;
public class HomeControllerTests
{
    [Test, MoqAutoData]
    public void Get_SignIn_Then_The_View_Is_Returned(
        [Greedy] HomeController controller)
    {
        // Act
        var actual = controller.SignIn();

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeAssignableTo<ViewResult>();
    }
    
    [Test, MoqAutoData]
    public void Get_SignIn_Then_The_View_Is_Returned_With_ViewModel_Properties(
        string redirectUrl,
        string origin,
        [Greedy] HomeController controller)
    {
        // Act
        var actual = controller.SignIn(redirectUrl, origin);

        // Assert
        actual.Should().NotBeNull();
        var viewResult = actual.Should().BeAssignableTo<ViewResult>();
        var viewModel = viewResult.Subject.Model.Should().BeAssignableTo<SigninViewModel>();
        viewModel.Subject.Continue.Should().Be(redirectUrl);
        viewModel.Subject.Origin.Should().Be(origin);
    }
    
    [Test, MoqAutoData]
    public async Task Post_SignIn_Invalid_ModelState_Return_To_SignIn(
        SigninViewModel viewModel,
        [NoAutoProperties] HomeController controller)
    {
        // Arrange
        viewModel = viewModel with
        {
            Password = string.Empty
        };
        controller.ModelState.AddModelError("Password", "Password is required");
        
        // Act
        var actual = await controller.SignIn(viewModel);

        // Assert
        actual.Should().NotBeNull();
        var viewResult = actual.Should().BeAssignableTo<ViewResult>();
        var resultViewModel = viewResult.Subject.Model.Should().BeAssignableTo<SigninViewModel>();
        resultViewModel.Subject.Should().BeEquivalentTo(viewModel);
    }
    
    [Test]
    [MoqInlineAutoData("some-string")]
    [MoqInlineAutoData("LE_11")]
    [MoqInlineAutoData("NL_22")]
    [MoqInlineAutoData("LE_AB_9999")]
    [MoqInlineAutoData("NL_CD_8888")]
    [MoqInlineAutoData("LE_1Z_9999")]
    [MoqInlineAutoData("NL_00_8888")]
    public async Task Post_SignIn_Invalid_UserId_Format_Return_Error(
        string userId,
        SigninViewModel viewModel,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [NoAutoProperties] HomeController controller)
    {
        // Arrange
        viewModel = viewModel with
        {
            UserId = userId
        };
        
        gatewayUserService
            .Setup(g => g.ValidateAsync(viewModel.UserId, viewModel.Password))
            .ReturnsAsync((GatewayUserResponse)null)
            .Verifiable();
        
        // Act
        var actual = await controller.SignIn(viewModel);

        // Assert
        actual.Should().NotBeNull();
        var viewResult = actual.Should().BeAssignableTo<ViewResult>();
        controller.ModelState.Should().ContainKey("Username");
        gatewayUserService.Verify();
        gatewayUserService.VerifyNoOtherCalls();
    }
    
    [Test]
    [MoqInlineAutoData("LE_99_9999")]
    [MoqInlineAutoData("NL_1_8888")]
    public async Task Post_SignIn_Valid_UserId_Format_Create_Gateway_User(
        string userId,
        SigninViewModel viewModel,
        [Frozen] Mock<IGatewayUserService> gatewayUserService,
        [NoAutoProperties] HomeController controller)
    {
        // Arrange
        viewModel = viewModel with
        {
            UserId = userId
        };
        
        gatewayUserService
            .Setup(g => g.ValidateAsync(viewModel.UserId, viewModel.Password))
            .ReturnsAsync((GatewayUserResponse)null)
            .Verifiable();
        
        // Act
        var actual = await controller.SignIn(viewModel);

        // Assert
        gatewayUserService.Verify(x => x.CreateGatewayUserAsync(It.Is<string>(s => s.StartsWith(viewModel.UserId)), viewModel.Password));
    }
}
