using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface IGatewayUserService
{
    Task<GatewayUserResponse> ValidateAsync(string userId, string password);
    Task<GatewayUserResponse> GetByGatewayIdAsync(string gatewayID);
    Task CreateGatewayUserAsync(string userId, string password);
}

public class MongoGatewayUserService(IMongoDatabase database) 
    : BaseMongoService<GatewayUserResponse>(database, "gateway_users"), IGatewayUserService
{
    public async Task<GatewayUserResponse> GetByGatewayIdAsync(string gatewayId)
    {
        var filter = Builders<GatewayUserResponse>.Filter.Eq(u => u.GatewayID, gatewayId);
        return await FindOne(filter);
    }

    public async Task CreateGatewayUserAsync(string userId, string password)
    {
        var user = new GatewayUserResponse
        {
            GatewayID = userId,
            Name = userId,
            Password = password,
            Empref = GeneratePAYEReference()
        };

        await CreateOne(user);
    }

    public async Task<GatewayUserResponse> GetByEmprefAsync(string empref)
    {
        var filter = Builders<GatewayUserResponse>.Filter.Eq(u => u.Empref, empref);
        return await FindOne(filter);
    }

    public async Task<GatewayUserResponse> ValidateAsync(string gatewayID, string password)
    {
        var filter = Builders<GatewayUserResponse>.Filter.Eq(u => u.GatewayID, gatewayID) &
                     Builders<GatewayUserResponse>.Filter.Eq(u => u.Password, password);
        var user = await _collection.Find(filter).FirstOrDefaultAsync();

        return user;
    }
    
    private static readonly Random Random = new();

    public static string GeneratePAYEReference()
    {
        // Generate tax office number (3 digits)
        string taxOfficeNumber = Random.Next(100, 1000).ToString(); // Generates a number between 100 and 999

        // Generate employer reference (1-3 letters followed by 4-6 digits)
        string employerReference = GenerateEmployerReference();

        // Combine them in the format 123/A12345
        return $"{taxOfficeNumber}/{employerReference}";
    }

    private static string GenerateEmployerReference()
    {
        StringBuilder reference = new StringBuilder();

        // 1-3 uppercase letters
        var lettersCount = Random.Next(1, 4); // Generate between 1 and 3 letters
        for (var i = 0; i < lettersCount; i++)
        {
            var randomLetter = (char)Random.Next('A', 'Z' + 1); // Generates a random uppercase letter
            reference.Append(randomLetter);
        }

        // 4-6 digits
        var digitsCount = Random.Next(4, 7); // Generate between 4 and 6 digits
        for (var i = 0; i < digitsCount; i++)
        {
            reference.Append(Random.Next(0, 10)); // Appends a random digit
        }

        return reference.ToString();
    }
}

[BsonIgnoreExtraElements]
public class GatewayUserResponse
{
    [BsonElement("gatewayID")]
    public string GatewayID { get; set; }

    [BsonElement("password")]
    public string Password { get; set; }

    [BsonElement("empref")]
    public string Empref { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("require2SV")]
    public bool? Require2SV { get; set; }
}