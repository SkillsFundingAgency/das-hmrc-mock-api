using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IAuthRecordService
{
    Task<int> Insert(AuthRecord authRecord);
    Task<int> Update(AuthRecord authRecord);
    Task<AuthRecord> Find(string accessToken);
    Task<AuthRecord> FindByRefreshToken(string refreshToken);
}

[ExcludeFromCodeCoverage]
public class MongoAuthRecordService(IMongoDatabase database) : BaseMongoService<AuthRecord>(database, "sys_auth_records"), IAuthRecordService
{
    public async Task<AuthRecord> Find(string accessToken)
    {
        var filter = Builders<AuthRecord>.Filter.Eq(authCode => authCode.AccessToken, accessToken);
        var result = await FindOne(filter);
        return result;
    }
    
    public async Task<AuthRecord> FindByRefreshToken(string refreshToken)
    {
        var filter = Builders<AuthRecord>.Filter.Eq(authCode => authCode.RefreshToken, refreshToken);
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
    
    public async Task<int> Update(AuthRecord authRecord)
    {
        var filter = Builders<AuthRecord>.Filter.Eq(record => record.Id, authRecord.Id);
        var updateOptions = new ReplaceOptions { IsUpsert = false };
        var result = await _collection.ReplaceOneAsync(filter, authRecord, updateOptions);

        return (int)result.ModifiedCount;
    }
}

[BsonIgnoreExtraElements]
public class AuthRecord
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }

    [BsonElement("accessToken")] public string AccessToken { get; set; }
    [BsonElement("refreshToken")] public string RefreshToken { get; set; }
    [BsonElement("gatewayID")] public string GatewayId { get; set; }
    [BsonElement("scope")] public string Scope { get; set; }
    [BsonElement("expiresIn")] public long ExpiresIn { get; set; }
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
    [BsonElement("clientID")] public string? ClientId { get; set; }
    [BsonElement("privileged")] public bool Privileged { get; set; }
    [BsonElement("refreshedAt")] public DateTime? RefreshedAt { get; set; }

    private const long EighteenMonthsInMillis = 18L * 30L * 24L * 60L * 60L * 1000L;

    private long AccessTokenExpiresAt => (RefreshedAt ?? CreatedAt).Ticks / TimeSpan.TicksPerMillisecond + ExpiresIn * 1000L;
    private long RefreshTokenExpiresAt => CreatedAt.Ticks / TimeSpan.TicksPerMillisecond + EighteenMonthsInMillis;

    public bool IsAccessTokenExpired(long referenceTimeInMillis)
    {
        return AccessTokenExpiresAt <= referenceTimeInMillis;
    }

    public bool IsRefreshTokenExpired(long referenceTimeInMillis)
    {
        return RefreshTokenExpiresAt <= referenceTimeInMillis;
    }
}