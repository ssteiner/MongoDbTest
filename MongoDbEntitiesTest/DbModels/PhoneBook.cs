using MongoDB.Entities;

namespace MongoDbEntitiesTest.DbModels;

[Collection("phoneBooks")]
internal class PhoneBook : BaseNamedEntity
{
    public string Description { get; set; }
}