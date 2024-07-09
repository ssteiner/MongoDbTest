using GeneralTools.DataValidation;
using System.ComponentModel.DataAnnotations;

namespace NoSqlModels;

public class DeltaBaseObject<T> where T: class
{
    public T Data { get; set; }

    public List<string> IncludedProperties { get; set; }

    public List<string> IncludedPropertiesIncludingPath { get; set; }
}

public class MassUpdateParameters
{
    /// <summary>
    /// the updated values
    /// </summary>
    [Text]
    [Required]
    public Dictionary<string, object> Values { get; set; }

    /// <summary>
    /// the updated ids
    /// </summary>
    [Required]
    public List<string> Ids { get; set; }
}

public class ExtendedMassUpdateParameters<T> : MassUpdateParameters
{
    public T TemplateObject { get; set; }

    public List<string> IncludedProperties { get; set; }
}
