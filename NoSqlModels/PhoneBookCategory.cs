namespace NoSqlModels;

public class PhoneBookCategory : IIdItem, INamedItem
{
    public string Id { get; set; }

    public string Name { get; set; }

    [DependencyField]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; }
}

public class PhoneBookCategorySearchParameters : GenericSearchParameters
{
    public List<string> PhoneBookIds { get; set; }
}