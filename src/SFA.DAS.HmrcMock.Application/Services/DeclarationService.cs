using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface ILevyDeclarationService
{
    Task<LevyDeclarationResponse> GetByEmpRef(string empRef);
    
    Task CreateDeclarationsAsync(string empref, int numberOfDeclarations, long amount);
}

[ExcludeFromCodeCoverage]
public class MongoLevyDeclarationService(IMongoDatabase database) : BaseMongoService<LevyDeclarationResponse>(database, "declarations"), ILevyDeclarationService
{
    public async Task<LevyDeclarationResponse> GetByEmpRef(string empref)
    {
        var filter = Builders<LevyDeclarationResponse>.Filter.Eq("empref", empref);
        return await FindOne(filter);
    }

    public async Task CreateDeclarationsAsync(string empref, int numberOfDeclarations, long amount)
    {
        var declarations = new List<DeclarationResponse>();
        long levyDueYtd = 0;

        for (var i = numberOfDeclarations; i > 0; i--)
        {
            _ = long.TryParse(DateTime.Now.ToString("yssfffffff"), out var declarationId);
            var submissionDate = DateTime.Now.AddMonths(-i);
            levyDueYtd += amount;
            
            var payrollYear = GetPayrollYear(submissionDate);
            var payrollMonth = GetPayrollMonth(submissionDate);

            declarations.Add(new DeclarationResponse
            {
                DeclarationId = declarationId,
                SubmissionTime = submissionDate,
                LevyDueYTD = levyDueYtd,
                LevyAllowanceForFullYear = 15000,
                PayrollPeriod = new PayrollPeriodResponse
                {
                    Year = payrollYear,
                    Month = payrollMonth,
                }
            });
        }

        var levyDeclarationsDto = new LevyDeclarationResponse
        {
            EmpRef = empref,
            Declarations = declarations
        };

        await CreateOne(levyDeclarationsDto);
    }

    private static string GetPayrollYear(DateTime submissionDate)
    {
        // If the date is before April, it belongs to the next payroll year
        int startYear = submissionDate.Month < 4 ? submissionDate.Year - 1 : submissionDate.Year;
        int endYear = startYear + 1;
    
        // Format as "YY-YY"
        return $"{startYear % 100:00}-{endYear % 100:00}";
    }

    private static int GetPayrollMonth(DateTime submissionDate)
    {
        return submissionDate.Month >= 4 ? submissionDate.Month - 3 : submissionDate.Month + 9;
    }

}

[BsonIgnoreExtraElements]
public class LevyDeclarationResponse
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("empref")]
    public string EmpRef { get; set; }
    [BsonElement("declarations")]
    public List<DeclarationResponse> Declarations { get; set; }
}

[BsonIgnoreExtraElements]
public class DeclarationResponse
{
    [BsonElement("id")]
    public long DeclarationId { get; set; }
    [BsonElement("submissionTime")]
    public DateTime SubmissionTime { get; set; }
    [BsonElement("payrollPeriod")]
    public PayrollPeriodResponse PayrollPeriod { get; set; }
    [BsonElement("levyDueYTD")]
    public long LevyDueYTD { get; set; }
    [BsonElement("levyAllowanceForFullYear")]
    public long LevyAllowanceForFullYear { get; set; }

    public long Id => DeclarationId;
}

[BsonIgnoreExtraElements]
public class PayrollPeriodResponse
{
    [BsonElement("year")]
    public string Year { get; set; }
    [BsonElement("month")]
    public int Month { get; set; }
}