using MongoDB.Entities;

namespace MongoDbEntitiesTest.DbModels;

[Collection("phoneBooks")]
public class PhoneBook : BaseNamedEntity
{
    public string Description { get; set; }
}