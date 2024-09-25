using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace SFA.DAS.HmrcMock.Application.Helpers;

public class DateAsStringSerializer : IBsonSerializer<DateTime>
{
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value)
    {
        var dateString = value.ToString("yyyy-MM-dd");
        context.Writer.WriteString(dateString);
    }

    public DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();
        if (bsonType == BsonType.String)
        {
            var dateString = context.Reader.ReadString();
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        throw new BsonSerializationException($"Cannot deserialize BsonType {bsonType}, value {context.Reader.ReadString()} to DateTime.");
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value is DateTime date)
        {
            Serialize(context, args, date);
        }
        else
        {
            throw new BsonSerializationException($"Cannot Serialize BsonType {value.GetType()} to DateTime.");
        }
    }

    public Type ValueType => typeof(DateTime);
}
