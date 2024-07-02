using GeneralTools.Extensions;

namespace NoSqlModels.Extensions;

public static class DeltaExtensions
{
    public static void ApplyDelta<T>(T oldObject, DeltaBaseObject<T> update) where T : class
    {
        if (update.IncludedProperties != null) // it's a real delta update
        {
            var updatedObject = oldObject;
            if (update.IncludedPropertiesIncludingPath?.Any(x => x.Contains('.')) == true) // subobject updates
            {
                var treatedPropertyNames = new List<string>();
                foreach (var pathName in update.IncludedPropertiesIncludingPath)
                {
                    var propertyName = pathName.ValueBefore(".");
                    if (treatedPropertyNames.Contains(propertyName))
                        continue;
                    var type = oldObject.GetPropertyType(propertyName, true);
                    if (type != null && type.ImplementsIDictionary())
                    { // if it's a dict or list, it'll contain the full value
                        var newValue = update.Data.ObjectValue(propertyName, true);
                        updatedObject.SetObjectValue(propertyName, newValue, true);
                        treatedPropertyNames.Add(propertyName);
                    }
                    else
                    {
                        var newValue = update.Data.ObjectValue(pathName, true);
                        updatedObject.SetObjectValue(pathName, newValue, true);
                    }
                }
            }
            else
            {
                foreach (var propertyName in update.IncludedProperties)
                {
                    var newValue = update.Data.ObjectValue(propertyName, true);
                    updatedObject.SetObjectValue(propertyName, newValue, true);
                }
            }
        }
    }
}
