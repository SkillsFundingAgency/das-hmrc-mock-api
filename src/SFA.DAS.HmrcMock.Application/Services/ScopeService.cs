using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IScopeService
{
    Task<Scope> GetByName(string name);
}

[ExcludeFromCodeCoverage]
public class MongoScopeService(IMongoDatabase database) : BaseMongoService<Scope>(database, "sys_scopes"), IScopeService
{
    public async Task<Scope> GetByName(string name)
    {
        var filter = Builders<Scope>.Filter.Eq(s => s.Name, name);
        return await FindOne(filter);
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