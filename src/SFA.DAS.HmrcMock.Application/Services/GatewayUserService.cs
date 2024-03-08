using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IGatewayUserService
{
    Task<GatewayUserResponse> ValidateAsync(string userId, string password);
    Task<GatewayUserResponse> GetByGatewayIdAsync(string gatewayID);
}

public class MongoGatewayUserService(IMongoDatabase database) : IGatewayUserService
{
    private readonly IMongoCollection<GatewayUserResponse> _collection =
        database.GetCollection<GatewayUserResponse>("gateway_users");

    public async Task<GatewayUserResponse> GetByGatewayIdAsync(string gatewayId)
    {
        var filter = Builders<GatewayUserResponse>.Filter.Eq(u => u.GatewayID, gatewayId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<GatewayUserResponse> GetByEmprefAsync(string empref)
    {
        var filter = Builders<GatewayUserResponse>.Filter.Eq(u => u.Empref, empref);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<GatewayUserResponse> ValidateAsync(string gatewayID, string password)
    {
        var filter = Builders<GatewayUserResponse>.Filter.Eq(u => u.GatewayID, gatewayID) &
                     Builders<GatewayUserResponse>.Filter.Eq(u => u.Password, password);
        var user = await _collection.Find(filter).FirstOrDefaultAsync();

        return user;
    }
}

[BsonIgnoreExtraElements]
public class GatewayUserResponse
{
    [BsonElement("gatewayID")]
    public string GatewayID { get; set; }

    [BsonElement("password")]
    public string Password { get; set; }

    [BsonElement("empref")]
    public string Empref { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("require2SV")]
    public bool? Require2SV { get; set; }
}