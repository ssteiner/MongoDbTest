using DynamicQuery;
using GeneralTools;
using GeneralTools.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NoSqlModels;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MongoDbTest;

internal static class FilterHelpers
{
    internal static IMongoQueryable<T> FilterByNameGeneric<T>(IMongoQueryable<T> items, GenericSearchParameters parameters)
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
            if (parameters.Query.StartsWith("%"))
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

    internal static IQueryable<T> AppendGenericFilter<T>(IQueryable<T> items, GenericSearchParameters parameters,
        ref bool isSorted) where T : class
    {
        if ((parameters.SearchParameters == null || parameters.SearchParameters.Count <= 0) &&
            string.IsNullOrEmpty(parameters.SortBy)) return items;
        Filter filter = null;
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
                    if (p.FieldValueCollection.Count == 1)
                    {
                        var myFilter = PredicateBuilder.ContainsPredicate<T>(p.FieldName, p.FieldValueCollection[0]);
                        //QueryHelper.AddSearchFilter<T>(mappingAttribute.DatabaseFieldName, p.FieldValueCollection[0], op, filter);
                        items = items.Where(myFilter);
                    }
                    else
                    {
                        var myFilter = PredicateBuilder.ContainsPredicate<T>(p.FieldName, p.FieldValueCollection[0]);
                        var fullFilter = myFilter;
                        foreach (var fieldValue in p.FieldValueCollection.Skip(1))
                        {
                            myFilter = PredicateBuilder.ContainsPredicate<T>(p.FieldName, fieldValue);
                            fullFilter = PredicateBuilder.Or(fullFilter, myFilter);
                            //items = items.Where(myFilter);
                            //QueryHelper.AddSearchFilter<T>(mappingAttribute.DatabaseFieldName, fieldValue, DynamicQuery.ComparisonOperator.Contains, filter);
                        }
                        items = items.Where(fullFilter);
                    }
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

    //internal static IMongoQueryable<T> AppendGenericFilter<T>(IMongoQueryable<T> items, GenericSearchParameters parameters, ref bool isSorted)
    //{
    //    if (!string.IsNullOrEmpty(parameters.SortBy))
    //    {
    //        ParameterExpression pe = Expression.Parameter(typeof(T), "t");
    //        MemberExpression me = Expression.Property(pe, parameters.SortBy);
    //        Expression conversion = Expression.Convert(me, typeof(object));
    //        Expression<Func<T, object>> orderExpression = Expression.Lambda<Func<T, object>>(conversion, [pe]);

    //        if (parameters.SortAscending == false)
    //            items = items.OrderByDescending(orderExpression);
    //        else
    //            items = items.OrderBy(orderExpression);
    //        isSorted = true;
    //    }
    //    if (parameters.SearchParameters == null || parameters.SearchParameters.Count < 1)
    //        return items;

    //    var builder = Builders<T>.Filter;
    //    List<FilterDefinition<T>> filters = [];

    //    List<Expression<Func<T, bool>>> expressions = [];
    //    //BsonExpression queryOperator = parameters.Operator == BooleanOperator.Or ? Query.Or() : Query.And;
    //    foreach (var p in parameters.SearchParameters.Where(x => !string.IsNullOrEmpty(x.FieldName)))
    //    {
    //        if (p.FieldValueCollection != null) // it's an array
    //        {
    //            var values = p.FieldValueCollection.Select(x => new StringOrRegularExpression(x));
    //            filters.Add(builder.StringIn(p.FieldName, values));
    //            if (p.FieldValueCollection.Count == 1)
    //            {
    //                var myFilter = PredicateBuilder.ContainsPredicate<T>(p.FieldName, p.FieldValueCollection[0]);
    //                expressions.Add(myFilter);
    //                //items = items.Where(myFilter);
    //            }
    //            else
    //            {
    //                var myFilter = PredicateBuilder.ContainsPredicate<T>(p.FieldName, p.FieldValueCollection[0]);
    //                var fullFilter = myFilter;
    //                foreach (var fieldValue in p.FieldValueCollection.Skip(1))
    //                {
    //                    myFilter = PredicateBuilder.ContainsPredicate<T>(p.FieldName, fieldValue);
    //                    fullFilter = PredicateBuilder.Or(fullFilter, myFilter);
    //                }
    //                expressions.Add(fullFilter);
    //                //items = items.Where(fullFilter);
    //            }
    //        }
    //        else
    //        {
    //            //Builders<T>.Sort.Ascending(x => x.na)
    //            if ((p.FieldValue != null && !string.IsNullOrEmpty(p.FieldValueString)) || IsEmptyValueOperator(p))
    //            {
    //                if (!IsAllowed<T>(p.FieldOperator, p.FieldName))
    //                    continue;
    //                if (p.FieldName.ToLower().EndsWith("id")) // assuming it's a referenced table, so we search on the linked object instead
    //                {
    //                    p.FieldName = $"{p.FieldName[..^2]}.$id";
    //                    if (p.FieldValue != null)
    //                    {
    //                        Guid myGuid = Guid.Empty;
    //                        if (Guid.TryParse(p.FieldValue, out myGuid))
    //                            p.FieldValue = myGuid;
    //                    }
    //                }
    //                switch (p.FieldOperator)
    //                {
    //                    case ComparisonOperator.EqualTo:



    //                        filters.Add(builder.Eq(p.FieldName, p.FieldValue));
    //                        break;
    //                    case ComparisonOperator.NotEqualTo:
    //                        filters.Add(builder.Not(p.FieldName));
    //                        //filters.Add(Query.Not(p.FieldName, p.FieldValue));
    //                        break;
    //                    case ComparisonOperator.Contains:
    //                        filters.Add(builder.StringIn(p.FieldName, p.FieldValue));
    //                        expressions.Add(PredicateBuilder.ContainsPredicate<T>(p.FieldName, p.FieldValueString));
    //                        //items = items.Where(containsFilter);
    //                        break;
    //                    case ComparisonOperator.StartsWith:
    //                        filters.Add(builder.Regex(p.FieldName, new Regex($"^{p.FieldValue}", RegexOptions.IgnoreCase)));
    //                        expressions.Add(PredicateBuilder.StartsWithPredicate<T>(p.FieldName, p.FieldValueString));
    //                        //items = items.Where(startsWithFilter);
    //                        ////filters.Add(Query.Contains(p.FieldName, p.FieldValueString));
    //                        break;
    //                    case ComparisonOperator.EndsWith:
    //                        filters.Add(builder.Regex(p.FieldName, new Regex($"{p.FieldValue}$", RegexOptions.IgnoreCase)));
    //                        expressions.Add(PredicateBuilder.EndsWithWithPredicate<T>(p.FieldName, p.FieldValueString));
    //                        //items = items.Where(endsWithFilter);
    //                        ////filters.Add(Query.Contains(p.FieldName, p.FieldValueString));
    //                        break;
    //                    case ComparisonOperator.LessThanOrEqualTo:
    //                        filters.Add(builder.Lte(p.FieldName, p.FieldValueString));
    //                        break;
    //                    case ComparisonOperator.LessThan:
    //                        filters.Add(builder.Lt(p.FieldName, p.FieldValueString));
    //                        break;
    //                    case ComparisonOperator.MoreThan:
    //                        filters.Add(builder.Gt(p.FieldName, p.FieldValueString));
    //                        break;
    //                    case ComparisonOperator.MoreThanOrEqualTo:
    //                        filters.Add(builder.Gte(p.FieldName, p.FieldValueString));
    //                        break;
    //                    case ComparisonOperator.IsEmpty:
    //                        filters.Add(builder.Eq(p.FieldName, BsonNull.Value));
    //                        break;
    //                    case ComparisonOperator.IsNotEmpty:
    //                        filters.Add(builder.Ne(p.FieldName, BsonNull.Value));
    //                        //filters.Add(Query.Not(p.FieldName, null));
    //                        break;
    //                }
    //            }
    //        }
    //    }
    //    if (expressions.Count > 1)
    //    {
    //        var fullExpression = expressions.First();
    //        foreach (var otherExpression in expressions.Skip(1))
    //        {
    //            fullExpression = PredicateBuilder.Or(fullExpression, otherExpression);
    //        }
    //        return items.Where(fullExpression);
    //    }
    //    else if (expressions.Count == 1)
    //    {
    //        return items.Where(expressions.First());
    //    }
    //    return items;
    //    //if (filters.Count > 1)
    //    //{
    //    //    var fullQuery = parameters.Operator == BooleanOperator.Or ? builder.Or(filters) : builder.And(filters);
    //    //    //BsonExpression fullQuery = parameters.Operator == BooleanOperator.Or ? Query.Or([.. filters]) : Query.And([.. filters]);
    //    //    return items.Where(fullQuery);
    //    //}
    //    //else if (filters.Count > 0)
    //    //    return items.Where(filters[0]);
    //    //return items;
    //}

    //private static bool IsAllowed<T>(ComparisonOperator? op, string fieldName)
    //{
    //    var memberType = ClassHelpers.GetPropertyType(typeof(T), fieldName);
    //    if (memberType == typeof(DateTime))
    //    {
    //        if (op == ComparisonOperator.Contains || op == ComparisonOperator.StartsWith || op == ComparisonOperator.EndsWith)
    //            return false; // these operators don't work for dates
    //    }
    //    TypeCode tc = Type.GetTypeCode(memberType);
    //    switch (tc)
    //    {
    //        case TypeCode.Boolean:
    //        case TypeCode.Byte:
    //        case TypeCode.Char:
    //        case TypeCode.Decimal:
    //        case TypeCode.Double:
    //        case TypeCode.Int16:
    //        case TypeCode.Int32:
    //        case TypeCode.Int64:
    //        case TypeCode.SByte:
    //        case TypeCode.UInt16:
    //        case TypeCode.UInt32:
    //        case TypeCode.UInt64:
    //            //numeric values that do not support contains, startswith, endswith
    //            if (op == ComparisonOperator.Contains || op == ComparisonOperator.StartsWith || op == ComparisonOperator.EndsWith)
    //                return false;
    //            break;
    //    }
    //    return true;
    //}

    //private static bool IsEmptyValueOperator(GenericSearchParameter p)
    //{
    //    return p.FieldOperator switch
    //    {
    //        ComparisonOperator.EqualTo or ComparisonOperator.IsEmpty or ComparisonOperator.IsNotEmpty or ComparisonOperator.NotEqualTo => true,
    //        _ => false,
    //    };
    //}

}