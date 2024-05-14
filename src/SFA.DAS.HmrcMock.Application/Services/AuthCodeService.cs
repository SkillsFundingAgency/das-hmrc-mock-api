using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IAuthCodeService
{
    Task<AuthCodeRow> Find(string code);
    Task<int> Delete(string code);
    Task<int> Insert(AuthCodeRow authCode);
}

public class MongoAuthCodeService(IMongoDatabase database) : BaseMongoService<AuthCodeRow>(database, "sys_auth_codes"), IAuthCodeService
{
    public async Task<AuthCodeRow> Find(string code)
    {
        var filter = Builders<AuthCodeRow>.Filter.Eq(authCode => authCode.AuthorizationCode, code);
        var result = await FindOne(filter);
        return result;
    }

    public async Task<int> Delete(string code)
    {
        var filter = Builders<AuthCodeRow>.Filter.Eq(authCode => authCode.AuthorizationCode, code);
        var result = await _collection.DeleteOneAsync(filter);
        return (int)result.DeletedCount;
    }

    public async Task<int> Insert(AuthCodeRow authCode)
    {
        await _collection.InsertOneAsync(authCode);
        return 1; // Assuming always one inserted
    }
}

[BsonIgnoreExtraElements]
public class AuthCodeRow
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("authorizationCode")]
    public string AuthorizationCode { get; set; }
    [BsonElement("gatewayId")]
    public string GatewayUserId { get; set; }
    [BsonElement("redirectUri")]
    public string RedirectUri { get; set; }
    [BsonElement("createdAt")]
    public DateTime IssueDateTime { get; set; }
    [BsonElement("scope")]
    public string Scope { get; set; }
    [BsonElement("clientId")]
    public string? ClientId { get; set; }
    [BsonElement("expiresIn")]
    public long ExpirationSeconds { get; set; }
}