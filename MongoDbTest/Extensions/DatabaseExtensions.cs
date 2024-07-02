using GeneralTools.Extensions;
using MongoDB.Driver;
using NoSqlModels;

namespace MongoDbTest.Extensions;

internal static class DatabaseExtensions
{
    internal static UpdateDefinition<T> ApplyMultiFields<T>(this UpdateDefinitionBuilder<T> builder, T obj)
    {
        var properties = obj.GetType().GetProperties();
        UpdateDefinition<T> definition = null;
        foreach (var property in properties)
        {
            if (definition == null)
            {
                definition = builder.Set(property.Name, property.GetValue(obj));
            }
            else
            {
                definition = definition.Set(property.Name, property.GetValue(obj));
            }
        }
        return definition;
    }

    internal static UpdateDefinition<T> GenerateUpdate<T>(this Dictionary<string, string> diff, T updatedObject) where T: class
    {
        if (diff.Count > 0)
        {
            UpdateDefinitionBuilder<T> builder = Builders<T>.Update;
            UpdateDefinition<T> definition = null;
            foreach (var difference in diff)
            {
                definition = SetProperty(builder, definition, difference.Key, updatedObject.ObjectValue(difference.Key));
            }
            return definition;
        }
        return null;
    }

    internal static UpdateDefinition<T> GenerateUpdate<T>(this T oldObject, DeltaBaseObject<T> update) where T: class
    {
        if (update.IncludedProperties != null) // it's a real delta update
        {
            UpdateDefinitionBuilder<T> builder = Builders<T>.Update;
            UpdateDefinition<T> definition = null;

            var updatedObject = oldObject;
            if (update.IncludedPropertiesIncludingPath?.Any(x => x.Contains('.')) == true) // subobject updates
            {
                var treatedPropertyNames = new List<string>();
                foreach (var pathName in update.IncludedPropertiesIncludingPath)
                {
                    var propertyName = pathName.ValueBefore(".");
                    if (treatedPropertyNames.Contains(propertyName))
                        continue;
                    //var realPropertyName = update.Data.RealPropertyName(propertyName, false);
                    var type = oldObject.GetPropertyType(propertyName, true);
                    if (type != null && type.ImplementsIDictionary())
                    { // if it's a dict or list, it'll contain the full value
                        var newValue = update.Data.ObjectValue(propertyName, true);
                        definition = SetProperty(builder, definition, propertyName, newValue);
                        //updatedObject.SetObjectValue(propertyName, newValue, true);
                        treatedPropertyNames.Add(propertyName);
                    }
                    else
                    {
                        var newValue = update.Data.ObjectValue(pathName, true);
                        definition = SetProperty(builder, definition, propertyName, newValue);
                        //updatedObject.SetObjectValue(pathName, newValue, true);
                    }
                }
            }
            else
            {
                foreach (var propertyName in update.IncludedProperties)
                {
                    var newValue = update.Data.ObjectValue(propertyName, true);
                    //var realPropertyName = update.Data.RealPropertyName(propertyName, false);
                    definition = SetProperty(builder, definition, propertyName, newValue); // case sensitivity
                }
            }
            return definition;
        }
        return null;
    }

    private static UpdateDefinition<T> SetProperty<T>(UpdateDefinitionBuilder<T> builder, UpdateDefinition<T> definition, string propertyName, object newValue)
    {
        if (definition == null)
            return builder.Set(propertyName, newValue);
        return definition.Set(propertyName, newValue);
    }




    public static List<string> GetDependencyProperties(this Type t)
    {
        return t.GetProperties().Where(prop => prop.IsDefined(typeof(DependencyFieldAttribute), true))
            .OrderBy(n => n.Name)
            .Select(x => x.Name)
            .ToList();
    }

}
