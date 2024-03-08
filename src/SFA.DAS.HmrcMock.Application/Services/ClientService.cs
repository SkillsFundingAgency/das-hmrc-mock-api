using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IClientService
{
    Task<Application> GetById(string? clientId);
}

public class MongoClientService(IMongoDatabase database) : IClientService
{
    private readonly IMongoCollection<Application> _collection =
        database.GetCollection<Application>("applications");

    public async Task<Application> GetById(string? clientId)
    {
        var filter = Builders<Application>.Filter.Eq(a => a.ClientId, clientId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}

[BsonIgnoreExtraElements]
public class Application
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("name")]
    public string Name { get; set; }
    [BsonElement("applicationID")]
    public string ApplicationId { get; set; }
    [BsonElement("clientID")]
    public string ClientId { get; set; }
    [BsonElement("clientSecret")]
    public string ClientSecret { get; set; }
    [BsonElement("serverToken")]
    public string ServerToken { get; set; }
    [BsonElement("privilegedAccess")]
    public bool PrivilegedAccess { get; set; }
}