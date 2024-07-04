using GenericProvisioningLib;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;

namespace MongoDbEntitiesTest;

internal class MongoDatabaseContext(IMongoDatabase database, IUserInformation user)
{
    internal IUserInformation User { get; private set; } = user;

    internal IMongoDatabase Database => database;

    internal IMongoCollection<T> GetCollection<T>(string entityName = null) where T : class, IEntity
    {
        return DB.Collection<T>();
    }

    //internal IMongoCollection<BsonDocument> GetCollection(string name)
    //{
    //    return DB.Collection<BsonDocument>();
    //}

    internal IMongoQueryable<T> AccessibleObjects<T>(bool applyPermissions, string entityName = null) where T : class, IEntity
    {
        IMongoQueryable<T> set = DB.Queryable<T>();
        return set;
    }
}
