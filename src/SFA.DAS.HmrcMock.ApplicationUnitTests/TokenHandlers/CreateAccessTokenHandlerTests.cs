using AutoFixture.NUnit3;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Application.TokenHandlers;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Application.UnitTests.TokenHandlers;

public class CreateAccessTokenHandlerTests
{
    [Test, MoqAutoData]
    public async Task CreateAccessToken_ShouldCreateNonPrivilegedRecord_WhenClientIdNull(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Greedy] CreateAccessTokenHandler sut)
    {
        // Arrange
        var authCodeRow = new AuthCodeRow
        {
            AuthorizationCode = "code-123344",
            ClientId = null,
            ExpirationSeconds = 100,
            GatewayUserId = "user-id",
            Scope = "scope",
            RedirectUri = "redirect-uri"
        };

        // Act
        await sut.CreateAccessTokenAsync(authCodeRow);

        // Assert
        authRecordService.Verify(x => x.Insert(It.Is<AuthRecord>(ar => ar.Privileged == false)), Times.Once);
    }
    
    [Test]
    [MoqInlineAutoData(false)]
    [MoqInlineAutoData(true)]
    public async Task CreateAccessToken_ShouldInsertRecord_WithMatchingPrivilegedStatus(
        bool isPrivileged,
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Frozen] Mock<IClientService> clientService,
        [Greedy] CreateAccessTokenHandler sut)
    {
        // Arrange
        var authCodeRow = new AuthCodeRow
        {
            AuthorizationCode = "code-123344",
            ClientId = "client-id",
            ExpirationSeconds = 100,
            GatewayUserId = "user-id",
            Scope = "scope",
            RedirectUri = "redirect-uri"
        };

        var application = new Services.Application
        {
            ClientId = "client-id",
            PrivilegedAccess = isPrivileged
        };

        clientService
            .Setup(x => x.GetById(authCodeRow.ClientId))
            .ReturnsAsync(application)
            .Verifiable();

        // Act
        await sut.CreateAccessTokenAsync(authCodeRow);

        // Assert
        authRecordService.Verify(
            x => x.Insert(It.Is<AuthRecord>(ar => ar.Privileged == isPrivileged && ar.ClientId == "client-id")), 
            Times.Once);
    }
    
    [Test, MoqAutoData]
    public async Task RefreshAccessToken_ShouldRaiseException_WhenRefreshTokenInvalid(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Greedy] CreateAccessTokenHandler sut)
    {
        // Arrange
        const string refreshToken = "refresh-token";
        authRecordService
            .Setup(x => x.FindByRefreshToken(refreshToken))
            .ReturnsAsync((AuthRecord) null)
            .Verifiable();

        // Act
        var act = () => sut.RefreshAccessTokenAsync(refreshToken);

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
        authRecordService.Verify();
        authRecordService.VerifyNoOtherCalls();
    }
    
    [Test, MoqAutoData]
    public async Task RefreshAccessToken_ShouldRaiseException_WhenRefreshTokenIsExpired(
        [Frozen] Mock<IAuthRecordService> authRecordService,
        [Greedy] CreateAccessTokenHandler sut)
    {
        // Arrange
        const string refreshToken = "refresh-token";

        var authRecord = new AuthRecord
        {
            ClientId = "client-id",
            RefreshToken = refreshToken,
            CreatedAt = DateTime.Now.AddMonths(-19),
            ExpiresIn = 100
        };
        
        authRecordService
            .Setup(x => x.FindByRefreshToken(refreshToken))
            .ReturnsAsync(authRecord)
            .Verifiable();

        // Act
        var act = () => sut.RefreshAccessTokenAsync(refreshToken);

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
        authRecordService.Verify();
        authRecordService.VerifyNoOtherCalls();
    }
}