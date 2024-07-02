namespace NoSqlModels;

public interface IIdItem
{
    Guid Id { get; set; }
}

public interface INamedItem
{
    string Name { get; set; }
}


public class PhoneBook: IIdItem
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public string Description { get; set; }
}

public class PhoneBookCategory : IIdItem
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    [DependencyField]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<Guid> PhoneBookIds { get; set;}
}

//public class PhoneBookContactToCategoryAssociation
//{
//    public Guid CategoryId { get; set; }

//    public Guid ContactId { get; set; }
//}

public class PhoneBookContact : IIdItem
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Location { get; set; }

    [DependencyField]
    public List<PhoneBookContactNumber> Numbers { get; set; }

    [DependencyField]
    public List<Guid> SecretaryIds { get; set; }

    public Guid? ManagerId { get; set; }

    [DependencyField]
    public List<PhoneBookCategory> Categories { get; set; } = [];

    public List<Guid> CategoryIds { get; set; } = [];

    [DependencyField]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<Guid> PhoneBookIds { get; set; } = [];
}

public class PhoneBookContactNumber : IIdItem
{
    public Guid Id { get; set; }

    public string Number { get; set; }

    public NumberType Type { get; set; }
}

public enum NumberType { Office, Mobile, Home }

public class PhoneBookCategorySearchParameters: GenericSearchParameters
{
    public List<Guid> PhoneBookIds { get; set; }
}

public class PhoneBookContactSearchParameters: GenericSearchParameters
{

    public string Location { get; set; }

    public List<Guid> CategoryIds { get; set; }

    public List<Guid> ManagerIds { get; set; }

    public List<Guid> SecretaryIds { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class DependencyFieldAttribute : Attribute
{

}