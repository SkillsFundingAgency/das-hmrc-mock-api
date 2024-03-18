using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IFractionCalcService
{
    Task<FractionCalculationResponse?> LastCalculationDate();
}

public class MongoFractionCalcService(IMongoDatabase database) 
    : BaseMongoService<FractionCalculationResponse>(database, "fraction_calculation_date"), IFractionCalcService
{
    public async Task<FractionCalculationResponse> LastCalculationDate()
    {
        var filter = Builders<FractionCalculationResponse>.Filter.Eq(_ => true, true);
        var document = await FindOne(filter);
        return document;
    }
}

[BsonIgnoreExtraElements]
public class FractionCalculationResponse
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("lastCalculationDate")]
    public DateTime LastCalculationDate { get; set; }
}