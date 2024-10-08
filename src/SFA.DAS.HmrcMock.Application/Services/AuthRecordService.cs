using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IAuthRecordService
{
    Task<int> Insert(AuthRecord authRecord);

    Task<AuthRecord> Find(string id);
}

public class MongoAuthRecordService(IMongoDatabase database) : BaseMongoService<AuthRecord>(database, "sys_auth_records"), IAuthRecordService
{
    public async Task<AuthRecord> Find(string accessToken)
    {
        var filter = Builders<AuthRecord>.Filter.Eq(authCode => authCode.AccessToken, accessToken);
        var result = await FindOne(filter);
        return result;
    }

    public async Task<int> Delete(string Id)
    {
        var filter = Builders<AuthRecord>.Filter.Eq(authCode => authCode.Id, ObjectId.Parse(Id));
        var result = await _collection.DeleteOneAsync(filter);
        return (int)result.DeletedCount;
    }

    public async Task<int> Insert(AuthRecord authRecord)
    {
        await _collection.InsertOneAsync(authRecord);
        return 1; // Assuming always one inserted
    }
}

[BsonIgnoreExtraElements]
public class AuthRecord
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("accessToken")]
    public string AccessToken { get; set; }
    [BsonElement("refreshToken")]
    public string RefreshToken { get; set; }
    [BsonElement("gatewayID")]
    public string GatewayId { get; set; }
    [BsonElement("scope")]
    public string Scope { get; set; }
    [BsonElement("expiresIn")]
    public long ExpiresIn { get; set; }
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    [BsonElement("clientID")]
    public string? ClientId { get; set; }
    [BsonElement("privileged")]
    public bool Privileged { get; set; }
    [BsonElement("refreshedAt")]
    public DateTime? RefreshedAt { get; set; }
}