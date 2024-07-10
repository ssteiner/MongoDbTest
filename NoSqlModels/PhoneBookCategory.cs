namespace NoSqlModels;

public class PhoneBookCategory : IIdItem, INamedItem, IDescriptionItem
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    [DependencyField(nameof(PhoneBookIds))]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; }
}

public class PhoneBookCategorySearchParameters : GenericSearchParameters
{
    public List<string> PhoneBookIds { get; set; }
}