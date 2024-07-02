namespace NoSqlModels;

public class DeltaBaseObject<T> where T: class
{
    public T Data { get; set; }

    public List<string> IncludedProperties { get; set; }

    public List<string> IncludedPropertiesIncludingPath { get; set; }
}
