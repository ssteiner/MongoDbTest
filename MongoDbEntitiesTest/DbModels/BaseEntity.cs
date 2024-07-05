using MongoDB.Bson;
using MongoDB.Entities;
using NoSqlModels;

namespace MongoDbEntitiesTest.DbModels;

public class BaseEntity: IEntity, IIdItem, ICreatedOn, IModifiedOn
{
    public string Id { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
        => string.IsNullOrEmpty(Id);
}

public class BaseNamedEntity: BaseEntity, INamedItem
{
    public string Name { get; set; }
}