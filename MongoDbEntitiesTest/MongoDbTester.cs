using GenericProvisioningLib;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDbEntitiesTest.DbModels;

namespace MongoDbEntitiesTest;

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

    internal async Task RunPhonebookTests()
    {
        var userInfo = new GenericTenantUser { UserId = "admin" };
        List<NoSqlModels.IIdItem> rollbackObjects = [];
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
                    new NoSqlModels.PhoneBookContactNumber { /*Id = Guid.NewGuid(), */ Id = ObjectId.GenerateNewId().ToString(), Number = "+41587770001", Type = NoSqlModels.NumberType.Office },
                    new NoSqlModels.PhoneBookContactNumber { /*Id = Guid.NewGuid(), */ Id = ObjectId.GenerateNewId().ToString(), Number = "+41767770002", Type = NoSqlModels.NumberType.Mobile }
                    ]
        };
        PhoneBookContact contact2 = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "contact 2",
            LastName = "Meister",
            Location = "Bern",
            Numbers = [
                new NoSqlModels.PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(),  Number = "+41587770003", Type = NoSqlModels.NumberType.Office },
                new NoSqlModels.PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(), Number = "+41767770004", Type = NoSqlModels.NumberType.Mobile }
                ]
        };
        PhoneBookContact manager = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "manager",
            LastName = "Meier",
            Location = "Züri",
            Numbers = [
                new NoSqlModels.PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(), Number = "+41587770005", Type = NoSqlModels.NumberType.Office },
            ]
        };

        PhoneBookContact secretary = new()
        {
            //Id = Guid.NewGuid(),
            FirstName = "secretary",
            LastName = "Müller",
            Location = "Züri",
            Numbers = [
                new NoSqlModels.PhoneBookContactNumber { /*Id = Guid.NewGuid(),*/ Id = ObjectId.GenerateNewId().ToString(),  Number = "+41587770006", Type = NoSqlModels.NumberType.Office },
            ]
        };


        try
        {
            var addRes = await db.AddObject(pb1, userInfo);
            if (addRes.IsSuccess)
            {
                Log($"Successfully added {pb1.GetType().Name} {pb1.Name}", 4);
                rollbackObjects.Add(pb1);
            }

            addRes = await db.AddObject(pb2, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(pb2);
            category1.PhoneBookIds = [pb1.Id];
            category2.PhoneBookIds = [pb2.Id];

            addRes = await db.AddObject(category1, userInfo);
            if (addRes.IsSuccess)
            {
                rollbackObjects.Add(category1);

                category1.Name = "category11";

                var catUpdateRes = await db.UpdateObject(category1, userInfo);
                if (catUpdateRes.IsSuccess)
                {
                    var catRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo);
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

                NoSqlModels.DeltaBaseObject<PhoneBookCategory> deltaUpdate = new()
                {
                    IncludedProperties = [nameof(PhoneBookCategory.Name)],
                    Data = new PhoneBookCategory { Name = "updated category 1" }
                };
                catUpdateRes = await db.UpdateObject(category1.Id, deltaUpdate, userInfo);
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
            }

            addRes = await db.AddObject(category2, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(category2);
            var categoryRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo);

            addRes = await db.AddObject(manager, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(manager);
            addRes = await db.AddObject(secretary, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(secretary);

            contact1.ManagerId = manager.Id;
            contact1.SecretaryIds = [secretary.Id];

            addRes = await db.AddObject(contact1, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(contact1);
            addRes = await db.AddObject(contact2, userInfo);
            if (addRes.IsSuccess)
                rollbackObjects.Add(contact2);


            var getFullRes = db.GetContactWithManager(contact1.Id, userInfo);

            var contactRes = db.GetObject<PhoneBookContact>(contact1.Id, userInfo);
            contactRes = db.GetObject<PhoneBookContact>(contact2.Id, userInfo);

            var associateRes = db.AssociateWithCategory(contact1.Id, category1.Id, userInfo);
            if (associateRes.IsSuccess)
            {
                contactRes = db.GetObject<PhoneBookContact>(contact1.Id, userInfo, true);
                if (contactRes.IsSuccess)
                {
                    if (contactRes.Result.CategoryIds?.Contains(category1.Id) == true)
                        Log($"Successfully validated adding category {category1.Name} to contact {contact1.Id}", 4);
                    else
                        Log($"Adding category {category1.Name} to contact {contact1.Id} succeded, but a get didn't return the added category", 2);
                }
            }
            //categoryRes = db.GetObject<PhoneBookCategory>(category1.Id, userInfo, true);

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
                var updateRes = await db.UpdateObject(tempContact2, userInfo);
                if (updateRes.IsSuccess)
                {
                    contactRes = db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true);
                    if (contactRes.IsSuccess)
                    {
                        if (contactRes.Result.CategoryIds?.Contains(category1.Id) == true)
                            Log($"Successfully validated adding category {category1.Name} to contact {tempContact2.Id}", 4);
                        else
                            Log($"Adding category {category1.Name} to contact {tempContact2.Id} succeded, but a get didn't return the added category", 2);
                    }
                }
                
                if (contactRes.IsSuccess)
                    tempContact2 = contactRes.Result;
                //tempContact2.Categories.RemoveAll(u => u.Id == category1.Id);
                tempContact2.CategoryIds.Remove(category1.Id);

                updateRes = await db.UpdateObject(tempContact2, userInfo);
                if (updateRes.IsSuccess)
                {
                    contactRes = db.GetObject<PhoneBookContact>(contact2.Id, userInfo, true);
                    if (contactRes.IsSuccess)
                    {
                        if (contactRes.Result.CategoryIds?.Contains(category1.Id) == true)
                            Log($"Removing category {category1.Name} from contact {tempContact2.Id} succeded, but a get still returns the included category", 2);
                        else
                            Log($"Successfully validated removing category {category1.Name} from contact {tempContact2.Id}", 4);
                    }
                }
            }
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
                    deleteRes = await db.DeleteObject<PhoneBook>(pb.Id, userInfo);
                }
                else if (rollbackObject is PhoneBookCategory pbCat)
                {
                    deleteRes = await db.DeleteObject<PhoneBookCategory>(pbCat.Id, userInfo);
                }
                else if (rollbackObject is PhoneBookContact pbContact)
                {
                    deleteRes = await db.DeleteObject<PhoneBookContact>(pbContact.Id, userInfo);
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
        NoSqlModels.PhoneBookContactSearchParameters searchParameters = new() // everything starting with 'contact', order descending
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
            SearchParameters = [new NoSqlModels.GenericSearchParameter
            {
                FieldName = nameof(PhoneBookContact.NumberOfTelephoneNumbers),
                FieldOperator = NoSqlModels.ComparisonOperator.LessThan,
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
            SearchParameters = [new NoSqlModels.GenericSearchParameter
            {
                FieldName = nameof(PhoneBookContact.NumberOfTelephoneNumbers),
                FieldOperator = NoSqlModels.ComparisonOperator.MoreThanOrEqualTo,
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

        searchParameters = new NoSqlModels.PhoneBookContactSearchParameters()
        {
            SearchParameters =
            [ // gets us contact 1
                new() { FieldName = nameof(PhoneBookContact.ManagerId), FieldOperator = NoSqlModels.ComparisonOperator.IsNotEmpty}
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

        searchParameters = new NoSqlModels.PhoneBookContactSearchParameters()
        {
            SearchParameters =
            [ // gets us contact 1
                new() { FieldName = nameof(PhoneBookContact.ManagerId), FieldOperator = NoSqlModels.ComparisonOperator.IsNotEmpty}
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

        searchParameters = new NoSqlModels.PhoneBookContactSearchParameters()
        {
            SearchParameters =
            [ // gets us contact 1
                new() {
                    FieldName = nameof(PhoneBookContact.SecretaryIds),
                    FieldValue = new Newtonsoft.Json.Linq.JArray(new string[] { secretary.Id.ToString() }),
                    FieldOperator = NoSqlModels.ComparisonOperator.Contains
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
        NoSqlModels.PhoneBookCategorySearchParameters searchParameters = new()
        {
            Query = "category",
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

        searchParameters = new NoSqlModels.PhoneBookCategorySearchParameters { Name = "category2" }; // case insensitive expected
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


        searchParameters = new NoSqlModels.PhoneBookCategorySearchParameters { Name = "%1" }; // case insensitive expected
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

        searchParameters = new NoSqlModels.PhoneBookCategorySearchParameters { PhoneBookIds = [pb1.Id] };
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
