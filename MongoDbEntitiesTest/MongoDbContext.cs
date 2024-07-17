using GenericProvisioningLib;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;
using MongoDbEntitiesTest.DatabaseExtensions;
using MongoDbEntitiesTest.DbModels;
using MongoDbTest;
using MongoDbTest.Conventions;
using System.Runtime.CompilerServices;

namespace MongoDbEntitiesTest;

internal partial class MongoDbContext
{
    readonly string databaseName;
    readonly string connectionString;

    internal MongoDbContext(string connectionString, string databaseName)
    {
        var pack = new ConventionPack
        {
            new IgnoreExtraElementsConvention(true),
            new StringObjectIdGeneratorConvention()
        };
        ConventionRegistry.Register("My Solution Conventions", pack, t => true);
        this.databaseName = databaseName;
        this.connectionString = connectionString;
    }

    private MongoClientSettings GetSettings(string connectionString)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        settings.LinqProvider = LinqProvider.V3;
        return settings;
    }

    internal async Task<IOperationResult> Configure(IUserInformation userInfo)
    {
        BsonClassMap.RegisterClassMap<NoSqlModels.PluginConfiguration>(classMap =>
        {
            classMap.AutoMap();
            //classMap.SetIgnoreExtraElements(true);
            //classMap.SetIgnoreExtraElementsIsInherited(true);
            //classMap.UnmapMember(m => m.Id);
            //classMap.MapMember(h => h.Id).ShouldSerialize(null, null);
            //classMap.MapMember(h => h.YearBuilt).SetDefaultValue(1900);
        });
        BsonClassMap.RegisterClassMap<PhoneBookCategory>(classMap =>
        {
            classMap.AutoMap();
            //classMap.UnmapMember(m => m.PhoneBooks);
            classMap.MapProperty(x => x.PhoneBookIds)
                .SetSerializer(
                    new EnumerableInterfaceImplementerSerializer<List<string>, string>(
                    new StringSerializer(BsonType.ObjectId)));
            //classMap.MapMember(h => h.Id).ShouldSerialize(null, null);
            //classMap.MapMember(h => h.YearBuilt).SetDefaultValue(1900);
        });
        BsonClassMap.RegisterClassMap<PhoneBookContact>(classMap =>
        {
            classMap.AutoMap();
            classMap.UnmapMember(m => m.Categories);
            classMap.UnmapMember(m => m.PhoneBooks);
            classMap.MapProperty(m => m.NumberOfTelephoneNumbers);
            classMap.MapProperty(x => x.ManagerId).SetSerializer(new StringSerializer(BsonType.ObjectId));
            //classMap.MapProperty(x => x.Numbers[0].Number).SetSerializer(new StringSerializer(BsonType.ObjectId));

            classMap.MapProperty(x => x.PhoneBookIds)
                .SetSerializer(
                    new EnumerableInterfaceImplementerSerializer<List<string>, string>(
                    new StringSerializer(BsonType.ObjectId)));
            classMap.MapProperty(x => x.CategoryIds)
                .SetSerializer(
                    new EnumerableInterfaceImplementerSerializer<List<string>, string>(
                    new StringSerializer(BsonType.ObjectId)));
            //classMap.MapMember(h => h.Id).ShouldSerialize(null, null);
            //classMap.MapMember(h => h.YearBuilt).SetDefaultValue(1900);
        });
        IOperationResult result = new GenericOperationResult();
        var settings = GetSettings(connectionString);
        try
        {
            await DB.InitAsync(databaseName, settings);
            result.IsSuccess = true;
        }
        catch (Exception e)
        {
            result.ErrorMessage = $"Unable to initialize connection: {e.Message}";
        }
        return result;
    }

    internal async Task<IOperationResult> CheckConnectivity(IUserInformation userInfo)
    {
        try
        {
            var db = DB.Database(null);
            var result = await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1)).ConfigureAwait(false);
            return GenericOperationResult.Success;
        }
        catch (Exception e)
        {
            GenericOperationResult result = new();
            ProcessException(e, nameof(CheckConnectivity), result, userInfo);
            return result;
        }
    }

    internal async Task<IOperationResult<List<string>>> GetDatabases(IUserInformation userInfo)
    {
        IOperationResult<List<string>> result = new GenericOperationResult<List<string>>();
        try
        {
            var items = await DB.AllDatabaseNamesAsync(MongoClientSettings.FromConnectionString(connectionString)).ConfigureAwait(false);
            result.Result = items.ToList();
            result.IsSuccess = true;
            return result;
        }
        catch (Exception e)
        {
            ProcessException(e, nameof(GetDatabases), result, userInfo);
            return result;
        }
    }

    #region generic operations

    public async Task<IOperationResult> AddObject<T>(T obj, IUserInformation userInfo) 
        where T : BaseEntity, NoSqlModels.IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Add))
            //    return NoPermissionError<T>(form, TopLevelPermission.Add);
            var result = new GenericOperationResult<T>();
            //var col = context.GetCollection<T>();
            //var validateRes = ValidateAddOrUpdateInput(obj, col, false);
            //if (!validateRes.IsSuccess)
            //    return validateRes;
            //ProcessNewDbObject(obj, context, true);
            await obj.SaveAsync().ConfigureAwait(false);
            await ProcessNewDbObject(obj, context, true);

            //await DB.InsertAsync(obj).ConfigureAwait(false);
            //col.InsertOne(obj);
            //MakeObjectAccessible(obj, context);
            //result.Result = obj.FromDbObject(true, false);
            result.Result = obj;
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }



    public IOperationResult<T> GetObject<T>(string id, IUserInformation userInfo, bool includeDependencies = false,
        bool ignoreObjectAccessibility = false, bool ignorePermissions = false, bool includeCredentials = true)
        where T : BaseEntity, NoSqlModels.IIdItem
    {
        return PerformDatabaseOperation(context =>
        {
            //if (perm != null && !CheckPermission(context, perm.Form, perm.Permission))
            //    return NoPermissionError<T>(perm.Form, perm.Permission);
            //else if (!ignorePermissions)
            //{
            //    var form = GetGuiFormFromObject<T>();
            //    if (!CheckPermission(context, form, TopLevelPermission.Show))
            //        return NoPermissionError<T>(form, TopLevelPermission.Show);
            //}
            var result = new GenericOperationResult<T>();
            var col = context.AccessibleObjects<T>(!ignoreObjectAccessibility);
            //if (includeDependencies)
            //    col = IncludeDependencies(col);

            var item = col.Where(u => u.Id == id).FirstOrDefault();
            if (item != null)
            {
                //result.Result = item.FromDbObject(includeDependencies, includeCredentials);
                result.Result = item;
                result.IsSuccess = true;
            }
            else
                return ObjectNotFoundError<T>(id);
            return result;
        }, userInfo);
    }

    public IOperationResult<PhoneBookContact> GetContactWithManager(string id, IUserInformation userInfo)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult<PhoneBookContact>();
            var col = context.AccessibleObjects<PhoneBookContact>(false);
            col = col.Join(col, contact => contact.ManagerId, manager => manager.Id, (contact, manager) => new PhoneBookContact
            { 
                Id = contact.Id, 
                FirstName = contact.FirstName, 
                ManagerId = contact.ManagerId, 
                Manager = manager.Manager 
            });

            //col = col.Join(col, contact => contact.SecretaryIds, secretary => secretary.Id, (contact, secretary) => contact);

            var hydratedContact = context.GetCollection<PhoneBookContact>()
                .Aggregate()
                .Match(u => u.Id == id)
                .Lookup(GetCollectionName<PhoneBookContact>(), nameof(PhoneBookContact.ManagerId), "_id", nameof(PhoneBookContact.Manager))
                .Unwind(nameof(PhoneBookContact.Manager))
                .Lookup(GetCollectionName<PhoneBookContact>(), nameof(PhoneBookContact.SecretaryIds), "_id", nameof(PhoneBookContact.Secretary))
                .As<PhoneBookContact>()
                .FirstOrDefault();

            var item = col.Where(u => u.Id == id).FirstOrDefault();
            if (item != null)
            {
                //result.Result = item.FromDbObject(includeDependencies, includeCredentials);
                result.Result = item;
                result.IsSuccess = true;
            }
            else
                return ObjectNotFoundError<PhoneBookContact>(id);
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> UpdateObject<T>(T obj, IUserInformation userInfo, bool ignoreUpdateOfInternalFields = true)
        where T : BaseEntity, NoSqlModels.IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Update))
            //    return NoPermissionError(form, TopLevelPermission.Update);
            var result = new GenericOperationResult();

            var existingItem = context.AccessibleObjects<T>(true).Where(x => x.Id == obj.Id).FirstOrDefault();
            if (existingItem == null)
                return ObjectNotFoundError<T>(obj.Id);
            var col = context.GetCollection<T>();
            //var validateRes = ValidateAddOrUpdateInput(obj, col, true);
            //if (!validateRes.IsSuccess)
            //    return validateRes;
            //ProcessUpdatedDbObject(obj, context, false, []);
            var ignoreList = new List<string> { nameof(NoSqlModels.IIdItem.Id) };
            //ignoreList.AddRange(typeof(T).GetIgnorePropertiesForObjectUpdate(ignoreUpdateOfInternalFields, true));
            var differences = ObjectDiffUtils.ObjectDiff.GenerateObjectDiff(existingItem, obj, [.. ignoreList]);
            if (differences.Count <= 0) return GenericOperationResult.Success;

            //var update = differences.GenerateUpdate(obj);
            //var updateRes = context.GetCollection<T>().UpdateOne(u => u.Id == obj.Id, update);
            ObjectDiffUtils.ObjectDiff.ApplyDiffData(existingItem, differences);
            await existingItem.SaveAsync().ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> UpdateObject<T>(string id, NoSqlModels.DeltaBaseObject<T> update, IUserInformation userInfo, bool ignoreUpdateOfInternalFields = true)
        where T : BaseEntity, NoSqlModels.IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Update))
            //    return NoPermissionError<T>(form, TopLevelPermission.Update);
            var existingItem = context.AccessibleObjects<T>(true).Where(x => x.Id == id).FirstOrDefault();
            if (existingItem == null)
                return ObjectNotFoundError<T>(id);
            //var col = context.GetCollection<T>();
            //var validateRes = ValidateAddOrUpdateInput(update.Data, col, true);
            //if (!validateRes.IsSuccess)
            //    return validateRes;
            //ProcessUpdatedDbObject(update.Data, context, true, update.IncludedProperties);
            return await UpdateObject(existingItem, update).ConfigureAwait(false);
        }, userInfo);
    }

    private async Task<IOperationResult<T>> UpdateObject<T>(T existingItem, NoSqlModels.DeltaBaseObject<T> update)
        where T : BaseEntity, NoSqlModels.IIdItem
    {
        GenericOperationResult<T> result = new()
        {
            //Result = existingItem.FromDbObject()
            Result = existingItem
        };
        var updater = DB.Update<T>().MatchID(existingItem.Id);
        updater = existingItem.ApplyUpdates(update, updater);
        await updater.ExecuteAsync().ConfigureAwait(false);
        //var updates = existingItem.GenerateUpdates(update);
        //if (updates.Count > 0)
        //{
        //    if (existingItem is PhoneBookCategory contact && update is NoSqlModels.DeltaBaseObject<PhoneBookCategory> detailedUpdate)
        //    {
        //        var updater = DB.Update<PhoneBookCategory>().MatchID(existingItem.Id);
        //        //updater = updater.Modify(u => u.Name, detailedUpdate.Data.Name);
        //        //updater = updater.Modify(u => u.Set(a => a.Name, detailedUpdate.Data.Name));
        //        updater = updater.Modify(u => u.Set(nameof(PhoneBookCategory.Name), detailedUpdate.Data.Name));
        //        await updater.ExecuteAsync().ConfigureAwait(false);
        //    }
        //    else
        //    {
        //        var updater = DB.Update<T>().Match(id => existingItem.Id);
        //        foreach (var upd in updates)
        //        {
        //            updater = updater.Modify(x => GenerateModifyFunc(x, upd));
        //        }
        //        await updater.ExecuteAsync().ConfigureAwait(false);
        //    }
        //}
        //UpdateDefinition<T> modifyFunc(UpdateDefinitionBuilder<T> x) => existingItem.GenerateUpdate(update);
        //var updateDefinition = existingItem.GenerateUpdate(update);
        //await DB.Update<T>().Match(id => existingItem.Id).Modify(modifyFunc).ExecuteAsync().ConfigureAwait(false);
        //var updateRes = col.UpdateOne(u => u.Id == existingItem.Id, updateDefinition);
        result.IsSuccess = true;
        return result;
    }

    private UpdateDefinition<T> GenerateModifyFunc<T>(UpdateDefinitionBuilder<T> x, UpdateDefinition<T> y) where T : BaseEntity, NoSqlModels.IIdItem
    {
        return y;
    }

    public IOperationResult<NoSqlModels.SearchResults<T>> SearchObjects<T>(NoSqlModels.GenericSearchParameters parameters, IUserInformation userInfo)
    where T : BaseEntity, NoSqlModels.IIdItem
    {
        return PerformDatabaseOperation(context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Show))
            //    return NoPermissionError<SearchResults<T>>(form, TopLevelPermission.Show);
            var result = new GenericOperationResult<NoSqlModels.SearchResults<T>>();
            var col = context.AccessibleObjects<T>(true); // GetCollection<T>(GetCollectionName<T>());
            col = IncludeDependencies(col);
            col = FilterHelpers.AddSortInclusions(col, parameters);
            var query = col;
            if (typeof(PhoneBookContact).IsAssignableFrom(typeof(T)))
            { // filter by name/query is done later one
            }
            //else if (typeof(BaseObject).IsAssignableFrom(typeof(T)))
            //{
            //    query = FilterHelpers.FilterByNameAndDescriptionGeneric(query, parameters);
            //    //var myItems = query as ILiteQueryable<BaseObject>;
            //    //var myQuery = FilterHelpers.FilterByNameAndDescription(myItems, parameters);
            //    //query = myQuery as ILiteQueryable<T>;
            //}
            else if (typeof(NoSqlModels.INamedItem).IsAssignableFrom(typeof(T)))
            {
                var myItems = query as IQueryable<NoSqlModels.INamedItem>;
                query = FilterHelpers.FilterByNameGeneric(query, parameters);
                //var filteredQuery = FilterHelpers.FilterByNameGeneric(myItems, parameters).Cast<T>();
                //query = filteredQuery as IMongoQueryable<T>;
                //var myItems = query as ILiteQueryable<INamedItem>;
                //var myQuery = FilterHelpers.FilterByName(myItems, parameters);
                //query = myQuery as ILiteQueryable<T>;
            }
            bool isSorted = false;
            query = FilterSearch(query, parameters);
            var queryable = query as IQueryable<T>;
            query = FilterHelpers.AppendGenericFilter(queryable, parameters, ref isSorted) as IMongoQueryable<T>;
            if (!isSorted)
            {
                if (typeof(PhoneBookContact).IsAssignableFrom(typeof(T))) // phonebook contact
                {
                    query = query.SortBy(nameof(PhoneBookContact.LastName), true).ThenSortBy(nameof(PhoneBookContact.FirstName), true);
                }
                else
                    query = query.SortBy(nameof(NoSqlModels.INamedItem.Name), true); // default sorting by name
            }
            result.Result = GeneratePagedListWithoutSorting(query, parameters);
            //result.Result = new SearchResults<T>
            //{
            //    ItemCount = query.Count()
            //};
            //if (parameters.PageSize > 0)
            //{
            //    var results = query.Limit(parameters.PageSize).Offset((parameters.Page - 1) * parameters.PageSize);
            //    //query = query.Skip((parameters.Page - 1) * parameters.PageSize).Take(parameters.PageSize);
            //    var currentCount = (parameters.Page - 1) * parameters.PageSize + results.Count();
            //    if (result.Result.ItemCount > currentCount)
            //        result.Result.More = true;
            //    result.Result.Results = results.ToList().Select(x => x.FromDbObject(parameters.IncludeDependentObjects, false)).ToList();
            //}
            //else
            //    result.Result.Results = query.ToList().Select(x => x.FromDbObject(parameters.IncludeDependentObjects, false)).ToList();
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    internal static IMongoQueryable<T> FilterSearch<T>(IMongoQueryable<T> items, NoSqlModels.GenericSearchParameters parameters)
    {
        switch (parameters)
        {
            case NoSqlModels.PhoneBookCategorySearchParameters phoneBookCategorySearchParameters:
                {
                    if (items is not IMongoQueryable<PhoneBookCategory> query) return items;
                    if (phoneBookCategorySearchParameters.PhoneBookIds?.Count > 0)
                    {
                        query = query.Where(x => x.PhoneBookIds.Any(x => phoneBookCategorySearchParameters.PhoneBookIds.Contains(x)));
                    }
                    return query as IMongoQueryable<T>;
                }
            case NoSqlModels.PhoneBookContactSearchParameters phoneBookContactSearchParameters:
                {
                    if (items is not IMongoQueryable<PhoneBookContact> query) return items;
                    query = FilterHelpers.FilterPhoneBookContacts(query, phoneBookContactSearchParameters.Name, false, false);
                    query = FilterHelpers.FilterPhoneBookContacts(query, phoneBookContactSearchParameters.Query);
                    if (phoneBookContactSearchParameters.CategoryIds?.Count > 0)
                    {
                        query = query.Where(x => x.CategoryIds.Any(x => phoneBookContactSearchParameters.CategoryIds.Contains(x)));
                    }
                    if (phoneBookContactSearchParameters.ManagerIds?.Count > 0)
                    {
                        query = query.Where(x => x.ManagerId != null && phoneBookContactSearchParameters.ManagerIds.Contains(x.ManagerId));
                    }
                    if (phoneBookContactSearchParameters.SecretaryIds?.Count > 0)
                    {
                        query = query.Where(x => x.SecretaryIds.Any(x => phoneBookContactSearchParameters.SecretaryIds.Contains(x)));
                    }
                    query = FilterHelpers.FilterStringProperties(query, phoneBookContactSearchParameters);
                    return query as IMongoQueryable<T>;
                }
        }
        return items;
    }

    internal static NoSqlModels.SearchResults<T> GeneratePagedListWithoutSorting<T>(IMongoQueryable<T> query, NoSqlModels.GenericSearchParameters parameters)
    {
        var result = new NoSqlModels.SearchResults<T>
        {
            ItemCount = query.Count()
        };
        if (parameters.PageSize > 0)
        {
            var results = query.Skip(parameters.PageSize * (parameters.Page - 1)).Take(parameters.PageSize);
            //query = query.Skip((parameters.Page - 1) * parameters.PageSize).Take(parameters.PageSize);
            var currentCount = (parameters.Page - 1) * parameters.PageSize + results.Count();
            if (result.ItemCount > currentCount)
                result.More = true;
            result.Results = results.ToList();//.Select(x => x.FromDbObject(parameters.IncludeDependentElements, false)).ToList();
        }
        else
            result.Results = query.ToList();//.Select(x => x.FromDbObject(parameters.IncludeDependentElements, false)).ToList();
        return result;
    }

    public async Task<IOperationResult> DeleteObject<T>(string id, IUserInformation userInfo)
        where T : BaseEntity, NoSqlModels.IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Delete))
            //    return NoPermissionError(form, TopLevelPermission.Delete);
            return await DeleteObject<T>(id, context).ConfigureAwait(false);
        }, userInfo);
    }

    private async Task<IOperationResult> DeleteObject<T>(string id, MongoDatabaseContext context)
        where T : BaseEntity, NoSqlModels.IIdItem
    {
        var result = new GenericOperationResult();
        var col = context.AccessibleObjects<T>(true);
        var existingItem = col.Where(x => x.Id == id).FirstOrDefault();
        if (existingItem == null)
            return ObjectNotFoundError<T>(id);
        await DB.DeleteAsync<T>(id).ConfigureAwait(false);
        result.IsSuccess = true;
        return result;
    }

    #endregion

    #region phonebook configuration

    public async Task<IOperationResult> AddSecretary(string contactId, string secretaryId, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var contacts = context.AccessibleObjects<PhoneBookContact>(true);
            var contact = await contacts.FirstOrDefaultAsync(u => u.Id == contactId);
            if (contact == null)
                return new GenericOperationResult { ErrorMessage = "Contact not found" };
            var categories = context.AccessibleObjects<PhoneBookCategory>(true);
            var secretary = await contacts.FirstOrDefaultAsync(u => u.Id == secretaryId).ConfigureAwait(false);
            if (secretary == null)
                return new GenericOperationResult { ErrorMessage = "Secretary not found" };
            if (!contact.Secretary.Any(x => x.Id == secretaryId))
                return GenericOperationResult.Success;
            contact.Secretary ??= [];
            await contact.Secretary.AddAsync(secretaryId).ConfigureAwait(false);
            contact.SecretaryIds ??= [];
            contact.SecretaryIds.Add(secretaryId);
            await DB.Update<PhoneBookContact>().MatchID(contactId).Modify(u => u.SecretaryIds, contact.SecretaryIds).ExecuteAsync().ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> RemoveSecretary(string contactId, string secretaryId, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var contacts = context.AccessibleObjects<PhoneBookContact>(true);
            var contact = await contacts.FirstOrDefaultAsync(u => u.Id == contactId);
            if (contact == null)
                return new GenericOperationResult { ErrorMessage = "Contact not found" };
            var categories = context.AccessibleObjects<PhoneBookCategory>(true);
            var secretaryExists = await contacts.AnyAsync(x => x.Id == secretaryId).ConfigureAwait(false);
            if (!secretaryExists)
                return new GenericOperationResult { ErrorMessage = "Secretary not found" };
            if (contact.Secretary.Any(x => x.Id == secretaryId))
            {
                await contact.Secretary.RemoveAsync(secretaryId).ConfigureAwait(false);
            }
            else
                return GenericOperationResult.Success;
            if (contact.SecretaryIds?.Contains(secretaryId) == true)
            {
                contact.SecretaryIds.Remove(secretaryId);
                await DB.Update<PhoneBookContact>().MatchID(contactId).Modify(u => u.SecretaryIds, contact.SecretaryIds).ExecuteAsync().ConfigureAwait(false);
            }
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> AssociateWithCategory(string contactId, string categoryId, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var contacts = context.AccessibleObjects<PhoneBookContact>(true);
            var contact = await contacts.FirstOrDefaultAsync(u => u.Id == contactId);
            if (contact == null)
                return new GenericOperationResult { ErrorMessage = "Contact not found" };
            var categories = context.AccessibleObjects<PhoneBookCategory>(true);
            var category = await categories.FirstOrDefaultAsync(u => u.Id == categoryId).ConfigureAwait(false);
            if (category == null)
                return new GenericOperationResult { ErrorMessage = "Category not found" };
            if (contact.CategoryIds?.Contains(categoryId) == true) //.Any(x => x.Id == categoryId)) // already present
                return GenericOperationResult.Success;
            contact.Categories ??= [];
            await contact.Categories.AddAsync(categoryId).ConfigureAwait(false);
            contact.CategoryIds ??= [];
            contact.CategoryIds.Add(categoryId);
            await DB.Update<PhoneBookContact>().MatchID(contactId).Modify(u => u.CategoryIds, contact.CategoryIds).ExecuteAsync().ConfigureAwait(false);
            //var updateDefinition = Builders<PhoneBookContact>.Update
            //    .Set(u => u.CategoryIds, contact.CategoryIds);
            ////.Set(u => u.Categories, contact.Categories);
            //var updateRes = context.GetCollection<PhoneBookContact>().UpdateOne(u => u.Id == contactId, updateDefinition);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> RemoveCategory(string contactId, string categoryId, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var contacts = context.AccessibleObjects<PhoneBookContact>(true);
            var contact = await contacts.FirstOrDefaultAsync(x => x.Id == contactId).ConfigureAwait(false);
            if (contact == null)
                return new GenericOperationResult { ErrorMessage = "Contact not found" };
            if (contact.Categories.Any(x => x.Id == categoryId)) // present
            {
                await contact.Categories.RemoveAsync(categoryId).ConfigureAwait(false);
                //contact.Categories.RemoveAll(u => u.Id == categoryId);
                await DB.Update<PhoneBookContact>().MatchID(contactId).Modify(u => u.CategoryIds, contact.CategoryIds).ExecuteAsync().ConfigureAwait(false);
                //contact.CategoryIds.Remove(categoryId);
                //var updateDefinition = Builders<PhoneBookContact>.Update
                //    .Set(u => u.CategoryIds, contact.CategoryIds)
                //    .Set(u => u.Categories, contact.Categories);
                //var updateRes = context.GetCollection<PhoneBookContact>().UpdateOne(u => u.Id == contactId, updateDefinition);
            } // not present => Ok as well
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    #endregion

    #region helpers

    internal static IMongoQueryable<T> IncludeDependencies<T>(IMongoQueryable<T> query)
    {
        var includeList = typeof(T).GetDependencyProperties();
        foreach (var includeProp in includeList)
        { // $.Customer, $.Customer.Address
          //query = query.Include($"$.{includeProp}");
        }
        return query;
    }

    protected IOperationResult<T> PerformDatabaseOperation<T>(Func<MongoDatabaseContext, IOperationResult<T>> myFunc, IUserInformation userInfo,
        [CallerMemberName] string methodName = null)
    {
        try
        {
            var context = new MongoDatabaseContext(DB.Database(null), userInfo);
            //User = userInfo;
            return myFunc(context);
        }
        catch (Exception e)
        {
            var result = new GenericOperationResult<T>();
            ProcessException(e, methodName, result, userInfo);
            return result;
        }
    }

    protected async Task<IOperationResult<T>> PerformDatabaseOperationAsync<T>(Func<MongoDatabaseContext, Task<IOperationResult<T>>> myFunc, IUserInformation userInfo,
        [CallerMemberName] string methodName = null)
    {
        try
        {
            var context = new MongoDatabaseContext(DB.Database(null), userInfo);
            //User = userInfo;
            return await myFunc(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            var result = new GenericOperationResult<T>();
            ProcessException(e, methodName, result, userInfo);
            return result;
        }
    }

    protected IOperationResult PerformDatabaseOperation(Func<MongoDatabaseContext, IOperationResult> myFunc, IUserInformation userInfo,
        [CallerMemberName] string methodName = null)
    {
        try
        {
            var context = new MongoDatabaseContext(DB.Database(null), userInfo);
            //User = userInfo;
            return myFunc(context);
        }
        catch (Exception e)
        {
            var result = new GenericOperationResult();
            ProcessException(e, methodName, result, userInfo);
            return result;
        }
    }

    protected async Task<IOperationResult> PerformDatabaseOperationAsync(Func<MongoDatabaseContext, Task<IOperationResult>> myFunc, IUserInformation userInfo,
        [CallerMemberName] string methodName = null)
    {
        try
        {
            var context = new MongoDatabaseContext(DB.Database(null), userInfo);
            //User = userInfo;
            return await myFunc(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            var result = new GenericOperationResult();
            ProcessException(e, methodName, result, userInfo);
            return result;
        }
    }

    internal static string GetCollectionName<T>() where T : class
    {
        var className = typeof(T).Name;
        className = $"{className[..1].ToLower()}{className[1..]}";
        if (!className.EndsWith('s'))
            className = $"{className}s";
        return className;
    }

    private void ProcessException(Exception e, string location, IOperationResult result, IUserInformation userInfo)
    {
        if (e is MongoWriteException we)
        {
            result.ErrorMessage = $"Unable to write {we.WriteError.Message}";
        }
        else
            result.ErrorMessage = "Generic problem in database";
        result.ErrorDetail = $"Generic exception in {location} : {e.Message} at {e.StackTrace}";
        Log(result.ErrorDetail, 2, userInfo);
    }

    private void Log(string message, int severity, IUserInformation userInfo)
    {
        Console.WriteLine($"{userInfo}:{message}");
    }

    protected IOperationResult<T> ObjectNotFoundError<T>(Guid id) where T : class
    {
        return new GenericOperationResult<T>
        {
            ErrorMessage = GetTranslatedString(DatabaseErrors.ObjectNotFound, typeof(T).Name, id),
            //ErrorType = ErrorType.ObjectNotFound
        };
    }

    protected IOperationResult<T> ObjectNotFoundError<T>(string id) where T : class
    {
        return new GenericOperationResult<T>
        {
            ErrorMessage = GetTranslatedString(DatabaseErrors.ObjectNotFound, typeof(T).Name, id),
            //ErrorType = ErrorType.ObjectNotFound
        };
    }

    protected string GetTranslatedString(string str, params object[] arguments)
    {
        //if (Localizer != null)
        //    return Localizer.GetString(str, arguments).Value;
        return str;
    }

    #endregion
}