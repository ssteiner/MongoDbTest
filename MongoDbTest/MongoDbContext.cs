using GeneralTools.Extensions;
using GenericProvisioningLib;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using MongoDbTest.Conventions;
using MongoDbTest.Extensions;
using MongoDbTest.Helpers;
using NoSqlModels;
using NoSqlModels.Extensions;
using System.Runtime.CompilerServices;

namespace MongoDbTest;

internal class MongoDbContext
{
    readonly MongoClient client;
    readonly string databaseName;

    internal MongoDbContext(string connectionString, string databaseName)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        settings.LinqProvider = LinqProvider.V3;

        var mySettings = new MongoClientSettings()
        {
            Scheme = ConnectionStringScheme.MongoDBPlusSrv,
            ApplicationName = "DevCluster0",
            Credential = new MongoCredential(null, new MongoInternalIdentity("admin", "dbadmin"), new PasswordEvidence("hQX0qfDVZVkjnd02")),
            ConnectTimeout = TimeSpan.FromSeconds(30), 
            RetryWrites = true
        };

        var pack = new ConventionPack
        {
            new IgnoreExtraElementsConvention(true),
            new StringObjectIdGeneratorConvention()
        };
        ConventionRegistry.Register("My Solution Conventions", pack, t => true);

        client = new MongoClient(settings);
        this.databaseName = databaseName;
    }

    internal async Task<IOperationResult> Configure(IUserInformation userInfo)
    {
        BsonClassMap.RegisterClassMap<PluginConfiguration>(classMap =>
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
            classMap.UnmapMember(m => m.PhoneBooks);
            classMap.MapProperty(x => x.PhoneBookIds)
                .SetSerializer(
                    new EnumerableInterfaceImplementerSerializer<List<string>, string>(
                    new StringSerializer(BsonType.ObjectId)));
        });

        BsonClassMap.RegisterClassMap<PhoneBookContactNumber>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(c => c.Id)
              .SetIdGenerator(StringObjectIdGenerator.Instance)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(c => c.Number).SetSerializer(new StringSerializer(BsonType.String));
        });

        BsonClassMap.RegisterClassMap<PhoneBookContact>(cm =>
        {
            cm.AutoMap();
            cm.UnmapMember(m => m.Categories);
            cm.UnmapMember(m => m.PhoneBooks);
            cm.MapProperty(m => m.NumberOfTelephoneNumbers);
            cm.MapProperty(x => x.ManagerId).SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapProperty(x => x.PhoneBookIds)
                .SetSerializer(new EnumerableInterfaceImplementerSerializer<List<string>, string>(
                    new StringSerializer(BsonType.ObjectId)));
            cm.MapProperty(x => x.CategoryIds)
                .SetSerializer(new EnumerableInterfaceImplementerSerializer<List<string>, string>(
                    new StringSerializer(BsonType.ObjectId)));

            cm.MapProperty(x => x.SecretaryIds)
                .SetSerializer(new EnumerableInterfaceImplementerSerializer<List<string>, string>(
                    new StringSerializer(BsonType.ObjectId)));

            cm.MapMember(x => x.Numbers).SetSerializer(
                new EnumerableInterfaceImplementerSerializer<List<PhoneBookContactNumber>, PhoneBookContactNumber>(
                    BsonSerializer.LookupSerializer<PhoneBookContactNumber>()));
        });

        //BsonClassMap.RegisterClassMap<PhoneBookContactNumber>(classMap =>
        //{
        //    classMap.AutoMap();
        //    classMap.MapIdMember(c => c.Id)
        //      .SetIdGenerator(StringObjectIdGenerator.Instance)
        //      .SetSerializer(new StringSerializer(BsonType.ObjectId));
        //    classMap.MapMember(c => c.Number).SetSerializer(new StringSerializer(BsonType.String));
        //});

        //BsonClassMap.RegisterClassMap<PhoneBookContact>(classMap =>
        //{
        //    classMap.AutoMap();
        //    classMap.UnmapMember(m => m.Secretary);
        //    classMap.UnmapMember(m => m.Categories);
        //    classMap.UnmapMember(m => m.PhoneBooks);
        //    classMap.MapProperty(m => m.NumberOfTelephoneNumbers);
        //    classMap.MapProperty(x => x.ManagerId).SetSerializer(new StringSerializer(BsonType.ObjectId));
        //    classMap.MapMember(x => x.Numbers)
        //        .SetSerializer(
        //            new EnumerableInterfaceImplementerSerializer<List<PhoneBookContactNumber>, PhoneBookContactNumber>(
        //            BsonSerializer.LookupSerializer<PhoneBookContactNumber>()));

        //    //classMap.MapProperty(x => x.Numbers[0].Id).SetSerializer(new StringSerializer(BsonType.ObjectId));
        //    //classMap.MapProperty(x => x.Numbers)
        //    //    .SetSerializer(
        //    //        new EnumerableInterfaceImplementerSerializer<List<PhoneBookContactNumber>, PhoneBookContactNumber>(
        //    //        new PhoneBookContactNumberSerializer()));

        //    classMap.MapProperty(x => x.SecretaryIds)
        //        .SetSerializer(
        //            new EnumerableInterfaceImplementerSerializer<List<string>, string>(
        //            new StringSerializer(BsonType.ObjectId)));
        //    classMap.MapProperty(x => x.PhoneBookIds)
        //        .SetSerializer(
        //            new EnumerableInterfaceImplementerSerializer<List<string>, string>(
        //            new StringSerializer(BsonType.ObjectId)));
        //    classMap.MapProperty(x => x.CategoryIds)
        //        .SetSerializer(
        //            new EnumerableInterfaceImplementerSerializer<List<string>, string>(
        //            new StringSerializer(BsonType.ObjectId)));
        //});


        return await PerformDatabaseOperationAsync(async context =>
        {
            IOperationResult result = new GenericOperationResult();
            var collections = await context.Database.ListCollectionNamesAsync().ConfigureAwait(false);
            var collectionNames = await collections.ToListAsync().ConfigureAwait(false);

            var collectionOptions = new CreateCollectionOptions
            {
                //Collation = new Collation("simple", strength: CollationStrength.Secondary, numericOrdering: true),
                //ValidationLevel = DocumentValidationLevel.Moderate,
                //ValidationAction = DocumentValidationAction.Error
            };
            if (!collectionNames.Contains(GetCollectionName<PluginConfiguration>(), StringComparer.OrdinalIgnoreCase))
            {
                await context.Database.CreateCollectionAsync(GetCollectionName<PluginConfiguration>(), collectionOptions).ConfigureAwait(false);
            }
            if (!collectionNames.Contains(GetCollectionName<PhoneBookCategory>(), StringComparer.OrdinalIgnoreCase))
            {
                await context.Database.CreateCollectionAsync(GetCollectionName<PhoneBookCategory>(), collectionOptions).ConfigureAwait(false);
            }
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    internal MongoDbContext(string login, string password, string address, string databaseName)
    {
        var settings = new MongoClientSettings()
        {
            Scheme = ConnectionStringScheme.MongoDB,
            Server = new MongoServerAddress("localhost", 27017), 
            Credential = new MongoCredential(null, new MongoInternalIdentity(databaseName, login), new PasswordEvidence(password))
        };
        if (!string.IsNullOrEmpty(login))
            settings.Credential = new MongoCredential(null, new MongoInternalIdentity(databaseName, login), new PasswordEvidence(password));
        settings.LinqProvider = LinqProvider.V3;
        client = new MongoClient(settings);
        this.databaseName = databaseName;
    }

    internal async Task<IOperationResult> CheckConnectivity(IUserInformation userInfo)
    {
        try
        {
            var db = client.GetDatabase("admin");
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
            var items = await client.ListDatabaseNamesAsync().ConfigureAwait(false);
            result.Result = await items.ToListAsync().ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }
        catch (Exception e)
        {
            ProcessException(e, nameof(GetDatabases), result, userInfo);
            return result;
        }
    }

    #region file storage

    private static GridFSBucket GetAudioFilesBucket(MongoDatabaseContext context) => new(context.Database, new GridFSBucketOptions { BucketName = "audioFiles" });

    public async Task<IOperationResult<AudioFileStorage>> GetAudioFile(string ownerId, AudioFileType type, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult<AudioFileStorage>();
            var audioFiles = context.AccessibleObjects<AudioFileStorage>(true);

            var filter = Builders<GridFSFileInfo>.Filter.And(
                Builders<GridFSFileInfo>.Filter.Eq("metadata.ownerId", ownerId),
                Builders<GridFSFileInfo>.Filter.Eq("metadata.audioType", type));

            var bucket = GetAudioFilesBucket(context);
            using var cursor = await bucket.FindAsync(filter).ConfigureAwait(false);
            var files = await cursor.ToListAsync().ConfigureAwait(false);

            var fileKey = GetAudioFileId(ownerId, type);

            var fileInfo = files.FirstOrDefault();
            if (fileInfo == null)
                return ObjectNotFoundError<AudioFileStorage>(fileKey);
            //result.Result = await ReadAudioFile(fileInfo, bucket, context).ConfigureAwait(false);
            result.Result = new AudioFileStorage
            {
                FileName = fileInfo.Filename,
                AudioType = type, Id = fileInfo.Id.ToString(),
                UploadDate = fileInfo.UploadDateTime,
                UploadedBy = fileInfo.Metadata["uploadedBy"].AsString
            };
            result.Result.Contents = await bucket.DownloadAsBytesAsync(fileInfo.Id).ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    private static async Task<AudioFileStorage> ReadAudioFile(GridFSFileInfo fileInfo, GridFSBucket bucket, MongoDatabaseContext context)
    {
        AudioFileStorage file = new()
        {
            FileName = fileInfo.Filename,
            Id = fileInfo.Id.ToString(),
            UploadDate = fileInfo.UploadDateTime,
            UploadedBy = fileInfo.Metadata["uploadedBy"].AsString,
            Contents = await bucket.DownloadAsBytesAsync(fileInfo.Id).ConfigureAwait(false)
        };
        var audioType = fileInfo.Metadata["audioType"].AsInt32;
        file.AudioType = (AudioFileType)audioType;
        return file;
    }

    public async Task<IOperationResult<List<AudioFileStorage>>> GetAudioFiles(string ownerId, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            IOperationResult<List<AudioFileStorage>> result = new GenericOperationResult<List<AudioFileStorage>>();
            var bucket = GetAudioFilesBucket(context);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.ownerId", ownerId);
            using var cursor = await bucket.FindAsync(filter).ConfigureAwait(false);
            var files = await cursor.ToListAsync().ConfigureAwait(false);
            result.Result = [];
            foreach (var file in files)
            {
                result.Result.Add(await ReadAudioFile(file, bucket, context).ConfigureAwait(false));
            }
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> AddAudioFile(string ownerId, AudioFileType type, AudioFileStorage audioFile, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            audioFile.AudioType = type;
            await AddAudioFile(ownerId, audioFile, context).ConfigureAwait(false);
            //await context.Session.SaveChangesAsync().ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    private static async Task AddAudioFile(string ownerId, AudioFileStorage audioFile, MongoDatabaseContext context)
    {
        audioFile.UploadedBy = context.User.UserId;
        audioFile.UploadDate = DateTime.Now;

        var filter = Builders<GridFSFileInfo>.Filter.And(
            Builders<GridFSFileInfo>.Filter.Eq("metadata.ownerId", ownerId),
            Builders<GridFSFileInfo>.Filter.Eq("metadata.audioType", audioFile.AudioType));

        var bucket = GetAudioFilesBucket(context);
        using var cursor = await bucket.FindAsync(filter).ConfigureAwait(false);
        var files = await cursor.ToListAsync().ConfigureAwait(false);
        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                { "uploadedBy", context.User.UserId },
                { "audioType", audioFile.AudioType },
                { "ownerId", ownerId }
            }
        };
        var id = await bucket.UploadFromBytesAsync(audioFile.FileName, audioFile.Contents, options).ConfigureAwait(false);
        audioFile.Id = id.ToString();
        if (files.Any())
        {
            await bucket.DeleteAsync(files.First().Id).ConfigureAwait(false);
        }
    }

    public async Task<IOperationResult> AddAudioFiles(string ownerId, List<AudioFileStorage> audioFiles, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            foreach (var audioFile in audioFiles)
            {
                await AddAudioFile(ownerId, audioFile.AudioType, audioFile, userInfo).ConfigureAwait(false);
            }
            //await context.Session.SaveChangesAsync().ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> RemoveAudioFile(string ownerId, AudioFileType type, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var bucket = GetAudioFilesBucket(context);
            var filter = Builders<GridFSFileInfo>.Filter.And(
                Builders<GridFSFileInfo>.Filter.Eq("metadata.ownerId", ownerId),
                Builders<GridFSFileInfo>.Filter.Eq("metadata.audioType", type));
            using var cursor = await bucket.FindAsync(filter).ConfigureAwait(false);
            var files = await cursor.ToListAsync().ConfigureAwait(false);
            foreach (var file in files)
            {
                await bucket.DeleteAsync(file.Id).ConfigureAwait(false);
            }
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult<int>> RemoveAudioFiles(string ownerId, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            IOperationResult<int> result = new GenericOperationResult<int>();
            var bucket = GetAudioFilesBucket(context);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.ownerId", ownerId);
            using var cursor = await bucket.FindAsync(filter).ConfigureAwait(false);
            var files = await cursor.ToListAsync().ConfigureAwait(false);
            foreach (var file in files)
            {
                await bucket.DeleteAsync(file.Id).ConfigureAwait(false);
            }
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    private static string GetAudioFileId(string ownerId, AudioFileType? type = null)
    {
        return $"audioFileStorages/{ownerId}{(type != null ? $"/{type}" : string.Empty)}";
    }

    #endregion

    #region generic operations

    public async Task<IOperationResult> AddObject<T>(T obj, IUserInformation userInfo) where T : class, IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Add))
            //    return NoPermissionError<T>(form, TopLevelPermission.Add);
            var result = new GenericOperationResult<T>();
            var col = context.GetCollection<T>();
            var validateRes = await ValidateAddOrUpdateInput(obj, context.AccessibleObjects<T>(false), false).ConfigureAwait(false);
            if (!validateRes.IsSuccess)
                return validateRes;
            DataPreProcessor.ProcessNewDbObject(obj, context, true);
            await col.InsertOneAsync(obj).ConfigureAwait(false);
            //MakeObjectAccessible(obj, context);
            //result.Result = obj.FromDbObject(true, false);
            result.Result = obj;
            result.IsSuccess = true;
            return result;
        }, userInfo).ConfigureAwait(false);
    }

    public async Task<IOperationResult<T>> GetObject<T>(string id, IUserInformation userInfo, bool includeDependencies = false,
        bool ignoreObjectAccessibility = false, bool ignorePermissions = false, bool includeCredentials = true)
        where T : class, IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
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

            var item = await col.Where(u => u.Id == id).FirstOrDefaultAsync().ConfigureAwait(false);
            if (item != null)
            {
                //result.Result = item.FromDbObject(includeDependencies, includeCredentials);
                result.Result = item;
                result.IsSuccess = true;
            }
            else
                return ObjectNotFoundError<T>(id);
            return result;
        }, userInfo).ConfigureAwait(false);
    }

    public IOperationResult<PhoneBookContact> GetContactWithManager(string id, IUserInformation userInfo)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult<PhoneBookContact>();
            var col = context.AccessibleObjects<PhoneBookContact>(false);

            col = col.Join(col, contact => contact.ManagerId, manager => manager.Id, (contact, manager) => new PhoneBookContact
                { Id = contact.Id, FirstName = contact.FirstName, ManagerId = contact.ManagerId, Manager = manager });

            //var myJoin = col.GroupJoin<PhoneBookContact, PhoneBookContact, string, PhoneBookContact>(col, contact => contact.SecretaryIds, secretary => secretary.Id, (contact, secretary) => new PhoneBookContact { });

            //col = col.Join(col, contact => contact.ManagerId, manager => manager.Id, (contact, manager) => new { PhoneBookContact = contact, Manager = manager });

            var contacts = context.GetCollection<BsonDocument>(GetCollectionName<PhoneBookContact>()).AsQueryable();

            //var myCol = contacts.Join(contacts, contact => contact["ManagerId"], manager => manager["_id"], new Func<BsonDocument, TInner, TResult> )


            var blub = context.GetCollection<PhoneBookContact>()
                .Aggregate()
                .Match(u => u.Id == id)
                .Lookup(GetCollectionName<PhoneBookContact>(), nameof(PhoneBookContact.ManagerId), "_id", nameof(PhoneBookContact.Manager))
                .Unwind(nameof(PhoneBookContact.Manager))
                .Lookup(GetCollectionName<PhoneBookContact>(), nameof(PhoneBookContact.SecretaryIds), "_id", nameof(PhoneBookContact.Secretary))
                .As<PhoneBookContact>()
                .FirstOrDefault();

            var myItem = context.GetCollection<PhoneBookContact>()
                .Aggregate()
                .Match(u => u.Id == id)
                .Lookup(GetCollectionName<PhoneBookContact>(), nameof(PhoneBookContact.ManagerId), "_id", nameof(PhoneBookContact.Manager))
                .Unwind(nameof(PhoneBookContact.Manager))
                .As<PhoneBookContact>()
                .FirstOrDefault();

            var json = myItem.ToJson();

            var myItems = context.GetCollection<PhoneBookContact>()
                .Aggregate()
                .Match(u => u.Id == id)
                .Lookup(GetCollectionName<PhoneBookContact>(), nameof(PhoneBookContact.SecretaryIds), "_id", nameof(PhoneBookContact.Secretary))
                .As<PhoneBookContact>()
                .FirstOrDefault();

            json = myItems.ToJson();

            //.Lookup<PhoneBookContact, PhoneBookContact>(GetCollectionName<PhoneBookContact>(), u => u.ManagerId, u => u.Id, null);

            //col.GroupJoin(col, contact => contact.Id, secretary => secretary.id)

            var test = col.SelectMany(r => r.Categories).ToList();

            //col = col.Join(col, contact => contact.SecretaryIds, secretary => secretary.Id, (contact, secretary) => contact);

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

    private PhoneBookContact ProjectManager(PhoneBookContact contact, PhoneBookContact manager)
    {
        contact.Manager = manager;
        return contact;
    }

    public IOperationResult<PhoneBookCategory> GetCategory(string id, IUserInformation userInfo, bool includeDependencies = true, 
        bool ignoreObjectAccessibility = false)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult<PhoneBookCategory>();
            var col = context.AccessibleObjects<PhoneBookCategory>(!ignoreObjectAccessibility);

            var phonebooks = context.AccessibleObjects<PhoneBook>(false);

            var myItem = context.GetCollection<PhoneBookCategory>()
                .Aggregate()
                .Match(u => u.Id == id)
                .Lookup(GetCollectionName<PhoneBook>(), nameof(PhoneBookCategory.PhoneBookIds), "_id", nameof(PhoneBookCategory.PhoneBooks))
                .As<PhoneBookCategory>()
                .FirstOrDefault();


            //col.Join(phonebooks, cat => cat.Id, pb => pb.Id, (cat, pb) => new PhoneBookCategory { Id = pb.Id, Name = pb.Name, PhoneBooks = } )


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
                return ObjectNotFoundError<PhoneBookCategory>(id);
            return result;
        }, userInfo);
    }

    public async Task<IOperationResult> UpdateObject<T>(T obj, IUserInformation userInfo, bool ignoreUpdateOfInternalFields = true)
        where T : class, IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Update))
            //    return NoPermissionError(form, TopLevelPermission.Update);
            var result = new GenericOperationResult();

            var existingItem = await context.AccessibleObjects<T>(true).Where(x => x.Id == obj.Id).FirstOrDefaultAsync().ConfigureAwait(false);
            if (existingItem == null)
                return ObjectNotFoundError<T>(obj.Id);
            var col = context.GetCollection<T>();
            var validateRes = await ValidateAddOrUpdateInput(obj, context.AccessibleObjects<T>(false), true).ConfigureAwait(false);
            if (!validateRes.IsSuccess)
                return validateRes;
            await DataPreProcessor.ProcessUpdatedDbObject(obj, context, false, []).ConfigureAwait(false);
            List<string> ignoreList = [nameof(IIdItem.Id)];
            ignoreList.AddRange(typeof(T).GetIgnorePropertiesForObjectUpdate(ignoreUpdateOfInternalFields, false));
            var differences = ObjectDiffUtils.ObjectDiff.GenerateObjectDiff(existingItem, obj, [.. ignoreList]);
            if (differences.Count <= 0) return GenericOperationResult.Success;
            var update = differences.GenerateUpdate(obj);
            if (update != null)
            {
                //ObjectDiffUtils.ObjectDiff.ApplyDiffData(existingItem, differences);
                var updateRes = await context.GetCollection<T>().UpdateOneAsync(u => u.Id == obj.Id, update).ConfigureAwait(false);
            }
            result.IsSuccess = true;
            return result;
        }, userInfo).ConfigureAwait(false);
    }

    public async Task<IOperationResult> UpdateObject<T>(string id, DeltaBaseObject<T> update, IUserInformation userInfo, bool ignoreUpdateOfInternalFields = true)
        where T : class, IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Update))
            //    return NoPermissionError<T>(form, TopLevelPermission.Update);

            var existingItem = await context.AccessibleObjects<T>(true).Where(x => x.Id == id).FirstOrDefaultAsync().ConfigureAwait(false);
            if (existingItem == null)
                return ObjectNotFoundError<T>(id);
            var col = context.GetCollection<T>();
            var validateRes = await ValidateAddOrUpdateInput(update.Data, context.AccessibleObjects<T>(false), true).ConfigureAwait(false);
            if (!validateRes.IsSuccess)
                return validateRes;
            await DataPreProcessor.ProcessUpdatedDbObject(update.Data, context, true, update.IncludedProperties).ConfigureAwait(false);
            return await UpdateObject(existingItem, update, col).ConfigureAwait(false);
        }, userInfo).ConfigureAwait(false);
    }

    private async Task<IOperationResult<T>> UpdateObject<T>(T existingItem, DeltaBaseObject<T> update, IMongoCollection<T> col)
        where T : class, IIdItem
    {
        GenericOperationResult<T> result = new()
        {
            //Result = existingItem.FromDbObject()
            Result = existingItem
        };
        var updateDefinition = existingItem.GenerateUpdate(update);
        if (updateDefinition != null)
        {
            //DeltaExtensions.ApplyDelta(existingItem, update);
            var updateRes = await col.UpdateOneAsync(u => u.Id == existingItem.Id, updateDefinition).ConfigureAwait(false);
        }
        result.IsSuccess = true;
        return result;
    }

    public async Task<IOperationResult<int>> BulkUpdateObject<T>(ExtendedMassUpdateParameters<T> parameters, IUserInformation userInfo)
        where T : class, IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Delete))
            //    return NoPermissionError<int>(form, TopLevelPermission.Delete);
            IOperationResult<int> result = new GenericOperationResult<int>();
            var col = context.GetCollection<T>(GetCollectionName<T>());
            var accessibleObjects = await context.AccessibleObjects<T>(true).Where(u => parameters.Ids.Contains(u.Id)).Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            parameters.Ids.RemoveAll(u => !accessibleObjects.Contains(u)); // ensure that only accessible objects can be updated
            var updateDefinition = parameters.GenerateUpdate();
            if (updateDefinition != null)
            {
                var res = await col.UpdateManyAsync(u => parameters.Ids.Contains(u.Id), updateDefinition).ConfigureAwait(false);
                result.Result = (int)res.ModifiedCount;
            }
            result.IsSuccess = true;
            return result;
        }, userInfo).ConfigureAwait(false);
    }

    public async Task<IOperationResult> DeleteObject<T>(string id, IUserInformation userInfo)
        where T : class, IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Delete))
            //    return NoPermissionError(form, TopLevelPermission.Delete);
            return await DeleteObject<T>(id, context).ConfigureAwait(false);
        }, userInfo).ConfigureAwait(false);
    }

    private async Task<IOperationResult> DeleteObject<T>(string id, MongoDatabaseContext context)
        where T : class, IIdItem
    {
        var result = new GenericOperationResult();
        var col = context.AccessibleObjects<T>(true);
        var existingItem = await col.Where(x => x.Id == id).FirstOrDefaultAsync().ConfigureAwait(false);
        if (existingItem == null)
            return ObjectNotFoundError<T>(id);
        await context.GetCollection<T>().DeleteOneAsync(u => u.Id == id).ConfigureAwait(false);
        result.IsSuccess = true;
        return result;
    }

    public async Task<IOperationResult<int>> BulkDelete<T>(List<string> ids, IUserInformation userInfo)
        where T : class, IIdItem
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.MassDelete))
            //    return NoPermissionError<int>(form, TopLevelPermission.MassDelete);
            IOperationResult<int> result = new GenericOperationResult<int>();
            var accessibleItems = await context.AccessibleObjects<T>(true).Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            ids.RemoveAll(u => !accessibleItems.Contains(u)); // enforce permissions
            var deleteRes = await context.GetCollection<T>().DeleteManyAsync(u => ids.Contains(u.Id)).ConfigureAwait(false);
            result.Result = (int)deleteRes.DeletedCount;
            result.Result = ids.Count;
            result.IsSuccess = true;
            return result;
        }, userInfo).ConfigureAwait(false);
    }

    public IOperationResult<SearchResults<T>> SearchObjects<T>(GenericSearchParameters parameters, IUserInformation userInfo) 
        where T: class, IIdItem
    {
        return PerformDatabaseOperation(context =>
        {
            //var form = GetGuiFormFromObject<T>();
            //if (!CheckPermission(context, form, TopLevelPermission.Show))
            //    return NoPermissionError<SearchResults<T>>(form, TopLevelPermission.Show);
            var result = new GenericOperationResult<SearchResults<T>>();
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
            else if (typeof(INamedItem).IsAssignableFrom(typeof(T)))
            {
                var myItems = query as IQueryable<INamedItem>;
                query = FilterHelpers.FilterByNameGeneric(query, parameters);
                //var filteredQuery = FilterHelpers.FilterByNameGeneric(myItems, parameters).Cast<T>();
                //query = filteredQuery as IMongoQueryable<T>;
                //var myItems = query as ILiteQueryable<INamedItem>;
                //var myQuery = FilterHelpers.FilterByName(myItems, parameters);
                //query = myQuery as ILiteQueryable<T>;
            }
            bool isSorted = false;
            query = FilterSearch(query, parameters);
            query = query.AppendGenericFilter(parameters, ref isSorted);

            //var queryable = query as IQueryable<T>;
            //query = FilterHelpers.AppendGenericFilter(queryable, parameters, ref isSorted) as IMongoQueryable<T>;
            if (!isSorted)
            {
                if (typeof(PhoneBookContact).IsAssignableFrom(typeof(T))) // phonebook contact
                {
                    query = query.SortBy(nameof(PhoneBookContact.LastName), true).ThenSortBy(nameof(PhoneBookContact.FirstName), true);
                }
                else
                    query = query.SortBy(nameof(INamedItem.Name), true); // default sorting by name
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

    internal static IMongoQueryable<T> FilterSearch<T>(IMongoQueryable<T> items, GenericSearchParameters parameters)
    {
        switch (parameters)
        {
            case PhoneBookCategorySearchParameters phoneBookCategorySearchParameters:
                {
                    if (items is not IMongoQueryable<PhoneBookCategory> query) return items;
                    if (phoneBookCategorySearchParameters.PhoneBookIds?.Count > 0)
                    {
                        query = query.Where(x => x.PhoneBookIds.Any(x => phoneBookCategorySearchParameters.PhoneBookIds.Contains(x)));
                    }
                    return query as IMongoQueryable<T>;
                }
            case PhoneBookContactSearchParameters phoneBookContactSearchParameters:
                {
                    if (items is not IMongoQueryable<PhoneBookContact> query) return items;
                    query = FilterHelpers.FilterPhoneBookContacts(query, phoneBookContactSearchParameters.Name, false, false);
                    query = FilterHelpers.FilterPhoneBookContacts(query, phoneBookContactSearchParameters.Query);
                    if (phoneBookContactSearchParameters.CategoryIds?.Count > 0)
                    {
                        query = query.Where(x => x.CategoryIds.Any(x => phoneBookContactSearchParameters.CategoryIds.Contains(x)));
                    }
                    if (phoneBookContactSearchParameters.ManagerIds?.Count> 0)
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

    internal static SearchResults<T> GeneratePagedListWithoutSorting<T>(IMongoQueryable<T> query, GenericSearchParameters parameters)
    {
        var result = new SearchResults<T>
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

    #endregion

    #region plugin configuration

    internal async Task<IOperationResult> AddPluginConfig(PluginConfiguration config, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var configs = context.GetCollection<PluginConfiguration>();
            await configs.InsertOneAsync(config).ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    internal async Task<IOperationResult> AddOrUpdatePluginConfig(Guid id, string config, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var configs = context.GetCollection(GetCollectionName<PluginConfiguration>());
            //config = config.Replace("\"Id\"", "\"_id\"");
            var jsonVal = BsonDocument.Parse(config);
            var filterX = Builders<BsonDocument>.Filter.Eq("_id", id);
            var res = await configs.ReplaceOneAsync(filterX, jsonVal, new ReplaceOptions { IsUpsert = true });
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    internal async Task<IOperationResult> UpdateConfig(PluginConfiguration config, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var configs = context.GetCollection<PluginConfiguration>();

            var existingConfig = await configs.Find(u => u.Id == config.Id).FirstOrDefaultAsync().ConfigureAwait(false);
            if (existingConfig != null)
            {
                List<string> ignoreList = [nameof(IIdItem.Id)];
                //ignoreList.AddRange(typeof(T).GetIgnorePropertiesForObjectUpdate(ignoreUpdateOfInternalFields, true));
                var differences = ObjectDiffUtils.ObjectDiff.GenerateObjectDiff(existingConfig, config, [.. ignoreList]);
                if (differences.Count <= 0) return GenericOperationResult.Success;
                var update = differences.GenerateUpdate(config);
                //ObjectDiffUtils.ObjectDiff.ApplyDiffData(existingItem, differences);
                if (update != null)
                {
                    var filter = Builders<PluginConfiguration>.Filter
                        .Eq(r => r.Id, config.Id);
                    var updateRes = await context.GetCollection<PluginConfiguration>().UpdateOneAsync(u => u.Id == config.Id, update).ConfigureAwait(false);
                }
            }
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    internal async Task<IOperationResult> DeleteConfig(Guid id, IUserInformation userInfo)
    {
        return await PerformDatabaseOperationAsync(async context =>
        {
            var result = new GenericOperationResult();
            var configs = context.GetCollection<PluginConfiguration>();
            var deletedDocument = await configs.FindOneAndDeleteAsync(u => u.Id == id).ConfigureAwait(false);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    internal IOperationResult<string> GetRawPluginConfiguration(Guid id, IUserInformation userInfo)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult<string>();
            var configs = context.GetCollection(GetCollectionName<PluginConfiguration>());
            var filterX = Builders<BsonDocument>.Filter.Eq("_id", id);
            //var filter = Builders<PluginConfiguration>.Filter.Eq(u => u.Id, id).ToBsonDocument();
            var existingConfig = configs.Find(filterX).FirstOrDefault();
            if (existingConfig != null)
            {
                //var dbId = existingConfig["_id"].AsGuid;
                result.Result = existingConfig.ToJson();
                result.IsSuccess = true;
            }
            else
                result.ErrorMessage = "config not found";
            return result;
        }, userInfo);
    }

    internal IOperationResult<PluginConfiguration> GetPluginConfig(Guid id, IUserInformation userInfo)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult<PluginConfiguration>();
            var configs = context.GetCollection(GetCollectionName<PluginConfiguration>());
            var filterX = Builders<BsonDocument>.Filter.Eq("_id", id);
            //var filter = Builders<PluginConfiguration>.Filter.Eq(u => u.Id, id).ToBsonDocument();
            var existingConfig = configs.Find(filterX).FirstOrDefault();
            if (existingConfig != null)
            {
                result.Result = BsonSerializer.Deserialize<PluginConfiguration>(existingConfig);
                result.IsSuccess = true;
            }
            else
                result.ErrorMessage = "config not found";
            return result;
        }, userInfo);
    }

    internal IOperationResult<List<PluginConfiguration>> GetAllPluginConfigurations(IUserInformation userInfo)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult<List<PluginConfiguration>> { Result = [] };
            var configs = context.GetCollection(GetCollectionName<PluginConfiguration>());
            var filter = Builders<BsonDocument>.Filter.Empty;
            var existingConfigs = configs.Find(filter).ToList();
            foreach (var existingConfig in existingConfigs)
                result.Result.Add(BsonSerializer.Deserialize<PluginConfiguration>(existingConfig));
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    #endregion

    #region phonebook configuration

    public IOperationResult AssociateWithCategory(string contactId, string categoryId, IUserInformation userInfo)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult();
            var contacts = context.AccessibleObjects<PhoneBookContact>(true);
            var contact = contacts.FirstOrDefault(u => u.Id == contactId);
            if (contact == null)
                return new GenericOperationResult { ErrorMessage = "Contact not found" };
            var categories = context.AccessibleObjects<PhoneBookCategory>(true);
            var category = categories.FirstOrDefault(u => u.Id == categoryId);
            if (category == null)
                return new GenericOperationResult { ErrorMessage = "Category not found" };
            if (contact.CategoryIds?.Contains(categoryId) == true) //.Any(x => x.Id == categoryId)) // already present
                return GenericOperationResult.Success;
            contact.Categories ??= [];
            contact.Categories.Add(new PhoneBookCategory { Id = categoryId });
            contact.CategoryIds ??= [];
            contact.CategoryIds.Add(categoryId);
            var updateDefinition = Builders<PhoneBookContact>.Update
                .Set(u => u.CategoryIds, contact.CategoryIds);
                //.Set(u => u.Categories, contact.Categories);
            var updateRes = context.GetCollection<PhoneBookContact>().UpdateOne(u => u.Id == contactId, updateDefinition);
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    public IOperationResult RemoveCategory(string contactId, string categoryId, IUserInformation userInfo)
    {
        return PerformDatabaseOperation(context =>
        {
            var result = new GenericOperationResult();
            var contacts = context.AccessibleObjects<PhoneBookContact>(true);
            var contact = contacts.FirstOrDefault(x => x.Id == contactId);
            if (contact == null)
                return new GenericOperationResult { ErrorMessage = "Contact not found" };
            if (contact.Categories.Any(x => x.Id == categoryId)) // present
            {
                contact.Categories.RemoveAll(u => u.Id == categoryId);
                contact.CategoryIds.Remove(categoryId);
                var updateDefinition = Builders<PhoneBookContact>.Update
                    .Set(u => u.CategoryIds, contact.CategoryIds)
                    .Set(u => u.Categories, contact.Categories);
                var updateRes = context.GetCollection<PhoneBookContact>().UpdateOne(u => u.Id == contactId, updateDefinition);
            } // not present => Ok as well
            result.IsSuccess = true;
            return result;
        }, userInfo);
    }

    #endregion

    #region validation

    private async Task<IOperationResult<T>> ValidateAddOrUpdateInput<T>(T apiObj, IMongoQueryable<T> set, bool isUpdate)
        where T : class, IIdItem
    {
        if (!isUpdate)
        {
            if (await set.Where(u => u.Id == apiObj.Id).FirstOrDefaultAsync() != null)
                return DuplicateObjectError<T>(apiObj.Id);
        }
        if (typeof(INamedIdItem).IsAssignableFrom(apiObj.GetType()))
        {
            if (set is IMongoQueryable<INamedIdItem> myItems && apiObj is INamedIdItem namedObject and not PhoneBookContact and not PhoneBookContactNumber)
            {
                if (!string.IsNullOrEmpty(namedObject.Name))
                {
                    var lowerCaseName = namedObject.Name.ToLower();
                    if (await myItems.AnyAsync(x => x.Name.ToLower() == lowerCaseName && x.Id != apiObj.Id))
                        return DuplicateNameError<T>(namedObject.Name);
                }
            }
        }
        return GenericOperationResult<T>.Success;
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
            var context = new MongoDatabaseContext(client.GetDatabase(databaseName), userInfo);
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
            var context = new MongoDatabaseContext(client.GetDatabase(databaseName), userInfo);
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
            var context = new MongoDatabaseContext(client.GetDatabase(databaseName), userInfo);
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
            var context = new MongoDatabaseContext(client.GetDatabase(databaseName), userInfo);
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

    protected IOperationResult<T> DuplicateObjectError<T>(string id) where T : class
    {
        return new GenericOperationResult<T>
        {
            //ErrorType = ErrorType.DuplicateObject,
            ErrorMessage = GetTranslatedString(DatabaseErrors.DuplicateObject, typeof(T).Name, id)
        };
    }

    protected IOperationResult<T> DuplicateNameError<T>(string name) where T : class
    {
        return new GenericOperationResult<T>
        {
            //ErrorType = ErrorType.DuplicateObject,
            ErrorMessage = GetTranslatedString(DatabaseErrors.DuplicateName, name)
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