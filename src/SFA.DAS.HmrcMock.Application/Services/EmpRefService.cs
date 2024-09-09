using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IEmpRefService
{
    Task<EmpRefResponse> GetByEmpRef(string empRef);
    
    Task CreateEmpRef(string empRef);
}

public class MongoEmpRefService(IMongoDatabase database) : BaseMongoService<EmpRefResponse>(database, "emprefs"), IEmpRefService
{
    public async Task<EmpRefResponse> GetByEmpRef(string empref)
    {
        var filter = Builders<EmpRefResponse>.Filter.Eq("empref", empref);
        return await FindOne(filter);
    }

    public async Task CreateEmpRef(string empRef)
    {
        var encodedEmpRef = Uri.EscapeDataString(empRef);
        var linkBase = $"/epaye/{encodedEmpRef}";
        var empRefDto = new EmpRefResponse
        {
            EmpRef = empRef,
            Links = new Links
            {
              Self  = new LinkObject{ Href =  $"{linkBase}" },
              Declarations  = new LinkObject{ Href =  $"{linkBase}/declarations" },
              Fractions  = new LinkObject{ Href =  $"{linkBase}/fractions" },
              EmploymentCheck  = new LinkObject{ Href =  $"{linkBase}/employed" },
            },
            Employer = new Employer
            {
                Name = new Name
                {
                    NameLine1 = empRef
                }
            }
        };

        await CreateOne(empRefDto);
    }
}

[BsonIgnoreExtraElements]
public class EmpRefResponse
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("_links")]
    public Links Links { get; set; }
    [BsonElement("empref")]
    public string EmpRef { get; set; }
    [BsonElement("employer")]
    public Employer Employer { get; set; }
}

[BsonIgnoreExtraElements]
public class Links
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("self")]
    public LinkObject Self { get; set; }
    [BsonElement("declarations")]
    public LinkObject Declarations { get; set; }
    [BsonElement("fractions")]
    public LinkObject Fractions { get; set; }
    [BsonElement("employment-check")]
    public LinkObject EmploymentCheck { get; set; }
}

[BsonIgnoreExtraElements]
public class Employer
{
    [BsonElement("name")]
    public Name Name { get; set; }
}

[BsonIgnoreExtraElements]
public class Name
{
    [BsonElement("nameLine1")]
    public string NameLine1 { get; set; }
}

[BsonIgnoreExtraElements]
public class LinkObject
{
    [BsonElement("href")]
    public string Href { get; set; }
}