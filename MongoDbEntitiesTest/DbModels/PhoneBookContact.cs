using MongoDB.Entities;
using NoSqlModels;

namespace MongoDbEntitiesTest.DbModels;

[Collection("phoneBookContacts")]
public class PhoneBookContact : BaseEntity
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Location { get; set; }

    [DependencyField]
    public List<PhoneBookContactNumber> Numbers { get; set; }

    [DependencyField]

    public Many<PhoneBookContact, PhoneBookContact> Secretary { get; set; }

    public List<string> SecretaryIds { get; set; }

    public string ManagerId { get; set; }

    [DependencyField]
    public One<PhoneBookContact> Manager { get; set; }

    [DependencyField]
    public Many<PhoneBookCategory, PhoneBookContact> Categories { get; set; }

    public List<string> CategoryIds { get; set; } = [];

    [DependencyField]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; } = [];

    public int NumberOfTelephoneNumbers => Numbers?.Count ?? 0;

    public PhoneBookContact()
    {
        this.InitOneToMany(() => Categories);
        this.InitOneToMany(() => Secretary);
    }
}
