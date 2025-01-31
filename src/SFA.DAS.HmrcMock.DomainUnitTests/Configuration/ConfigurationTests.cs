using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Domain.Configuration;

namespace SFA.DAS.HmrcMock.Domain.UnitTests.Configuration;

public class ConfigurationTests
{
    [Test]
    public void HmrcMockConfiguration()
    {
        // Arrange
        var config = new HmrcMockConfiguration
        {
            DataProtectionKeysDatabase = "Default=1",
            RedisConnectionString = "127.0.0.1"
        };
        
        // Assert
        config.RedisConnectionString.Should().Be("127.0.0.1");
        config.DataProtectionKeysDatabase.Should().Be("Default=1");
    }
    
    [Test]
    public void MongoDbOptions()
    {
        // Arrange
        var config = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017"
        };
        
        // Assert
        config.ConnectionString.Should().Be("mongodb://localhost:27017");
        
    }
}