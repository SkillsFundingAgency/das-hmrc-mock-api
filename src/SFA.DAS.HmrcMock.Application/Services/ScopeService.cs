using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IScopeService
{
    Task<Scope> GetByName(string name);
}

public class MongoScopeService(IMongoDatabase database) : IScopeService
{
    private readonly IMongoCollection<Scope> _collection = database.GetCollection<Scope>("sys_scopes");

    public async Task<Scope> GetByName(string name)
    {
        var filter = Builders<Scope>.Filter.Eq(s => s.Name, name);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}

[BsonIgnoreExtraElements]
public class Scope
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]  
    [BsonElement("_id")]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }
}