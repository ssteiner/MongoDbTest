﻿using DynamicQuery;
using GeneralTools;
using GeneralTools.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NoSqlModels;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MongoDbTest;

internal static class FilterHelpers
{
    private const char spaceSplitter = ' ';

    internal static IQueryable<T> FilterByNameGeneric<T>(IQueryable<T> items, GenericSearchParameters parameters)
        where T : INamedItem
    {
        if (!string.IsNullOrEmpty(parameters.Name))
        {
            if (parameters.Name.StartsWith('%'))
            {
                var query = parameters.Name.TrimStart('%').ToLower();
                items = items.Where(x => x.Name.ToLower().Contains(query));
            }
            else
            {
                var query = parameters.Name.ToLower();
                items = items.Where(x => x.Name.ToLower().StartsWith(query));
            }
        }
        if (!string.IsNullOrEmpty(parameters.Query))
        {
            if (parameters.Query.StartsWith('%'))
            {
                var query = parameters.Query.TrimStart('%').ToLower();
                items = items.Where(u => u.Name.ToLower().Contains(query));
            }
            else
            {
                var query = parameters.Query.ToLower();
                items = items.Where(u => u.Name.ToLower().StartsWith(query));
            }
        }
        return items;
    }

    internal static IMongoQueryable<T> FilterByNameGeneric<T>(IMongoQueryable<T> items, GenericSearchParameters parameters)
    {
        var builder = Builders<T>.Filter;
        if (!string.IsNullOrEmpty(parameters.Name))
        {
            if (parameters.Name.StartsWith('%'))
            {
                var query = parameters.Name.TrimStart('%').ToLower();
                var filter = builder.Regex(nameof(INamedItem.Name), new Regex($"{query}", RegexOptions.IgnoreCase));
                items = items.Where(x => filter.Inject());
                //items = items.Where(PredicateBuilder.ContainsPredicate<T>(nameof(INamedItem.Name), query));
                //items = items.Where(x => x.Name.ToLower().Contains(query));
            }
            else
            {
                var filter = builder.Regex(nameof(INamedItem.Name), new Regex($"^{parameters.Name}", RegexOptions.IgnoreCase));
                items = items.Where(x => filter.Inject());
                //var query = parameters.Name.ToLower();
                //items = items.Where(PredicateBuilder.StartsWithPredicate<T>(nameof(INamedItem.Name), query)); // works but is case sensitive
            }
        }
        if (!string.IsNullOrEmpty(parameters.Query))
        {
            if (parameters.Query.StartsWith('%'))
            {
                var query = parameters.Query.TrimStart('%').ToLower();
                var filter = builder.Regex(nameof(INamedItem.Name), new Regex($"{query}", RegexOptions.IgnoreCase));
                items = items.Where(x => filter.Inject());
                //items = items.Where(PredicateBuilder.ContainsPredicate<T>(nameof(INamedItem.Name), query)); // works but is case sensitive
            }
            else
            {
                var filter = builder.Regex(nameof(INamedItem.Name), new Regex($"^{parameters.Query}", RegexOptions.IgnoreCase));
                items = items.Where(x => filter.Inject());
                //var query = parameters.Query.ToLower();
                //items = items.Where(PredicateBuilder.StartsWithPredicate<T>(nameof(INamedItem.Name), query)); // works but is case sensitive
            }
        }
        return items;
    }

    internal static IMongoQueryable<PhoneBookContact> FilterPhoneBookContacts(IMongoQueryable<PhoneBookContact> items, string query,
        bool includeNumber = true, bool includeUserId = true)
    {
        string firstToken, secondToken = null;
        if (string.IsNullOrEmpty(query))
            return items;
        query = query.Trim();
        if (query == "*") // return all
            return items;
        if (query.Contains(' '))
        {
            var split = query.Split([spaceSplitter], 2);
            firstToken = split[0].ToLower();
            secondToken = split[1].ToLower();
        }
        else
            firstToken = query.ToLower();
        var useContainsSearch = query.StartsWith('%');
        if (useContainsSearch)
            firstToken = firstToken.TrimStart('%');
        if (secondToken == null)
        {
            if (useContainsSearch)
                return items.Where(u => (u.FirstName != null && u.FirstName.ToLower().Contains(firstToken))
                                        || (u.LastName != null && u.LastName.ToLower().Contains(firstToken))
                                        || (includeUserId && u.UserId != null && u.UserId.ToLower().Contains(firstToken))
                                        || (includeNumber && u.Numbers.Select(x => x.Number).Any(x => x.Contains(firstToken))));
            return items.Where(u => (u.FirstName != null && u.FirstName.ToLower().StartsWith(firstToken))
                                    || (u.LastName != null && u.LastName.ToLower().StartsWith(firstToken))
                                    || (includeUserId && u.UserId != null && u.UserId.ToLower().StartsWith(firstToken))
                                    || (includeNumber && u.Numbers.Select(x => x.Number).Any(x => x.StartsWith(firstToken))));
        }

        if (useContainsSearch)
            return items.Where(u => (u.FirstName != null && u.FirstName.ToLower().Contains(firstToken) && u.LastName != null && u.LastName.ToLower().Contains(secondToken))
                                    || (u.FirstName != null && u.FirstName.ToLower().Contains(secondToken) && u.LastName != null && u.LastName.ToLower().Contains(firstToken))
                                    || (u.FirstName != null && u.FirstName.ToLower().Contains(query) || u.LastName != null && u.LastName.ToLower().Contains(query))
                                    || (includeNumber && u.Numbers.Select(x => x.Number).Any(x => x.Contains(query))));
        return items.Where(u => (u.FirstName != null && u.FirstName.ToLower().StartsWith(firstToken) && u.LastName != null && u.LastName.ToLower().StartsWith(secondToken))
                                || (u.FirstName != null && u.FirstName.ToLower().StartsWith(secondToken) && u.LastName != null && u.LastName.ToLower().StartsWith(firstToken))
                                || (u.FirstName != null && u.FirstName.ToLower().StartsWith(query)) || (u.LastName != null && u.LastName.ToLower().StartsWith(query))
                                || (includeNumber && u.Numbers.Select(x => x.Number).Any(x => x.StartsWith(query))));
    }

    internal static IMongoQueryable<T> FilterStringProperties<T, K>(IMongoQueryable<T> items, K parameters) where K: GenericSearchParameters
    {
        var stringProperties = typeof(K)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(x => x.PropertyType == typeof(string))
            .ToList();
        var builder = Builders<T>.Filter;
        foreach (var prop in stringProperties)
        {
            if (prop.Name == nameof(PhoneBookContactSearchParameters.Number) && parameters is PhoneBookContactSearchParameters contactSearchParameters
                && items is IMongoQueryable<PhoneBookContact> contacts)
            {
                if (!string.IsNullOrEmpty(contactSearchParameters.Number))
                {
                    if (contactSearchParameters.Number.StartsWith('%'))
                    {
                        var query = contactSearchParameters.Number.TrimStart('%');
                        var filter = builder.Regex(prop.Name, new Regex($"{query}", RegexOptions.IgnoreCase));
                        items = items.Where(x => filter.Inject());
                        //items = contacts.Where(u => u.Numbers.Select(x => x.Number).Any(x => x.Contains(query))) as IMongoQueryable<T>;
                    }
                    else
                    {
                        var filter = builder.Regex(prop.Name, new Regex($"^{contactSearchParameters.Number}", RegexOptions.IgnoreCase));
                        items = items.Where(x => filter.Inject());
                        //var lowerCaseQuery = contactSearchParameters.Number.ToLower();
                        //items = contacts.Where(u => u.Numbers.Select(x => x.Number).Any(x => x.Contains(lowerCaseQuery))) as IMongoQueryable<T>;
                    }
                }
            }
            else
            {
                if (prop.GetValue(parameters) is string strValue && !string.IsNullOrEmpty(strValue))
                {
                    if (strValue.StartsWith('%')) // contains
                    {
                        var query = strValue.TrimStart('%');//.ToLower();
                        var filter = builder.Regex(prop.Name, new Regex($"{query}", RegexOptions.IgnoreCase));
                        items = items.Where(x => filter.Inject());
                    }
                    else
                    {
                        var filter = builder.Regex(prop.Name, new Regex($"^{strValue}", RegexOptions.IgnoreCase));
                        items = items.Where(x => filter.Inject());
                    }
                }
            }
        }
        return items;
    }

    internal static IMongoQueryable<TEntity> AddSortInclusions<TEntity>(IMongoQueryable<TEntity> set, GenericSearchParameters parameters)
        where TEntity : class
    {
        if (!string.IsNullOrEmpty(parameters.SortBy))
        {
            if (parameters.SortBy.ToLower().EndsWith("id")) // could be a navigation property
            {
                var navigationPropertyName = parameters.SortBy.Substring(0, parameters.SortBy.Length - 2);
                var navigationProperty = CompareUtils.GetProperty<TEntity>(navigationPropertyName, true);
                if (navigationProperty != null)
                {
                    if (typeof(INamedItem).IsAssignableFrom(navigationProperty.PropertyType))
                    {
                        var fullPropertyName = $"{navigationProperty.Name}.{nameof(INamedItem.Name)}";
                        //set = set.Include(navigationProperty.Name); // ensure that query includes the subtable
                        var keyProperty = CompareUtils.GetProperty<TEntity>(parameters.SortBy, true);
                        if (keyProperty != null)
                        {
                            if (keyProperty.PropertyType.IsNullableType())
                                fullPropertyName = $"np({fullPropertyName})";
                        }
                        parameters.SortBy = fullPropertyName;
                    }
                }
            }
        }
        return set;
    }

    internal static IMongoQueryable<T> SortBy<T>(this IMongoQueryable<T> items, string sortBy, bool? sortAscending)
    {
        ParameterExpression pe = Expression.Parameter(typeof(T), "t");
        MemberExpression me = Expression.Property(pe, sortBy);
        Expression conversion = Expression.Convert(me, typeof(object));
        Expression<Func<T, object>> orderExpression = Expression.Lambda<Func<T, object>>(conversion, [pe]);

        if (sortAscending == false)
            items = items.OrderByDescending(orderExpression);
        else
            items = items.OrderBy(orderExpression);
        return items;
    }

    internal static IMongoQueryable<T> ThenSortBy<T>(this IMongoQueryable<T> items, string sortBy, bool? sortAscending)
    {
        if (items is IOrderedMongoQueryable<T> ordered)
        {
            ParameterExpression pe = Expression.Parameter(typeof(T), "t");
            MemberExpression me = Expression.Property(pe, sortBy);
            Expression conversion = Expression.Convert(me, typeof(object));
            Expression<Func<T, object>> orderExpression = Expression.Lambda<Func<T, object>>(conversion, [pe]);
            if (sortAscending == false)
                items = ordered.ThenByDescending(orderExpression);
            else
                items = ordered.ThenBy(orderExpression);
        }
        return items;
    }

    internal static IQueryable<T> AppendGenericFilter<T>(this IQueryable<T> items, GenericSearchParameters parameters,
        ref bool isSorted) where T : class
    {
        if ((parameters.SearchParameters == null || parameters.SearchParameters.Count <= 0) &&
            string.IsNullOrEmpty(parameters.SortBy)) return items;
        Filter filter = null;
        var builder = Builders<T>.Filter;
        if (parameters.SearchParameters != null && parameters.SearchParameters.Count > 0)
        {
            filter = new Filter
            {
                Criteria = [],
                BooleanCompare = GetOperator(parameters)
            };
            foreach (var p in parameters.SearchParameters.Where(x => !string.IsNullOrEmpty(x.FieldName)))
            {
                var op = GetComparisonOperator(p);
                if (p.FieldValueCollection != null) // it's an array
                {
                    //var values = p.FieldValueCollection.Select(x => new StringOrRegularExpression(x));
                    var anyFilter = builder.In(p.FieldName, p.FieldValueCollection);
                    items = items.Where(x => anyFilter.Inject());
                }
                else
                {
                    if ((p.FieldValue != null && !string.IsNullOrEmpty(p.FieldValueString)) || QueryHelper.IsEmptyValueOperator(op))
                        QueryHelper.AddSearchFilter<T>(p.FieldName, p.FieldValueString, op, filter);
                }
            }
        }
        Sorter sorter = null;
        if (!string.IsNullOrEmpty(parameters.SortBy))
        {
            sorter = new Sorter { SortCriteria = [] }; // we could sort like that 
            sorter.SortCriteria.Add(new SortCriteria
            {
                FieldName = parameters.SortBy,
                SortOrder = GetSortDirection(parameters)
            });
        }
        try
        {
            if (filter != null)
                items = items.Where(filter.GetDynamicLinqString(), filter.GetDynamicLinqParameters());
            if (sorter != null)
            {
                items = items.OrderBy(sorter.ToString());
                isSorted = true;
            }
        }
        catch (ParseException p)
        {
            //Log($"Unable to perform dynamic linq query: {p.Message}", 4, userInfo);
            throw;
        }
        return items;
    }

    private static DynamicQuery.ComparisonOperator GetComparisonOperator(GenericSearchParameter p)
    {
        if (p.FieldOperator.HasValue)
            return (DynamicQuery.ComparisonOperator)p.FieldOperator;
        return DynamicQuery.ComparisonOperator.EqualTo;
    }

    private static DynamicQuery.SortDirection GetSortDirection(GenericSearchParameters parameters)
    {
        var sortDirection = DynamicQuery.SortDirection.Ascending;
        if (parameters.SortAscending.HasValue)
            sortDirection = parameters.SortAscending.Value ? DynamicQuery.SortDirection.Ascending : DynamicQuery.SortDirection.Descending;
        return sortDirection;
    }

    private static DynamicQuery.BooleanOperator GetOperator(GenericSearchParameters parameters)
    {
        if (parameters.Operator.HasValue)
            return (DynamicQuery.BooleanOperator)parameters.Operator;
        return DynamicQuery.BooleanOperator.And;
    }

    internal static IMongoQueryable<T> AppendGenericFilter<T>(this IMongoQueryable<T> items, GenericSearchParameters parameters, ref bool isSorted)
    {
        if (!string.IsNullOrEmpty(parameters.SortBy))
        {
            var sortExpression = parameters.SortAscending == false ? Builders<T>.Sort.Descending(parameters.SortBy) : Builders<T>.Sort.Ascending(parameters.SortBy);

            ParameterExpression pe = Expression.Parameter(typeof(T), "t");
            MemberExpression me = Expression.Property(pe, parameters.SortBy);
            Expression conversion = Expression.Convert(me, typeof(object));
            Expression<Func<T, object>> orderExpression = Expression.Lambda<Func<T, object>>(conversion, [pe]);

            if (parameters.SortAscending == false)
                items = items.OrderByDescending(orderExpression);
            else
                items = items.OrderBy(orderExpression);
            isSorted = true;
        }
        if (parameters.SearchParameters == null || parameters.SearchParameters.Count < 1)
            return items;

        var builder = Builders<T>.Filter;
        List<FilterDefinition<T>> filters = [];

        List<Expression<Func<T, bool>>> expressions = [];
        foreach (var p in parameters.SearchParameters.Where(x => !string.IsNullOrEmpty(x.FieldName)))
        {
            if (p.FieldValueCollection != null) // it's an array
            {
                //var values = p.FieldValueCollection.Select(x => new StringOrRegularExpression(x));
                filters.Add(builder.In(p.FieldName, p.FieldValueCollection));
            }
            else
            {
                //Builders<T>.Sort.Ascending(x => x.na)
                if ((p.FieldValue != null && !string.IsNullOrEmpty(p.FieldValueString)) || IsEmptyValueOperator(p))
                {
                    if (!IsAllowed<T>(p.FieldOperator, p.FieldName))
                        continue;
                    //if (p.FieldName.ToLower().EndsWith("id")) // assuming it's a referenced table, so we search on the linked object instead //LiteDb only
                    //{
                    //    p.FieldName = $"{p.FieldName[..^2]}.$id";
                    //    if (p.FieldValue != null)
                    //    {
                    //        Guid myGuid = Guid.Empty;
                    //        if (Guid.TryParse(p.FieldValue, out myGuid))
                    //            p.FieldValue = myGuid;
                    //    }
                    //}
                    switch (p.FieldOperator)
                    {
                        case NoSqlModels.ComparisonOperator.EqualTo:
                            filters.Add(builder.Eq(p.FieldName, p.FieldValue));
                            break;
                        case NoSqlModels.ComparisonOperator.NotEqualTo:
                            filters.Add(builder.Ne(p.FieldName, p.FieldValue));
                            break;
                        case NoSqlModels.ComparisonOperator.Contains:
                            filters.Add(builder.Regex(p.FieldName, new Regex($"{p.FieldValue}", RegexOptions.IgnoreCase)));
                            //expressions.Add(PredicateBuilder.ContainsPredicate<T>(p.FieldName, p.FieldValueString));
                            break;
                        case NoSqlModels.ComparisonOperator.StartsWith:
                            filters.Add(builder.Regex(p.FieldName, new Regex($"^{p.FieldValue}", RegexOptions.IgnoreCase)));
                            //expressions.Add(PredicateBuilder.StartsWithPredicate<T>(p.FieldName, p.FieldValueString));
                            break;
                        case NoSqlModels.ComparisonOperator.EndsWith:
                            filters.Add(builder.Regex(p.FieldName, new Regex($"{p.FieldValue}$", RegexOptions.IgnoreCase)));
                            //expressions.Add(PredicateBuilder.EndsWithWithPredicate<T>(p.FieldName, p.FieldValueString));
                            break;
                        case NoSqlModels.ComparisonOperator.LessThanOrEqualTo:
                            filters.Add(builder.Lte(p.FieldName, p.FieldValueString));
                            break;
                        case NoSqlModels.ComparisonOperator.LessThan:
                            filters.Add(builder.Lt(p.FieldName, p.FieldValueString));
                            break;
                        case NoSqlModels.ComparisonOperator.MoreThan:
                            filters.Add(builder.Gt(p.FieldName, p.FieldValueString));
                            break;
                        case NoSqlModels.ComparisonOperator.MoreThanOrEqualTo:
                            filters.Add(builder.Gte(p.FieldName, p.FieldValueString));
                            break;
                        case NoSqlModels.ComparisonOperator.IsEmpty:
                            var propType2 = GetPropertyType<T>(p.FieldName);
                            //filters.Add(builder.Eq(p.FieldName, MongoDB.Bson.BsonNull.Value));
                            filters.Add(builder.Eq(p.FieldName, propType2.IsValueType ? Activator.CreateInstance(propType2) : null));
                            break;
                        case NoSqlModels.ComparisonOperator.IsNotEmpty:
                            //builder.Exists(p.FieldName, true);
                            //filters.Add(builder.Ne(p.FieldName, MongoDB.Bson.BsonNull.Value));
                            var propType = GetPropertyType<T>(p.FieldName);
                            filters.Add(builder.Ne(p.FieldName, propType.IsValueType ? Activator.CreateInstance(propType) : null));
                            break;
                    }
                }
            }
        }

        if (filters.Count > 1)
        {
            var finalFilter = parameters.Operator == NoSqlModels.BooleanOperator.And ? builder.And(filters) : builder.Or(filters);
            items.Where(x => finalFilter.Inject());
        }
        else if (filters.Count > 0)
            items = items.Where(x => filters[0].Inject());
        return items;

        //if (expressions.Count > 1)
        //{
        //    var fullExpression = expressions.First();
        //    foreach (var otherExpression in expressions.Skip(1))
        //    {
        //        fullExpression = PredicateBuilder.Or(fullExpression, otherExpression);
        //    }
        //    return items.Where(fullExpression);
        //}
        //else if (expressions.Count == 1)
        //{
        //    return items.Where(expressions.First());
        //}
        //return items;
    }

    private static Dictionary<string, Type> typeCache = [];

    private static Type GetPropertyType<T>(string propertyName)
    {
        if (typeCache.TryGetValue($"{typeof(T).Name}_{propertyName}", out var myType))
            return myType;
        myType = typeof(T).GetProperty(propertyName).PropertyType;
        typeCache.TryAdd($"{typeof(T).Name}_{propertyName}", myType);
        return myType;
    }

    private static bool IsAllowed<T>(NoSqlModels.ComparisonOperator? op, string fieldName)
    {
        var memberType = ClassHelpers.GetPropertyType(typeof(T), fieldName);
        if (memberType == typeof(DateTime))
        {
            if (op == NoSqlModels.ComparisonOperator.Contains || op == NoSqlModels.ComparisonOperator.StartsWith || op == NoSqlModels.ComparisonOperator.EndsWith)
                return false; // these operators don't work for dates
        }
        TypeCode tc = Type.GetTypeCode(memberType);
        switch (tc)
        {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.Char:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                //numeric values that do not support contains, startswith, endswith
                if (op == NoSqlModels.ComparisonOperator.Contains || op == NoSqlModels.ComparisonOperator.StartsWith || op == NoSqlModels.ComparisonOperator.EndsWith)
                    return false;
                break;
        }
        return true;
    }

    private static bool IsEmptyValueOperator(GenericSearchParameter p)
    {
        return p.FieldOperator switch
        {
            NoSqlModels.ComparisonOperator.EqualTo or NoSqlModels.ComparisonOperator.IsEmpty or NoSqlModels.ComparisonOperator.IsNotEmpty or NoSqlModels.ComparisonOperator.NotEqualTo => true,
            _ => false,
        };
    }

}