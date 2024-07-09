using GeneralTools.Extensions;
using GenericProvisioningLib;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
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

        //await RunPluginTest().ConfigureAwait(false);
        await RunPhonebookTests().ConfigureAwait(false);
    }

    internal async Task RunPluginTest()
    {
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

        var addRes = await db.AddPluginConfig(pluginConfig, userInfo).ConfigureAwait(false);
        if (addRes.IsSuccess)
        {
            var getRes = db.GetPluginConfig(pluginConfig.Id, userInfo);
            var rawRes = db.GetRawPluginConfiguration(pluginConfig.Id, userInfo);
        }

        addRes = await db.AddPluginConfig(pluginConfig2, userInfo).ConfigureAwait(false);
        if (addRes.IsSuccess)
        {
            var getRes = db.GetPluginConfig(pluginConfig2.Id, userInfo);
            var rawRes = db.GetRawPluginConfiguration(pluginConfig2.Id, userInfo);
        }

        addRes = await db.AddOrUpdatePluginConfig(pluginConfig3.Id, rawConfig, userInfo).ConfigureAwait(false);
        if (addRes.IsSuccess)
        {
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
        deleteRes = await db.DeleteConfig(pluginConfig2.Id, userInfo).ConfigureAwait(false);
        deleteRes = await db.DeleteConfig(pluginConfig3.Id, userInfo).ConfigureAwait(false);
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
            Name = "Category1"
        };
        PhoneBookCategory category2 = new()
        {
            //Id = Guid.NewGuid(),
            Name = "Category2"
        };

        PhoneBookContact contact1 = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "contact 1",
            LastName = "Grossmeister",
            Location = "Bern",
            Numbers = [
                    new PhoneBookContactNumber { /*Id = Guid.NewGuid(), */ Id = ObjectId.GenerateNewId().ToString(), Number = "+41587770001", Type = NumberType.Office },
                    new PhoneBookContactNumber { /*Id = Guid.NewGuid(), */ Id = ObjectId.GenerateNewId().ToString(), Number = "+41767770002", Type = NumberType.Mobile }
                    ]
        };
        PhoneBookContact contact2 = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "contact 2",
            LastName = "Meister",
            Location = "Bern",
            Numbers = [
                new PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(),  Number = "+41587770003", Type = NumberType.Office },
                new PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(), Number = "+41767770004", Type = NumberType.Mobile }
                ]
        };
        PhoneBookContact manager = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "manager",
            LastName = "Meier",
            Location = "Züri",
            Numbers = [
                new PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(), Number = "+41587770005", Type = NumberType.Office },
            ]
        };

        PhoneBookContact secretary = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "secretary", 
            LastName = "Müller",
            Location = "Züri",
            Numbers = [
                new PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(),  Number = "+41587770006", Type = NumberType.Office },
            ]
        };


        try
        {
            var addRes = db.AddObject(pb1, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(pb1);

            addRes = db.AddObject(pb2, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(pb2);
            category1.PhoneBookIds = [pb1.Id];
            category2.PhoneBookIds = [pb2.Id, pb1.Id];

            addRes = db.AddObject(category1, userInfo);
            if (addRes.IsSuccess)
            {
                rollbackObjects.Add(category1);

                category1.Name = "category11";

                var catUpdateRes = db.UpdateObject(category1, userInfo);
                if (catUpdateRes.IsSuccess)
                {
                    var catRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo);
                    if (catRes.IsSuccess)
                    {
                        if (catRes.Result.Name == category1.Name)
                        {
                            Log($"Validated delta update of category {category1.Id}", 4);
                            category1 = catRes.Result;
                        }
                        else
                            Log($"Delta update of category {category1} didn't take, expected: {category1.Name}, received: {catRes.Result.Name}", 2);
                    }
                }

                DeltaBaseObject<PhoneBookCategory> deltaUpdate = new()
                {
                    IncludedProperties = [nameof(PhoneBookCategory.Name)],
                    Data = new PhoneBookCategory { Name = "updated category 1" }
                };
                catUpdateRes = db.UpdateObject(category1.Id, deltaUpdate, userInfo);
                if (catUpdateRes.IsSuccess)
                {
                    var catRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo);
                    if (catRes.IsSuccess)
                    {
                        if (catRes.Result.Name == deltaUpdate.Data.Name)
                        {
                            Log($"Validated delta update of category {category1.Id}", 4);
                            category1 = catRes.Result;
                        }
                        else
                            Log($"Delta update of category {category1} didn't take, expected: {deltaUpdate.Data.Name}, received: {catRes.Result.Name}", 2);
                    }
                }
                var newDescription = "hello world";
                ExtendedMassUpdateParameters<PhoneBookCategory> massUpdates = new()
                {
                    Ids = [category1.Id, category2.Id],
                    IncludedProperties = [nameof(PhoneBookCategory.Description)],
                    TemplateObject = new PhoneBookCategory { Description = newDescription },
                    Values = new Dictionary<string, object> { { nameof(PhoneBookCategory.Description), newDescription } }
                };

                var massUpdateRes = db.BulkUpdateObject(massUpdates, userInfo);
                if (massUpdateRes.IsSuccess)
                {
                    var checkRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo);
                    if (checkRes.IsSuccess)
                    {
                        if (checkRes.Result.Description == newDescription)
                            Log($"Successfully validated bulk update of {category1.GetType().Name}", 4);
                        else
                            Log($"Unable to validate bulk update of {category1.GetType().Name}", 2);
                    }
                }
            }

            addRes = db.AddObject(category2, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(category2);
            var categoryRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo);

            addRes = db.AddObject(manager, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(manager);
            addRes = db.AddObject(secretary, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(secretary);

            contact1.ManagerId = manager.Id;
            contact1.SecretaryIds = [secretary.Id, manager.Id];

            addRes = db.AddObject(contact1, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(contact1);
            addRes = db.AddObject(contact2, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(contact2);

            var getFullRes = db.GetContactWithManager(contact1.Id, userInfo);

            var getFullCategoryRes = db.GetCategory(category2.Id, userInfo, true);

            var contactRes = db.GetObject<PhoneBookContact>(contact1.Id, userInfo);
            contactRes = db.GetObject<PhoneBookContact>(contact2.Id, userInfo);

            var associateRes = db.AssociateWithCategory(contact1.Id, category1.Id, userInfo);
            contactRes = db.GetObject<PhoneBookContact>(contact1.Id, userInfo, true);
            categoryRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo, true);

            associateRes = db.AssociateWithCategory(contact1.Id, category2.Id, userInfo);
            associateRes = db.AssociateWithCategory(contact2.Id, category2.Id, userInfo);

            contactRes = db.GetObject<PhoneBookContact>(contact1.Id, userInfo, true);
            contactRes = db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true);

            await SearchTest(category1, category2, pb1, pb2).ConfigureAwait(false);

            await ContactSearchTest(category1, category2, contact1, contact2, manager, secretary).ConfigureAwait(false);

            // try to add with just an Id
            {
                var tempContact2 = contactRes.Result;
                //tempContact2.Categories.Add(category1);
                tempContact2.CategoryIds.Add(category1.Id);
                var updateRes = db.UpdateObject(tempContact2, userInfo);

                contactRes = db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true);

                tempContact2 = contactRes.Result;
                //tempContact2.Categories.RemoveAll(u => u.Id == category1.Id);
                tempContact2.CategoryIds.Remove(category1.Id);

                updateRes = db.UpdateObject(tempContact2, userInfo);
                contactRes = db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true);
            }

            List<string> idsToDelete = [category1.Id, category2.Id];
            var bulkDeletRes = db.BulkDelete<PhoneBookCategory>(idsToDelete, userInfo);
            if (bulkDeletRes.IsSuccess)
            {
                if (bulkDeletRes.Result == 2)
                {
                    Log($"Successfully validated bulk delete of PhoneBookCategory", 4);
                    rollbackObjects.RemoveAll(u => u.Id == category1.Id);
                    rollbackObjects.RemoveAll(u => u.Id == category2.Id);
                }
                else
                    Log($"Unable to validate bulk delete of PhoneBookCategory. expected to delete 2, actually deleted: {bulkDeletRes.Result}", 2);
            }
            else
                Log($"Bulk delete of PhoneBookCategory failed: {bulkDeletRes}", 2);
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
                    deleteRes = db.DeleteObject<PhoneBook>(pb.Id, userInfo);
                }
                else if (rollbackObject is PhoneBookCategory pbCat)
                {
                    deleteRes = db.DeleteObject<PhoneBookCategory>(pbCat.Id, userInfo);
                }
                else if (rollbackObject is PhoneBookContact pbContact)
                {
                    deleteRes = db.DeleteObject<PhoneBookContact>(pbContact.Id, userInfo);
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
                    Log($"Sort didn't work, first item in't {contact2.FirstName}", 2);
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
                    Log($"Sort didn't work, first item in't {secretary.FirstName}", 2);
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
                    Log($"Sort didn't work, first item in't {contact1.FirstName}", 2);
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
                    Log($"Sort didn't work, first item in't {contact1.FirstName}", 2);
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
                    Log($"Sort didn't work, first item in't {contact1.FirstName}", 2);
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
                    Log($"Sort didn't work, first item in't {contact1.FirstName}", 2);
            }
        }
    }

    private async Task SearchTest(PhoneBookCategory category1, PhoneBookCategory category2, PhoneBook pb1, PhoneBook pb2)
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
            if (searchRes.Result.Results.Count != 2)
            {
                Log($"Not all categories found, expected: 2, received {searchRes.Result.Results.Count}", 2);
            }
            else
            {
                if (searchRes.Result.Results[0].Id != category1.Id)
                    Log($"Sort didn't work, first item in't {category2.Name}", 2);
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

            if (searchRes.Result.Results.Count != 1)
            {
                Log($"Not all categories found, expected: 2, received {searchRes.Result.Results.Count}", 2);
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
