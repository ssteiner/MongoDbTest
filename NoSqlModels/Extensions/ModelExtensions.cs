using System.Reflection;

namespace NoSqlModels.Extensions;

public static class ModelExtensions
{
    public static List<Tuple<string, string>> GetDependencyProperties(this Type t)
    {
        return t.GetProperties()
            .Where(prop => prop.IsDefined(typeof(DependencyFieldAttribute), true))
            .OrderBy(n => n.Name)
            .Select(x => new Tuple<string, string>(x.Name, x.GetCustomAttribute<DependencyFieldAttribute>(true).IdProperty))
            .ToList();
    }

    public static List<string> GetIgnorePropertiesForObjectUpdate(this Type t, bool updateInternalFields = false, bool updateDependencyFields = false)
    {
        IEnumerable<PropertyInfo> properties = t.GetProperties();
        if (updateInternalFields)
        {
            if (updateDependencyFields)
                properties = properties.Where(prop => false);
            else
                properties = properties.Where(prop => prop.IsDefined(typeof(DependencyFieldAttribute), false));
        }
        else
        {
            if (updateDependencyFields)
                properties = properties.Where(prop => false);
            else
                properties = properties.Where(prop => prop.IsDefined(typeof(DependencyFieldAttribute), false));
        }
        var props = properties.Select(pi => pi.Name).ToList();
        return [.. props.OrderBy(n => n)];
    }
}
