using MongoDB.Entities;
using NoSqlModels;

namespace MongoDbEntitiesTest.DbModels;

[Collection("phoneBookContacts")]
internal class PhoneBookContact : BaseEntity
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Location { get; set; }

    [DependencyField]
    public List<PhoneBookContactNumber> Numbers { get; set; }

    [DependencyField]
    public List<string> SecretaryIds { get; set; }

    public string ManagerId { get; set; }

    [DependencyField]
    public PhoneBookContact Manager { get; set; }

    [DependencyField]
    public List<PhoneBookCategory> Categories { get; set; }

    public List<string> CategoryIds { get; set; } = [];

    [DependencyField]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; } = [];

    public int NumberOfTelephoneNumbers => Numbers?.Count ?? 0;
}
