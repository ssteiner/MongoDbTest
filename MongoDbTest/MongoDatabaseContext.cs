using GenericProvisioningLib;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDbTest;

internal class MongoDatabaseContext(IMongoDatabase database, IUserInformation user)
{
    internal IUserInformation User { get; private set; } = user;

    internal IMongoDatabase Database => database;

    internal IMongoCollection<T> GetCollection<T>(string entityName = null) where T: class
    {
        return database.GetCollection<T>(entityName ?? MongoDbContext.GetCollectionName<T>());
    }

    internal IMongoCollection<BsonDocument> GetCollection(string name)
    {
        return database.GetCollection<BsonDocument>(name);
    }

    internal IMongoQueryable<T> AccessibleObjects<T>(bool applyPermissions, string collectionName = null) where T: class
    {
        IMongoQueryable<T> set = GetCollection<T>(collectionName).AsQueryable();
        return set;
    }
}