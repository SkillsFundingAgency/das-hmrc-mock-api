using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IFractionService
{
    Task<EnglishFractionDeclarationsResponse> GetByEmpRef(string empRef);
}

public class MongoFractionService(IMongoDatabase database) : BaseMongoService<EnglishFractionDeclarationsResponse>(database, "fractions"), IFractionService
{
    public async Task<EnglishFractionDeclarationsResponse> GetByEmpRef(string empref)
    {
        var filter = Builders<EnglishFractionDeclarationsResponse>.Filter.Eq("empref", empref);
        return await FindOne(filter);
    }
}

[BsonIgnoreExtraElements]
public class EnglishFractionDeclarationsResponse
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("empref")]
    public string EmpRef { get; set; }
    [BsonElement("fractionCalculations")]
    public List<FractionCalcResponse> FractionCalcResponses { get; set; }
}

[BsonIgnoreExtraElements]
public class FractionCalcResponse
{
    [BsonElement("calculatedAt")]
    public DateTime CalculatedAt { get; set; }
    [BsonElement("fractions")]
    public List<FractionResponse> Fractions { get; set; }
}

[BsonIgnoreExtraElements]
public class FractionResponse
{
    [BsonElement("region")]
    public string Region { get; set; }
    [BsonElement("value")]
    public string Value { get; set; }
}