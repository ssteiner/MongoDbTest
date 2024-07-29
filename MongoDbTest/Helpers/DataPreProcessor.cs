using NoSqlModels;

namespace MongoDbTest.Helpers;

internal class DataPreProcessor
{
    internal static void ProcessNewDbObject<T>(T newObject, MongoDatabaseContext context, bool lookupItems) where T : class, IIdItem
    {
        if (newObject is PhoneBookContact pbc)
        {
            pbc.Categories ??= [];
            pbc.Numbers ??= [];
            //ProcessPhoneBookContact(pbc, context, lookupItems, []);
        }
        else if (newObject is PhoneBookCategory category)
        {
            //category.PhoneBooks ??= [];
            //ProcessPhoneBookCategory(category, context, lookupItems, []);
        }
        else if (newObject is PhoneBook pb)
            ProcessPhoneBook(pb, context, lookupItems, []);
    }

    internal static async Task ProcessUpdatedDbObject<T>(T updatedObject, MongoDatabaseContext context, bool lookupItems, List<string> updatedProperties)
    {
        //if (updatedObject is LdapConfiguration ldapConfig)
        //    ProcessLdapConfiguration(ldapConfig, context, lookupItems, updatedProperties);
        if (updatedObject is PhoneBookContact pbc)
            await ProcessPhoneBookContact(pbc, context, lookupItems, updatedProperties).ConfigureAwait(false);
        else if (updatedObject is PhoneBook pb)
            ProcessPhoneBook(pb, context, lookupItems, updatedProperties);
        else if (updatedObject is PhoneBookCategory category)
            ProcessPhoneBookCategory(category, context, lookupItems, updatedProperties);
        //else if (updatedObject is GlobalConfiguration globalConfig)
        //    ProcessGlobalConfiguration(globalConfig, context, lookupItems, updatedProperties);
    }

    private static async Task ProcessPhoneBookContact(PhoneBookContact pbc, MongoDatabaseContext context, bool lookupItems, List<string> updatedProperties)
    {
        if (pbc.CategoryIds != null)
        {
            if (lookupItems)
            {
                var categoryTable = context.AccessibleObjects<PhoneBookCategory>(false);
                var dbItems = categoryTable.Where(u => pbc.CategoryIds.Contains(u.Id)).ToList();
                int nbRemoved = pbc.CategoryIds.RemoveAll(x => !dbItems.Any(y => x == y.Id)); // remove unknown values
                if (nbRemoved > 0)
                    AddUpdatedProperty(nameof(PhoneBookContact.CategoryIds), updatedProperties);
            }
        }
        if (pbc.Numbers != null)
        {
            foreach (var nb in pbc.Numbers)
            {
                nb.Id ??= Guid.NewGuid().ToString();
            }
            AddUpdatedProperty(nameof(PhoneBookContact.Numbers), updatedProperties);
        }
        else if (updatedProperties.Contains(nameof(PhoneBookContact.Numbers)))
            pbc.Numbers = [];

        var table = context.AccessibleObjects<PhoneBookContact>(false);
        if (pbc.ManagerId != null)
        {
            if (lookupItems)
            {
                var manager = table.FirstOrDefault(u => u.Id == pbc.ManagerId);
                if (manager == null)
                {
                    pbc.ManagerId = null;
                    AddUpdatedProperty(nameof(PhoneBookContact.ManagerId), updatedProperties);
                }
            }
        }
        //if (pbc.AssistantId != null)
        //{
        //    if (lookupItems)
        //    {
        //        var manager = table.FindOne(u => u.Id == pbc.AssistantId);
        //        if (manager != null)
        //            pbc.Assistant = manager;
        //        else
        //            pbc.Assistant = null;
        //    }
        //    else
        //        pbc.Assistant = new PhoneBookContact { Id = pbc.AssistantId.Value };
        //}
        //else if (updatedProperties.Contains(nameof(PhoneBookContact.AssistantId)))
        //    pbc.Assistant = null;
        //if (updatedProperties.Contains(nameof(PhoneBookContact.AssistantId)))
        //    updatedProperties.Add(nameof(PhoneBookContact.Assistant));
        if (lookupItems)
        {
            //if (pbc.ManagerId == null && !string.IsNullOrEmpty(pbc.ManagerUserId)) // from import
            //{
            //    var lowerCaseUserId = pbc.ManagerUserId.ToLower();
            //    var manager = table.FindOne(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId);
            //    if (manager != null)
            //        pbc.Manager = manager;
            //    else
            //        Log($"Unable to look up {pbc.ManagerUserId} in phonebook contact {pbc.UserId}", 3, context.User);
            //}
            //if (pbc.AssistantId == null && !string.IsNullOrEmpty(pbc.AssistantUserId))
            //{
            //    var lowerCaseUserId = pbc.AssistantUserId.ToLower();
            //    pbc.AssistantId = table.FindOne(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId)?.Id;
            //    if (pbc.AssistantId == null)
            //        Log($"Unable to look up {pbc.AssistantUserId} in phonebook contact {pbc.UserId}", 3, context.User);
            //}
            if (pbc.Secretary != null)
            {
                foreach (var secretary in pbc.Secretary)
                {
                    if (secretary.Id == null && !string.IsNullOrEmpty(secretary.UserId))
                    {
                        var lowerCaseUserId = secretary.UserId.ToLower();
                        secretary.Id = table.FirstOrDefault(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId)?.Id ?? null;
                        //if (secretary.Id == null) // not found
                        //    Log($"Unable to look up {secretary.UserId} in phonebook contact {pbc.UserId}", 3, context.User);
                    }
                    //if (secretary.Id != Guid.Empty)
                    //{
                    //    pbc.Secretary.Add(secretary);
                    //    //dbObj.Secretary.Add(new PhoneBookContactSecretary { PhoneBookContact = dbObj, SecretaryId = secretary.Id });
                    //}
                }
            }
            if (pbc.SecretaryIds != null)
            {
                var dbItems = table.Where(u => pbc.CategoryIds.Contains(u.Id)).ToList();
                int nbRemoved = pbc.SecretaryIds.RemoveAll(x => !dbItems.Any(y => x == y.Id)); // remove unknown values
                if (nbRemoved > 0)
                    AddUpdatedProperty(nameof(PhoneBookContact.SecretaryIds), updatedProperties);
            }
            //if (pbc.SeeAlso != null)
            //{
            //    foreach (var seeAlso in pbc.SeeAlso)
            //    {
            //        if (seeAlso.Id == Guid.Empty && !string.IsNullOrEmpty(seeAlso.UserId))
            //        {
            //            var lowerCaseUserId = seeAlso.UserId.ToLower();
            //            seeAlso.Id = table.FindOne(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId)?.Id ?? Guid.Empty;
            //            if (seeAlso.Id == Guid.Empty) // not found
            //                Log($"Unable to look up {seeAlso.UserId} in phonebook contact {pbc.UserId}", 3, context.User);
            //        }
            //        //if (seeAlso.Id != Guid.Empty)
            //        //{
            //        //    //dbObj.SeeAlso.Add(new PhoneBookContactSeeAlso { PhoneBookContact = dbObj, SeeAlsoId = seeAlso.Id });
            //        //}
            //    }
            //}
        }
        pbc.LastUpdate = DateTime.Now;
        pbc.LastUpdateBy = context.User.UserId;
    }

    private static void AddUpdatedProperty(string propertyName, List<string> updatedProperties)
    {
        if (!updatedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
            updatedProperties.Add(propertyName);
    }

    private static void ProcessPhoneBookCategory(PhoneBookCategory category, MongoDatabaseContext context, bool lookupItems, List<string> updatedProperties)
    {
        if (category.PhoneBookIds != null && category.PhoneBookIds.Count > 0)
        {
            category.PhoneBooks ??= [];
            if (lookupItems)
            {
                var table = context.AccessibleObjects<PhoneBook>(false);
                var dbItems = table.Where(u => category.PhoneBookIds.Contains(u.Id)).ToList();
                int nbRemoved = category.PhoneBookIds.RemoveAll(x => !dbItems.Any(y => x == y.Id)); // remove unknown values
                if (nbRemoved > 0)
                    AddUpdatedProperty(nameof(PhoneBookContact.PhoneBookIds), updatedProperties);

                //foreach (var added in category.PhoneBookIds)
                //{
                //    var dbItem = dbItems.FirstOrDefault(u => u.Id == added);
                //    if (dbItem != null)
                //        category.PhoneBooks.Add(dbItem);
                //}
            }
            //else
            //{
            //    foreach (var added in category.PhoneBookIds)
            //    {
            //        category.PhoneBooks.Add(new PhoneBook { Id = added });
            //    }
            //}
            //category.PhoneBookIds = null;
        }
        //else
        //    category.PhoneBooks = [];
        //if (updatedProperties.Contains(nameof(PhoneBookCategory.PhoneBookIds)))
        //    updatedProperties.Add(nameof(PhoneBookCategory.PhoneBooks));
    }

    private static void ProcessPhoneBook(PhoneBook pb, MongoDatabaseContext context, bool lookupItems, List<string> updatedProperties)
    {
        //    if (pb.ImportLdapDirectoryId.HasValue)
        //    {
        //        if (lookupItems)
        //        {
        //            var table = context.GetCollection<LdapDirectory>();
        //            var dbItem = table.FindOne(u => u.Id == pb.ImportLdapDirectoryId);
        //            if (dbItem != null)
        //                pb.ImportLdapDirectory = dbItem;
        //            else
        //                pb.ImportLdapDirectoryId = null;
        //        }
        //        else
        //            pb.ImportLdapDirectory = new LdapDirectory { Id = pb.ImportLdapDirectoryId.Value };
        //        pb.ImportLdapDirectoryId = null;
        //    }
        //    else if (updatedProperties.Contains(nameof(PhoneBook.ImportLdapDirectoryId)))
        //        pb.ImportLdapDirectory = null;
        //    if (updatedProperties.Contains(nameof(PhoneBook.ImportLdapDirectoryId)))
        //        updatedProperties.Add(nameof(PhoneBook.ImportLdapDirectory));
        //    if (pb.LdapDirectoryId.HasValue)
        //    {
        //        if (lookupItems)
        //        {
        //            var table = context.GetCollection<LdapDirectory>();
        //            var dbItem = table.FindOne(u => u.Id == pb.LdapDirectoryId);
        //            if (dbItem != null)
        //                pb.LdapDirectory = dbItem;
        //            else
        //                pb.LdapDirectory = null;
        //        }
        //        else
        //            pb.LdapDirectory = new LdapDirectory { Id = pb.LdapDirectoryId.Value };
        //        pb.LdapDirectoryId = null;
        //    }
        //    else if (updatedProperties.Contains(nameof(PhoneBook.LdapDirectoryId)))
        //        pb.LdapDirectory = null;
        //    if (updatedProperties.Contains(nameof(PhoneBook.LdapDirectoryId)))
        //        updatedProperties.Add(nameof(PhoneBook.LdapDirectory));
        //    if (pb.DefaultImportPhoneBookCategoryId.HasValue)
        //    {
        //        if (lookupItems)
        //        {
        //            var table = context.GetCollection<PhoneBookCategory>();
        //            var dbItem = table.FindOne(u => u.Id == pb.DefaultImportPhoneBookCategoryId);
        //            if (dbItem != null)
        //                pb.DefaultImportPhoneBookCategory = dbItem;
        //            else
        //                pb.DefaultImportPhoneBookCategory = null;
        //        }
        //        else
        //            pb.DefaultImportPhoneBookCategory = new PhoneBookCategory { Id = pb.DefaultImportPhoneBookCategoryId.Value };
        //        pb.DefaultImportPhoneBookCategoryId = null;
        //    }
        //    else if (updatedProperties.Contains(nameof(PhoneBook.DefaultImportPhoneBookCategoryId)))
        //        pb.DefaultImportPhoneBookCategory = null;
        //    if (updatedProperties.Contains(nameof(PhoneBook.DefaultImportPhoneBookCategoryId)))
        //        updatedProperties.Add(nameof(PhoneBook.DefaultImportPhoneBookCategory));
        //    if (pb.ImportDialPlanId.HasValue)
        //    {
        //        if (lookupItems)
        //        {
        //            var table = context.GetCollection<DialPlan>();
        //            var dbItem = table.FindOne(u => u.Id == pb.ImportDialPlanId);
        //            if (dbItem != null)
        //                pb.ImportDialPlan = dbItem;
        //            else
        //                pb.ImportDialPlan = null;
        //        }
        //        else
        //            pb.ImportDialPlan = new DialPlan { Id = pb.ImportDialPlanId.Value };
        //    }
        //    else if (updatedProperties.Contains(nameof(PhoneBook.ImportDialPlanId)))
        //        pb.ImportDialPlan = null;
        //    if (updatedProperties.Contains(nameof(PhoneBook.ImportDialPlanId)))
        //        updatedProperties.Add(nameof(PhoneBook.ImportDialPlan));
    }
}
