namespace NoSqlModels;

public class SearchResults<T>
{
    public bool More { get; set; }
    public int PageSize { get; set; }
    public int ItemCount { get; set; }

    public List<T> Results { get; set; }

    public SearchResults()
    {
        More = false;
    }
}
