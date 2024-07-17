using MongoDB.Entities;
using NoSqlModels;

namespace MongoDbEntitiesTest.DbModels;

[Collection("phoneBookContacts")]
public class PhoneBookContact : BaseEntity
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Location { get; set; }

    [DependencyField(null)]
    public List<PhoneBookContactNumber> Numbers { get; set; }

    [DependencyField(nameof(SecretaryIds))]

    public Many<PhoneBookContact, PhoneBookContact> Secretary { get; set; }

    public List<string> SecretaryIds { get; set; }

    public string ManagerId { get; set; }

    [DependencyField(nameof(ManagerId))]
    public One<PhoneBookContact> Manager { get; set; }

    [DependencyField(nameof(CategoryIds))]
    public Many<PhoneBookCategory, PhoneBookContact> Categories { get; set; }

    public List<string> CategoryIds { get; set; } = [];

    [DependencyField(nameof(PhoneBookIds))]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; } = [];

    public int NumberOfTelephoneNumbers => Numbers?.Count ?? 0;

    public PhoneBookContact()
    {
        this.InitOneToMany(() => Categories);
        this.InitOneToMany(() => Secretary);
    }
}
