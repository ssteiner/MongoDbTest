using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDbTest.Conventions;

internal class StringObjectIdGeneratorConvention : ConventionBase, IPostProcessingConvention
{
    /// <summary>
    /// Applies a post processing modification to the class map.
    /// </summary>
    /// <param name="classMap">The class map.</param>
    public void PostProcess(BsonClassMap classMap)
    {
        var idMemberMap = classMap.IdMemberMap;
        if (idMemberMap == null || idMemberMap.IdGenerator != null)
            return;
        if (idMemberMap.MemberType == typeof(string))
        {
            idMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance).SetSerializer(new StringSerializer(BsonType.ObjectId));
        }
    }
}
