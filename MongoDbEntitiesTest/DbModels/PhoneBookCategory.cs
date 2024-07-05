using MongoDB.Entities;
using NoSqlModels;

namespace MongoDbEntitiesTest.DbModels;

[Collection("phoneBookCategorys")]
public class PhoneBookCategory : BaseNamedEntity
{
    [DependencyField]
    public Many<PhoneBook, PhoneBookCategory> PhoneBooks { get; set; }
    //public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; }

    public PhoneBookCategory()
    {
        this.InitOneToMany(() => PhoneBooks);
    }
}