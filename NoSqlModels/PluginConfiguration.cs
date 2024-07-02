namespace NoSqlModels;

public class PluginConfiguration
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public SpecialConfiguration SpecialConfig { get; set; }
}

public class BasePluginConfiguration
{
    public Guid Id { get; set; }

    public string Name { get; set; }
}

public class SpecialConfiguration
{
    public string Value1 { get; set; }

    public string Value2 { get; set; }
}
