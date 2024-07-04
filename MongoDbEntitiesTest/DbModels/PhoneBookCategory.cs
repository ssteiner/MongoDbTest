using MongoDB.Entities;
using NoSqlModels;

namespace MongoDbEntitiesTest.DbModels;

[Collection("phoneBookCategorys")]
internal class PhoneBookCategory : BaseNamedEntity
{
    [DependencyField]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; }
}