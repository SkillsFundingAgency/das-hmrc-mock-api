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
        string dateString = string.Empty;

        switch (bsonType)
        {
            case BsonType.DateTime:
            {
                var millisecondsSinceEpoch = context.Reader.ReadDateTime(); // Gets the long value (milliseconds since 1970)
                return DateTimeOffset.FromUnixTimeMilliseconds(millisecondsSinceEpoch).UtcDateTime;
            }
            case BsonType.String:
            {
                dateString = context.Reader.ReadString();
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    return date;
                }

                break;
            }
        }


        throw new BsonSerializationException($"Cannot deserialize BsonType {bsonType} value {dateString} to DateTime.");
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
            throw new BsonSerializationException($"Cannot serialize BsonType {value.GetType()} to DateTime.");
        }
    }

    public Type ValueType => typeof(DateTime);
}
