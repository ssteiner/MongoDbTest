using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NoSqlModels;

namespace MongoDbTest.Extensions;

internal class PhoneBookContactNumberSerializer : SerializerBase<PhoneBookContactNumber>
{
    public override PhoneBookContactNumber Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        return new PhoneBookContactNumber
        {
            Id = document["_id"].AsObjectId.ToString(),
            Number = document["Number"].AsString,
            Type = (NumberType)Enum.Parse(typeof(NumberType), document["Type"].AsString)
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PhoneBookContactNumber value)
    {
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Number", value.Number },
            { "Type", value.Type.ToString() }
        };
        BsonSerializer.Serialize(context.Writer, document);
    }
}
