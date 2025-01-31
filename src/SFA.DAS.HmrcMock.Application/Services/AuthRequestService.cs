using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IAuthRequestService
{
    Task<string> Save(AuthRequest authRequest);
    Task<AuthRequest> Get(string id);
    Task<AuthRequest> Delete(string id);
}

[ExcludeFromCodeCoverage]
public class MongoAuthRequestService(IMongoDatabase database) : BaseMongoService<AuthRequest>(database, "sys_auth_requests"), IAuthRequestService
{
    public async Task<string> Save(AuthRequest authRequest)
    {
        authRequest.Id = ObjectId.GenerateNewId();
        await _collection.InsertOneAsync(authRequest);
        return authRequest.Id.ToString();
    }
    
    public async Task<AuthRequest> Delete(string id)
    {
        var filter = Builders<AuthRequest>.Filter.Eq("_id", ObjectId.Parse(id));
        var result = await _collection.FindOneAndDeleteAsync(filter);
        return result;
    }

    public async Task<AuthRequest> Get(string id)
    {
        var filter = Builders<AuthRequest>.Filter.Eq("_id", ObjectId.Parse(id));
        var result = await FindOne(filter);
        return result;
    }
}

[BsonIgnoreExtraElements]
public class AuthRequest
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("scope")]
    public string Scope { get; set; }
    [BsonElement("clientId")]
    public string? ClientId { get; set; }
    [BsonElement("redirectUri")]
    public string RedirectUri { get; set; }
    [BsonElement("creationDate")]
    public DateTime CreationDate { get; set; }
}