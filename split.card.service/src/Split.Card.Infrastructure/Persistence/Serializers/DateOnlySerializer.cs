using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace SplitCard.Infrastructure.Persistence.Serializers;

/// <summary>
/// MongoDB.Driver does not natively serialize System.DateOnly. This serializer stores it
/// as a UTC DateTime at midnight, so range queries (>=, <=) against DateOnly fields work
/// correctly at the BSON level.
/// </summary>
public sealed class DateOnlySerializer : SerializerBase<DateOnly>
{
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var dateTime = context.Reader.ReadDateTime();
        var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(dateTime).UtcDateTime;
        return DateOnly.FromDateTime(utcDateTime);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        var utcDateTime = value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var milliseconds = new DateTimeOffset(utcDateTime).ToUnixTimeMilliseconds();
        context.Writer.WriteDateTime(milliseconds);
    }
}
