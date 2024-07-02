using GeneralTools.DataValidation;

namespace NoSqlModels;

public class GenericSearchParameters : BaseSearchParameters
{
    /// <summary>
    /// generic query that searches over all fields where it makes sense
    /// </summary>
    public string Query { get; set; }

    public BooleanOperator? Operator { get; set; }

    public List<GenericSearchParameter> SearchParameters { get; set; }

    public bool? SortAscending { get; set; }

    [BasicText]
    public string SortBy { get; set; }

    [Text/*, GuiField*/]
    public string Name { get; set; }
}

public class BaseSearchParameters
{
    public int PageSize { get; set; }

    public int Page { get; set; } = 1;

    public bool IncludeDependentElements { get; set; }
}

public enum ComparisonOperator
{
    EqualTo,
    NotEqualTo,
    MoreThan,
    LessThan,
    MoreThanOrEqualTo,
    LessThanOrEqualTo,
    StartsWith,
    EndsWith,
    Contains,
    IsEmpty,
    IsNotEmpty
}

public enum BooleanOperator
{
    And,
    Or
}

public class GenericSearchParameter
{
    [BasicText]
    public string FieldName { get; set; }

    //[SearchText]
    public dynamic FieldValue { get; set; }

    public string FieldValueString => FieldValue?.ToString();

    public List<string> FieldValueCollection
    {
        get
        {
            if (FieldValue is Newtonsoft.Json.Linq.JArray array)
            {
                var list = new List<string>();
                var jArray = array;
                foreach (var child in jArray.Children())
                {
                    list.Add(child.ToString());
                }
                return list;
            }
            return null;
        }
    }

    public ComparisonOperator? FieldOperator { get; set; }
}