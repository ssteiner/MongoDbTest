namespace NoSqlModels;

public interface IIdItem
{
    string Id { get; set; }
}

public interface INamedItem
{
    string Name { get; set; }
}

public class PhoneBook: IIdItem, INamedItem
{
    public string Id { get; set; }
    public string Name { get; set; }

    public string Description { get; set; }
}

//public class PhoneBookContactToCategoryAssociation
//{
//    public Guid CategoryId { get; set; }

//    public Guid ContactId { get; set; }
//}



[AttributeUsage(AttributeTargets.Property)]
public class DependencyFieldAttribute : Attribute
{

}