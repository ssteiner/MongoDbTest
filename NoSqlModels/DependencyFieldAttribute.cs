namespace NoSqlModels;

[AttributeUsage(AttributeTargets.Property)]
public class DependencyFieldAttribute(string idProperty) : Attribute
{
    public string IdProperty { get; set; } = idProperty;
}
