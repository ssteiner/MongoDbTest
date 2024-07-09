namespace NoSqlModels;

public class PhoneBook: IIdItem, INamedItem, IDescriptionItem
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }
}