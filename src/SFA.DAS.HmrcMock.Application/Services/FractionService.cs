using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using SFA.DAS.HmrcMock.Application.Helpers;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IFractionService
{
    Task<EnglishFractionDeclarationsResponse> GetByEmpRef(string empRef);

    Task CreateFractionAsync(string empref);
}

[ExcludeFromCodeCoverage]
public class MongoFractionService(IMongoDatabase database) : BaseMongoService<EnglishFractionDeclarationsResponse>(database, "fractions"), IFractionService
{
    public async Task<EnglishFractionDeclarationsResponse> GetByEmpRef(string empref)
    {
        var filter = Builders<EnglishFractionDeclarationsResponse>.Filter.Eq("empref", empref);
        return await FindOne(filter);
    }

    public async Task CreateFractionAsync(string empref)
    {
        await CreateOne(new EnglishFractionDeclarationsResponse
        {
            EmpRef = empref,
            FractionCalcResponses =
            [
                new FractionCalcResponse
                {
                    CalculatedAt = DateTime.UtcNow,
                    Fractions =
                    [
                        new FractionResponse
                        {
                            Region = "England",
                            Value = "1.00"
                        }
                    ]
                }
            ]
        });
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
    [BsonSerializer(typeof(DateAsStringSerializer))]
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