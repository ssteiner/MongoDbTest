using GeneralTools.Extensions;
using GenericProvisioningLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mongo.Migration;
using Mongo.Migration.Startup;
using Mongo.Migration.Startup.DotNetCore;
using NoSqlModels;

namespace MongoDbTest;

internal class MongoDbTester
{
    private readonly IConfiguration configuration;
    private readonly IUserInformation userInfo = new GenericTenantUser { UserId = "admin" };
    private readonly MongoDbContext db;

    internal MongoDbTester()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json", false, true)
            .AddUserSecrets<MongoDbTester>(true, true)
            .Build(); 
        var connectionString = configuration.GetValue<string>("MongoDbConnectionString");
        var dbName = configuration.GetValue<string>("DatabaseName");
        db = new MongoDbContext(connectionString, dbName);
    }

    internal async Task RunTest()
    {
        var configRes = await db.Configure(userInfo).ConfigureAwait(false);
        if (!configRes.IsSuccess)
        {
            Log($"Unable to initialize database: {configRes}", 2);
            return;
        }

        //var pingRes = await db.CheckConnectivity(userInfo).ConfigureAwait(false);
        //var dbListRes = await db.GetDatabases(userInfo).ConfigureAwait(false);

        await RunAudioFileTest().ConfigureAwait(false);

        RunMigrationTests();

        //await RunPluginTest().ConfigureAwait(false);
        await RunPhonebookTests().ConfigureAwait(false);
    }

    internal async Task RunAudioFileTest()
    {
        var ownerId = "owner/1";
        List<AudioFileStorage> itemsToDelete = [];
        try
        {
            AudioFileStorage audioFile1, audioFile2, audioFile3;
            #region loading files
            var audioFileName1 = configuration.GetValue<string>("AudioFile1");
            if (File.Exists(audioFileName1))
            {
                audioFile1 = new AudioFileStorage
                {
                    FileName = Path.GetFileName(audioFileName1),
                    AudioType = AudioFileType.Audio1,
                    Contents = File.ReadAllBytes(audioFileName1)
                };
            }
            else
            {
                Log($"Unable to load audio file 1, unable to run test", 1);
                return;
            }
            var audioFileName2 = configuration.GetValue<string>("AudioFile2");
            if (File.Exists(audioFileName2))
            {
                audioFile2 = new AudioFileStorage
                {
                    FileName = Path.GetFileName(audioFileName2),
                    AudioType = AudioFileType.Audio2,
                    Contents = File.ReadAllBytes(audioFileName2)
                };
            }
            else
            {
                Log($"Unable to load audio file 2, unable to run test", 1);
                return;
            }
            var audioFileName3 = configuration.GetValue<string>("AudioFile3");
            if (File.Exists(audioFileName3))
            {
                audioFile3 = new AudioFileStorage
                {
                    FileName = Path.GetFileName(audioFileName3),
                    AudioType = AudioFileType.Audio3,
                    Contents = File.ReadAllBytes(audioFileName3)
                };
            }
            else
            {
                Log($"Unable to load audio file 3, unable to run test", 1);
                return;
            }
            #endregion

            var addRes = await db.AddAudioFile(ownerId, AudioFileType.Audio1, audioFile1, userInfo).ConfigureAwait(false);
            if (!addRes.IsSuccess)
            {
                Log($"Unable to add audio file 1: {addRes}", 2);
                return;
            }
            itemsToDelete.Add(audioFile1);
            var getRes = await db.GetAudioFile(ownerId, AudioFileType.Audio1, userInfo).ConfigureAwait(false);
            if (!getRes.IsSuccess)
            {
                Log($"Unable to extract audio file 2: {getRes}", 2);
                return;
            }
            if (!audioFile1.Contents.SequenceEqual(getRes.Result.Contents))
                Log($"Extracted audio file doesn't match local audio file", 2);
            else
                Log($"Validated correct storage of audio file", 4);

            var getAllRes = await db.GetAudioFiles(ownerId, userInfo).ConfigureAwait(false);
            if (!getAllRes.IsSuccess)
            {
                Log($"Unable to extract all audio files: {getAllRes}", 2);
                return;
            }
            if (getAllRes.Result.Count != 1)
            {
                Log($"Number of audio files for {ownerId} doesn't match. Expected: 1, actual: {getAllRes.Result.Count}", 2);
                return;
            }

            var bulkAddRes = await db.AddAudioFiles(ownerId, [audioFile2, audioFile3], userInfo).ConfigureAwait(false);
            if (!bulkAddRes.IsSuccess)
            {
                Log($"Unable to add multiple audio files for {ownerId}: {bulkAddRes}", 2);
                return;
            }
            itemsToDelete.Add(audioFile2);
            itemsToDelete.Add(audioFile3);

            getAllRes = await db.GetAudioFiles(ownerId, userInfo).ConfigureAwait(false);
            if (!getAllRes.IsSuccess)
            {
                Log($"Unable to extract all audio files: {getAllRes}", 2);
                return;
            }
            if (getAllRes.Result.Count != 3)
            {
                Log($"Number of audio files for {ownerId} doesn't match. Expected: 3, actual: {getAllRes.Result.Count}", 2);
                return;
            }
            else
            {
                var loadedFile2 = getAllRes.Result.FirstOrDefault(u => u.FileName == audioFile2.FileName);
                if (loadedFile2 == null)
                    Log($"File {audioFile2.FileName} not found", 2);
                else if (!audioFile2.Contents.SequenceEqual(loadedFile2.Contents))
                    Log($"Extracted audio file {loadedFile2.FileName} doesn't match local audio file", 2);
                else
                    Log($"Validated correct storage of audio file {loadedFile2.FileName}", 4);
                var loadedFile3 = getAllRes.Result.FirstOrDefault(u => u.FileName == audioFile3.FileName);
                if (loadedFile3 == null)
                    Log($"File {audioFile3.FileName} not found", 2);
                else if (!audioFile3.Contents.SequenceEqual(loadedFile3.Contents))
                    Log($"Extracted audio file {loadedFile3.FileName} doesn't match local audio file", 2);
                else
                    Log($"Validated correct storage of audio file {loadedFile3.FileName}", 4);
            }

            var deleteRes = await db.RemoveAudioFile(ownerId, AudioFileType.Audio1, userInfo).ConfigureAwait(false);
            if (!deleteRes.IsSuccess)
            {
                Log($"Unable to remove audio file {audioFile1.AudioType}: {deleteRes}", 2);
                return;
            }
            itemsToDelete.Remove(audioFile1);
            Log($"Successfully deleted audio file {audioFile1.AudioType}", 4);

            getRes = await db.GetAudioFile(ownerId, AudioFileType.Audio1, userInfo).ConfigureAwait(false);
            if (!getRes.IsSuccess)
                Log($"Audio file {audioFile1.AudioType} is no longer present", 4);
            else
                Log($"Audio file {audioFile1.AudioType} is still present after delete", 2);

            var bulkDeleteRes = await db.RemoveAudioFiles(ownerId, userInfo).ConfigureAwait(false);
            if (!bulkDeleteRes.IsSuccess)
            {
                Log($"Unable to remove all audio files from {ownerId}: {bulkDeleteRes}", 2);
                return;
            }
            itemsToDelete.Remove(audioFile2);
            itemsToDelete.Remove(audioFile3);

            getAllRes = await db.GetAudioFiles(ownerId, userInfo).ConfigureAwait(false);
            if (!getAllRes.IsSuccess)
            {
                Log($"Unable to extract all audio files: {getAllRes}", 2);
                return;
            }
            if (getAllRes.Result.Count != 0)
            {
                Log($"Number of audio files for {ownerId} doesn't match. Expected: 0, actual: {getAllRes.Result.Count}", 2);
                return;
            }
        }
        finally
        {
            foreach (var audioFile in itemsToDelete)
            {
                var deleteRes = await db.RemoveAudioFile(ownerId, audioFile.AudioType, userInfo).ConfigureAwait(false);
                if (deleteRes.IsSuccess)
                    Log($"Successfully removed audio file {audioFile.AudioType} from {ownerId}", 4);
                else
                    Log($"Unable to remove audio file {audioFile.AudioType} from {ownerId}", 2);
            }
        }
    }

    internal void RunMigrationTests()
    {
        MongoMigrationSettings options = new()
        {
            ConnectionString = configuration.GetValue<string>("MongoDbConnectionString"),
            Database = configuration.GetValue<string>("DatabaseName"), 
            DatabaseMigrationVersion = new Mongo.Migration.Documents.DocumentVersion("0.0.0")
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddMigration(options);

        using IHost host = builder.Build();

        var migration = host.Services.GetRequiredService<IMongoMigration>();
        migration.Run();

        //host.Run();
    }

    internal async Task RunPluginTest()
    {
        List<Guid> rollbackObjects = [];
        var pluginConfig = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "some plugin",
            SpecialConfig = new SpecialConfiguration
            {
                Value1 = "val1",
                Value2 = "val2"
            }
        };
        var pluginConfig2 = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "another",
            SpecialConfig = new SpecialConfiguration
            {
                Value1 = "val1",
                Value2 = "val2"
            }
        };

        var pluginConfig3 = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "third",
            SpecialConfig = new SpecialConfiguration
            {
                Value1 = "valx1",
                Value2 = "valx2"
            }
        };

        var rawConfig = pluginConfig3.ToJsonString();

        try
        {
            var addRes = await db.AddPluginConfig(pluginConfig, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
            {
                rollbackObjects.Add(pluginConfig.Id);
                var getRes = db.GetPluginConfig(pluginConfig.Id, userInfo);
                var rawRes = db.GetRawPluginConfiguration(pluginConfig.Id, userInfo);
            }

            addRes = await db.AddPluginConfig(pluginConfig2, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
            {
                rollbackObjects.Add(pluginConfig2.Id);
                var getRes = db.GetPluginConfig(pluginConfig2.Id, userInfo);
                var rawRes = db.GetRawPluginConfiguration(pluginConfig2.Id, userInfo);
            }

            addRes = await db.AddOrUpdatePluginConfig(pluginConfig3.Id, rawConfig, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
            {
                rollbackObjects.Add(pluginConfig3.Id);
                var getRes = db.GetPluginConfig(pluginConfig3.Id, userInfo);
                var rawRes = db.GetRawPluginConfiguration(pluginConfig3.Id, userInfo);
            }


            var allConfigs = db.GetAllPluginConfigurations(userInfo);

            pluginConfig3.Name = "another name";
            pluginConfig3.SpecialConfig.Value2 = "vaaaaalllllluuuuuueeeee";
            rawConfig = pluginConfig3.ToJsonString();

            var rawUpdateRes = await db.AddOrUpdatePluginConfig(pluginConfig3.Id, rawConfig, userInfo).ConfigureAwait(false);
            if (rawUpdateRes.IsSuccess)
            {
                var getRes = db.GetPluginConfig(pluginConfig3.Id, userInfo);
                var rawRes = db.GetRawPluginConfiguration(pluginConfig3.Id, userInfo);
            }


            pluginConfig.Name = "gugu";
            var updateRes = await db.UpdateConfig(pluginConfig, userInfo).ConfigureAwait(false);
            if (updateRes.IsSuccess)
            {
                var getRes = db.GetPluginConfig(pluginConfig.Id, userInfo);
                if (getRes.IsSuccess)
                {
                    if (getRes.Result.Name == pluginConfig.Name)
                        Log($"Successfully updated plugin config {nameof(pluginConfig)}", 4);
                    else
                        Log($"Modified values of plugin config {nameof(pluginConfig)} could not be written", 2);
                }
                else
                    Log($"Unable to update plugin config of {nameof(pluginConfig)}: {getRes}", 2);
            }

            pluginConfig2.Name = "habla bimbam";
            pluginConfig2.SpecialConfig.Value2 = "gaga";

            updateRes = await db.UpdateConfig(pluginConfig2, userInfo).ConfigureAwait(false);
            if (updateRes.IsSuccess)
            {
                var getRes = db.GetPluginConfig(pluginConfig2.Id, userInfo);
            }

            var deleteRes = await db.DeleteConfig(pluginConfig.Id, userInfo).ConfigureAwait(false);
            if (deleteRes.IsSuccess)
                rollbackObjects.Remove(pluginConfig.Id);
            deleteRes = await db.DeleteConfig(pluginConfig2.Id, userInfo).ConfigureAwait(false);
            if (deleteRes.IsSuccess)
                rollbackObjects.Remove(pluginConfig2.Id);
            deleteRes = await db.DeleteConfig(pluginConfig3.Id, userInfo).ConfigureAwait(false);
            if (deleteRes.IsSuccess)
                rollbackObjects.Remove(pluginConfig3.Id);
        }
        catch (Exception e)
        {
            Log($"Unhandled exception in plugin test: {e.Message}", 2);
        }
        finally
        {
            foreach (var id in rollbackObjects)
            {
                var deleteRes = await db.DeleteConfig(id, userInfo).ConfigureAwait(false);
                if (deleteRes.IsSuccess)
                    Log($"Successfully removed plugin config {id}", 4);
                else
                    Log($"Unable to remove plugin config {id}: {deleteRes}", 2);
            }
        }
    }

    internal async Task RunPhonebookTests()
    {
        List<IIdItem> rollbackObjects = [];
        PhoneBook pb1 = new()
        {
            //Id = Guid.NewGuid(),
            Name = "Phonebook 1"
        };

        PhoneBook pb2 = new()
        {
            //Id = Guid.NewGuid(),
            Name = "Phonebook 2"
        };

        PhoneBookCategory category1 = new()
        {
            //Id = Guid.NewGuid(),
            Name = "Category1",
            SubProp = new TestObject { StringProp = "guguseli", IntProp = 10 }
        };
        PhoneBookCategory category2 = new()
        {
            //Id = Guid.NewGuid(),
            Name = "Category2"
        };
        PhoneBookCategory category3 = new()
        {
            //Id = Guid.NewGuid(),
            Name = "special Subcategory3"
        };

        PhoneBookContact contact1 = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "contact 1",
            LastName = "Grossmeister",
            Location = "Bern",
            Numbers = [
                    new PhoneBookContactNumber { /*Id = ObjectId.GenerateNewId().ToString(),*/ Number = "+41587770001", Type = NumberType.Office },
                    new PhoneBookContactNumber { /*Id = ObjectId.GenerateNewId().ToString(),*/ Number = "+41767770002", Type = NumberType.Mobile }
                    ]
        };
        PhoneBookContact contact2 = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "contact 2",
            LastName = "Meister",
            Location = "Bern",
            Numbers = [
                new PhoneBookContactNumber { /*Id = ObjectId.GenerateNewId().ToString(),*/  Number = "+41587770003", Type = NumberType.Office },
                new PhoneBookContactNumber { /*Id = ObjectId.GenerateNewId().ToString(),*/ Number = "+41767770004", Type = NumberType.Mobile }
                ]
        };
        PhoneBookContact manager = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "manager",
            LastName = "Meier",
            Location = "Züri",
            Numbers = [
                new PhoneBookContactNumber { /*Id = ObjectId.GenerateNewId().ToString(),*/ Number = "+41587770005", Type = NumberType.Office },
            ]
        };

        PhoneBookContact secretary = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "secretary", 
            LastName = "Müller",
            Location = "Züri",
            Numbers = [
                new PhoneBookContactNumber { /*Id = ObjectId.GenerateNewId().ToString(),*/  Number = "+41587770006", Type = NumberType.Office },
            ]
        };


        try
        {
            var addRes = await db.AddObject(pb1, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(pb1);

            addRes = await db.AddObject(pb2, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(pb2);
            category1.PhoneBookIds = [pb1.Id];
            category2.PhoneBookIds = [pb2.Id, pb1.Id];

            addRes = await db.AddObject(category1, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
            {
                var getRes = await db.GetObject<PhoneBookCategory>(category1.Id, userInfo).ConfigureAwait(false);
                if (getRes.IsSuccess)
                    category1 = getRes.Result;
                rollbackObjects.Add(category1);

                category1.Name = "category11";

                var catUpdateRes = await db.UpdateObject(category1, userInfo).ConfigureAwait(false);
                if (catUpdateRes.IsSuccess)
                {
                    var catRes = await db.GetObject<PhoneBookCategory>(category1.Id, userInfo).ConfigureAwait(false);
                    if (catRes.IsSuccess)
                    {
                        if (catRes.Result.Name == category1.Name)
                        {
                            Log($"Validated full update of category {category1.Id}", 4);
                            category1 = catRes.Result;
                        }
                        else
                            Log($"Full update of category {category1} didn't take, expected: {category1.Name}, received: {catRes.Result.Name}", 2);
                    }
                }

                DeltaBaseObject<PhoneBookCategory> deltaUpdate = new()
                {
                    IncludedProperties = [nameof(PhoneBookCategory.Name), nameof(PhoneBookCategory.SubProp)],
                    IncludedPropertiesIncludingPath = [nameof(PhoneBookCategory.Name), $"{nameof(PhoneBookCategory.SubProp)}.{nameof(TestObject.IntProp)}"],
                    Data = new PhoneBookCategory { Name = "updated category 1", SubProp = new TestObject { IntProp = 5 } }
                };
                catUpdateRes = await db.UpdateObject(category1.Id, deltaUpdate, userInfo).ConfigureAwait(false);
                if (catUpdateRes.IsSuccess)
                {
                    var catRes = await db.GetObject<PhoneBookCategory>(category1.Id, userInfo).ConfigureAwait(false);
                    if (catRes.IsSuccess)
                    {
                        if (catRes.Result.Name == deltaUpdate.Data.Name)
                        {
                            Log($"Validated delta name update of category {category1.Id}", 4);
                            category1 = catRes.Result;
                        }
                        else
                            Log($"Delta name update of category {category1} didn't take, expected: {deltaUpdate.Data.Name}, received: {catRes.Result.Name}", 2);
                        if (catRes.Result.SubProp?.IntProp == 5)
                        {
                            Log($"Validated subproperty delta update of category {category1.Id}", 4);
                            category1 = catRes.Result;
                        }
                        else
                            Log($"Delta update of category {category1} didn't take, expected: {deltaUpdate.Data.SubProp.IntProp}, received: {catRes.Result.SubProp?.IntProp}", 2);
                    }
                }   
            }

            addRes = await db.AddObject(category2, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(category2);
            var categoryRes = await db.GetObject<PhoneBookCategory>(category2.Id, userInfo).ConfigureAwait(false);

            addRes = await db.AddObject(category3, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(category3);
            categoryRes = await db.GetObject<PhoneBookCategory>(category3.Id, userInfo).ConfigureAwait(false);

            if (addRes.IsSuccess)
            {
                var newDescription = "hello world";
                ExtendedMassUpdateParameters<PhoneBookCategory> massUpdates = new()
                {
                    Ids = [category1.Id, category2.Id, category3.Id],
                    IncludedProperties = [nameof(PhoneBookCategory.Description)],
                    TemplateObject = new PhoneBookCategory { Description = newDescription },
                    Values = new Dictionary<string, object> { { nameof(PhoneBookCategory.Description), newDescription } }
                };

                var massUpdateRes = await db.BulkUpdateObject(massUpdates, userInfo).ConfigureAwait(false);
                if (massUpdateRes.IsSuccess)
                {
                    if (massUpdateRes.Result != massUpdates.Ids.Count)
                        Log($"Number of updated descriptions doesn't match. Number expected: {massUpdates.Ids.Count}, updated: {massUpdateRes.Result}", 2);
                    else
                        Log("Number of updated descriptions matches", 4);
                    var checkRes = await db.GetObject<PhoneBookCategory>(category1.Id, userInfo).ConfigureAwait(false);
                    if (checkRes.IsSuccess)
                    {
                        if (checkRes.Result.Description == newDescription)
                            Log($"Successfully validated bulk update of {category1.GetType().Name}", 4);
                        else
                            Log($"Unable to validate bulk update of {category1.GetType().Name}", 2);
                    }
                }
            }

            addRes = await db.AddObject(manager, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(manager);
            addRes = await db.AddObject(secretary, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(secretary);

            contact1.ManagerId = manager.Id;
            contact1.SecretaryIds = [secretary.Id, manager.Id];

            addRes = await db.AddObject(contact1, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(contact1);
            addRes = await db.AddObject(contact2, userInfo).ConfigureAwait(false);
            if (addRes.IsSuccess)
                rollbackObjects.Add(contact2);

            var getFullRes = db.GetContactWithManager(contact1.Id, userInfo);

            var getFullCategoryRes = db.GetCategory(category2.Id, userInfo, true);

            var contactRes = await db.GetObject<PhoneBookContact>(contact1.Id, userInfo).ConfigureAwait(false);
            contactRes = await db.GetObject<PhoneBookContact>(contact2.Id, userInfo).ConfigureAwait(false);

            var associateRes = db.AssociateWithCategory(contact1.Id, category1.Id, userInfo);
            contactRes = await db.GetObject<PhoneBookContact>(contact1.Id, userInfo, true).ConfigureAwait(false);
            categoryRes = await db.GetObject<PhoneBookCategory>(category1.Id, userInfo, true).ConfigureAwait(false);

            associateRes = db.AssociateWithCategory(contact1.Id, category2.Id, userInfo);
            associateRes = db.AssociateWithCategory(contact2.Id, category2.Id, userInfo);

            contactRes = await db.GetObject<PhoneBookContact>(contact1.Id, userInfo, true).ConfigureAwait(false);
            contactRes = await db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true).ConfigureAwait(false);

            await CategorySearchTest(category1, category2, pb1, pb2).ConfigureAwait(false);

            await ContactSearchTest(category1, category2, contact1, contact2, manager, secretary).ConfigureAwait(false);

            // try to add with just an Id
            {
                var tempContact2 = contactRes.Result;
                tempContact2.CategoryIds ??= [];
                tempContact2.CategoryIds.Add(category1.Id);
                var updateRes = await db.UpdateObject(tempContact2, userInfo).ConfigureAwait(false);
                if (updateRes.IsSuccess)
                {
                    contactRes = await db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true).ConfigureAwait(false);
                    if (contactRes.IsSuccess)
                    {
                        if (contactRes.Result.CategoryIds?.Contains(category1.Id) == true)
                            Log($"Successfully validated adding category {category1.Name} to contact {tempContact2.Id}", 4);
                        else
                            Log($"Category {category1.Name} was not added to contact {tempContact2.Id}", 2);
                    }
                    else
                        Log($"Unable to extract contact {contact2.Id}: {contactRes}", 2);
                }

                tempContact2 = contactRes.Result;
                //tempContact2.Categories.RemoveAll(u => u.Id == category1.Id);
                tempContact2.CategoryIds?.Remove(category1.Id);

                updateRes = await db.UpdateObject(tempContact2, userInfo).ConfigureAwait(false);
                if (updateRes.IsSuccess)
                {
                    contactRes = await db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true).ConfigureAwait(false);
                    if (contactRes.IsSuccess)
                    {
                        if (contactRes.Result.CategoryIds?.Contains(category1.Id) == true)
                            Log($"Category {category1.Name} was not removed from contact {tempContact2.Id}", 2);
                        else
                            Log($"Category {category1.Name} was successfully removed from contact {tempContact2.Id}", 4);
                    }
                    else
                        Log($"Unable to extract contact {contact2.Id}: {contactRes}", 2);
                }
                contactRes = await db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true).ConfigureAwait(false);
            }

            List<string> idsToDelete = [category1.Id, category2.Id];
            var bulkDeleteRes = await db.BulkDelete<PhoneBookCategory>(idsToDelete, userInfo).ConfigureAwait(false);
            if (bulkDeleteRes.IsSuccess)
            {
                if (bulkDeleteRes.Result == 2)
                {
                    Log($"Successfully validated bulk delete of PhoneBookCategory", 4);
                    rollbackObjects.RemoveAll(u => u.Id == category1.Id);
                    rollbackObjects.RemoveAll(u => u.Id == category2.Id);
                }
                else
                    Log($"Unable to validate bulk delete of PhoneBookCategory. expected to delete 2, actually deleted: {bulkDeleteRes.Result}", 2);
            }
            else
                Log($"Bulk delete of PhoneBookCategory failed: {bulkDeleteRes}", 2);
        }
        catch (Exception e)
        {
            Log($"OOps, something went wrong in the test: {e.Message}", 2);
        }
        finally
        {
            foreach (var rollbackObject in rollbackObjects)
            {
                IOperationResult deleteRes = null;
                if (rollbackObject is PhoneBook pb)
                {
                    deleteRes = await db.DeleteObject<PhoneBook>(pb.Id, userInfo).ConfigureAwait(false);
                }
                else if (rollbackObject is PhoneBookCategory pbCat)
                {
                    deleteRes = await db.DeleteObject<PhoneBookCategory>(pbCat.Id, userInfo).ConfigureAwait(false);
                }
                else if (rollbackObject is PhoneBookContact pbContact)
                {
                    deleteRes = await db.DeleteObject<PhoneBookContact>(pbContact.Id, userInfo).ConfigureAwait(false);
                }
                if (deleteRes.IsSuccess)
                    Log($"Successfully deleted {rollbackObject.GetType().Name} {rollbackObject.Id}", 4);
                else
                    Log($"Unable to delete {rollbackObject.GetType().Name} {rollbackObject.Id}: {deleteRes}", 2);
            }
        }
    }

    private async Task ContactSearchTest(PhoneBookCategory category1, PhoneBookCategory category2, PhoneBookContact contact1, PhoneBookContact contact2,
        PhoneBookContact manager, PhoneBookContact secretary)
    {
        PhoneBookContactSearchParameters searchParameters = new() // everything starting with 'contact', order descending
        {
            Query = "contact", 
            SortAscending = false, 
            SortBy = nameof(PhoneBookContact.FirstName)
        };
        var searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count < 2)
            {
                Log($"Not all contacts, expected: 2, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != contact2.Id)
                    Log($"Sort didn't work, first item isn't {contact2.FirstName}", 2);
            }
        }

        searchParameters = new() // all, sort ascending
        {
            Query = null,
            SortAscending = true,
            SortBy = nameof(PhoneBookContact.FirstName)
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count < 4)
            {
                Log($"Not all contacts, expected: 4, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != contact1.Id)
                    Log($"Sort didn't work, first item isn't {secretary.FirstName}", 2);
            }
        }

        searchParameters = new()
        {
            Query = "%contact",
            Location = "Bern"
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 2)
            {
                Log($"Not all contacts, expected: 2, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (!searchRes.Result.Results.All(x => x.Location == searchParameters.Location))
                    Log($"Search didn't work, not all results have location {searchParameters.Location}", 2);
            }
        }

        searchParameters = new() // < 2 numbers
        {
            SearchParameters = [new GenericSearchParameter
            {
                FieldName = nameof(PhoneBookContact.NumberOfTelephoneNumbers),
                FieldOperator = ComparisonOperator.LessThan, 
                FieldValue = 2
            }]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 2)
            {
                Log($"Not all contacts, expected: 2, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (!searchRes.Result.Results.All(x => x.NumberOfTelephoneNumbers < 2))
                    Log($"Not all contacts have less than 2 numbers", 2);
            }
        }

        searchParameters = new() // >= 2 numbers
        {
            SearchParameters = [new GenericSearchParameter
            {
                FieldName = nameof(PhoneBookContact.NumberOfTelephoneNumbers),
                FieldOperator = ComparisonOperator.MoreThanOrEqualTo,
                FieldValue = 2
            }]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 2)
            {
                Log($"Not all contacts, expected: 2, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (!searchRes.Result.Results.All(x => x.NumberOfTelephoneNumbers >= 2))
                    Log($"Not all contacts returned have >= 2 numbers", 2);
            }
        }

        string number = "+41587770001";
        searchParameters = new()
        {
            Query = number
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all contacts, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (!searchRes.Result.Results[0].Numbers.Any(x => x.Number == number))
                    Log($"Search didn't pick the contact with number {number}", 2);
            }
        }

        searchParameters = new()
        {
            ManagerIds = [manager.Id]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all contacts, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != contact1.Id)
                    Log($"Sort didn't work, first item isn't {contact1.FirstName}", 2);
            }
        }

        searchParameters = new()
        {
            SecretaryIds = [secretary.Id],
            ManagerIds = [manager.Id]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all contacts, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != contact1.Id)
                    Log($"Sort didn't work, first item isn't {contact1.FirstName}", 2);
            }
        }

        searchParameters = new PhoneBookContactSearchParameters()
        {
            SearchParameters =
            [ // gets us contact 1
                new() { FieldName = nameof(PhoneBookContact.ManagerId), FieldOperator = ComparisonOperator.IsNotEmpty}
            ]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all contacts, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != contact1.Id)
                    Log($"Sort didn't work, first item isn't {contact1.FirstName}", 2);
            }
        }

        searchParameters = new PhoneBookContactSearchParameters()
        {
            SearchParameters =
            [ // gets us contact 1
                new() { FieldName = nameof(PhoneBookContact.ManagerId), FieldOperator = ComparisonOperator.IsEmpty}
            ]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 3)
            {
                Log($"Not all contacts, expected: 3, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (!searchRes.Result.Results.All(x => x.ManagerId == null))
                    Log($"Sort didn't work, not all items have an empty ManagerId", 2);
            }
        }

        searchParameters = new PhoneBookContactSearchParameters()
        {
            SearchParameters =
            [ // gets us contact 1
                new() { 
                    FieldName = nameof(PhoneBookContact.SecretaryIds),
                    FieldValue = new Newtonsoft.Json.Linq.JArray(new string[] { secretary.Id.ToString() }),
                    FieldOperator = ComparisonOperator.Contains
                }
            ]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all contacts, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != contact1.Id)
                    Log($"Sort didn't work, first item isn't {contact1.FirstName}", 2);
            }
        }

        searchParameters = new PhoneBookContactSearchParameters()
        {
            SearchParameters =
            [ // gets us contact 1
                new() { FieldName = nameof(PhoneBookContact.ManagerId), FieldOperator = ComparisonOperator.EqualTo, FieldValue = manager.Id }
            ]
        };
        searchRes = db.SearchObjects<PhoneBookContact>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all contacts, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != contact1.Id)
                    Log($"Sort didn't work, first item isn't {contact1.FirstName}", 2);
            }
        }
    }

    private async Task CategorySearchTest(PhoneBookCategory category1, PhoneBookCategory category2, PhoneBook pb1, PhoneBook pb2)
    {
        PhoneBookCategorySearchParameters searchParameters = new()
        {
            Query = "%category", 
            SortAscending = false, 
            SortBy = nameof(PhoneBookCategory.Name)
        };

        var searchRes = db.SearchObjects<PhoneBookCategory>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 3)
            {
                Log($"Not all categories found, expected: 2, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != category1.Id)
                    Log($"Sort didn't work, first item isn't {category2.Name}", 2);
            }
        }
        else
            Log($"searching for categories failed: {searchRes}", 2);

        searchParameters = new PhoneBookCategorySearchParameters { Name = "category2" }; // case insensitive expected
        searchRes = db.SearchObjects<PhoneBookCategory>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Category 2 not found, received {searchRes.Result.Results.Count}", 2);
            }
        }
        else
            Log($"searching for categories failed: {searchRes}", 2);


        searchParameters = new PhoneBookCategorySearchParameters { Name = "%1" }; // case insensitive expected
        searchRes = db.SearchObjects<PhoneBookCategory>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (!searchRes.Result.Results.Any(x => x.Id == category1.Id))
            {
                Log($"result does not include {category1.Name}", 2);
            }
            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all categories found, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
        }
        else
            Log($"searching for categories failed: {searchRes}", 2);

        searchParameters = new PhoneBookCategorySearchParameters { PhoneBookIds = [pb1.Id] };
        searchRes = db.SearchObjects<PhoneBookCategory>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (!searchRes.Result.Results.Any(x => x.Id == category1.Id))
            {
                Log($"result does not include {category1.Name}", 2);
            }
            if (searchRes.Result.Results.Count != 2)
            {
                Log($"Not all categories found, expected: 2, received {searchRes.Result.Results.Count}", 2);
            }
        }
        else
            Log($"searching for categories failed: {searchRes}", 2);

        searchParameters = new PhoneBookCategorySearchParameters
        {
            SearchParameters = [new GenericSearchParameter
            {
                FieldName = nameof(PhoneBookCategory.PhoneBookIds),
                FieldOperator = ComparisonOperator.Contains,
                FieldValue = new Newtonsoft.Json.Linq.JArray(new string[] { pb1.Id, pb2.Id }),
            }]
        };
        searchRes = db.SearchObjects<PhoneBookCategory>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            var itemsWithoutPhonebooks = searchRes.Result.Results.Where(x => !x.PhoneBookIds.Contains(pb1.Id) && !x.PhoneBookIds.Contains(pb2.Id)).ToList();
            if (itemsWithoutPhonebooks.Count > 0)
            {
                Log($"result includes elements that are not part of neither phonebook1 nor phonebook2: {string.Join(",", itemsWithoutPhonebooks.Select(x => x.Name))}", 2);
            }
            else
                Log($"All categories found that are in either phonebook1 or phonebook2", 4);
        }
        else
            Log($"searching for categories failed: {searchRes}", 2);

        searchParameters = new PhoneBookCategorySearchParameters
        {
            SearchParameters = [new GenericSearchParameter
            {
                FieldName = nameof(PhoneBookCategory.Name),
                FieldOperator = ComparisonOperator.EndsWith,
                FieldValue = "2"
            }]
        };
        searchRes = db.SearchObjects<PhoneBookCategory>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (!searchRes.Result.Results.Any(x => x.Id == category2.Id))
            {
                Log($"result does not include {category1.Name}", 2);
            }

            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all categories found, expected: 1, received {searchRes.Result.Results.Count}", 2);
            }
        }
        else
            Log($"searching for categories failed: {searchRes}", 2);
        searchParameters = new PhoneBookCategorySearchParameters
        {
            SearchParameters = [new GenericSearchParameter
            {
                FieldName = nameof(PhoneBookCategory.Name),
                FieldOperator = ComparisonOperator.Contains,
                FieldValue = "category"
            }],
            SortAscending = false,
            SortBy = nameof(PhoneBookCategory.Name)
        };
        searchRes = db.SearchObjects<PhoneBookCategory>(searchParameters, userInfo);
        if (searchRes.IsSuccess)
        {
            if (searchRes.Result.Results.Count != 3)
            {
                Log($"Not all categories found, expected: 3, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != category1.Id)
                    Log($"Sort didn't work, first item isn't {category2.Name}", 2);
            }
        }
        else
            Log($"searching for categories failed: {searchRes}", 2);
    }

    private void Log(string message, int severity)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}|{severity}|{message}");
    }
}
