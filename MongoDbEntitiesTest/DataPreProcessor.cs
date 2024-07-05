using GeneralTools.Extensions;
using MongoDB.Entities;
using MongoDbEntitiesTest.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDbEntitiesTest;

internal partial class MongoDbContext
{
    private async Task ProcessNewDbObject<T>(T newObject, MongoDatabaseContext context, bool lookupItems) where T: BaseEntity, NoSqlModels.IIdItem
    {
        if (newObject is PhoneBookContact pbc)
        {
            if (pbc.CategoryIds?.Count > 0)
            {
                var dbItems = await DB.Find<PhoneBookCategory>().ManyAsync(u => pbc.PhoneBookIds.Contains(u.Id)).ConfigureAwait(false);
                foreach (var added in dbItems)
                {
                    await pbc.Categories.AddAsync(added.Id).ConfigureAwait(false);
                }
            }
            if (pbc.ManagerId != null)
            {
                var dbItem = await DB.Find<PhoneBookContact>().MatchID(pbc.ManagerId).ExecuteFirstAsync().ConfigureAwait(false);
                if (dbItem != null)
                {
                    pbc.Manager = new(pbc.ManagerId);
                    await pbc.SaveAsync().ConfigureAwait(false);
                }
            }
        }
        else if (newObject is PhoneBookCategory pbcat)
        {
            if (pbcat.PhoneBookIds?.Count > 0)
            {
                var dbItems = await DB.Find<PhoneBook>().ManyAsync(u => pbcat.PhoneBookIds.Contains(u.Id)).ConfigureAwait(false);
                foreach (var added in dbItems)
                {
                    await pbcat.PhoneBooks.AddAsync(added.Id).ConfigureAwait(false);
                }
            }
        }
    }

    //private async Task ProcessPhoneBookContact(PhoneBookContact pbc, MongoDatabaseContext context, bool lookupItems, List<string> updatedProperties)
    //{
    //    //pbc.Categories ??= [];
    //    //pbc.Secretary ??= [];
    //    //pbc.SeeAlso ??= [];
    //    //pbc.Numbers ??= [];
    //    if (pbc.CategoryIds != null)
    //    {
    //        pbc.Categories = [];
    //        if (lookupItems)
    //        {
    //            var categoryTable = context.GetCollection<PhoneBookCategory>();
    //            var dbItems = await DB.Find<PhoneBookCategory>().ManyAsync(u => pbc.CategoryIds.Contains(u.Id)).ConfigureAwait(false);

    //            foreach (var added in pbc.CategoryIds)
    //            {
    //                var dbItem = dbItems.FirstOrDefault(u => u.Id == added);
    //                if (dbItem != null)
    //                    await pbc.Categories.AddAsync(dbItem.Id).ConfigureAwait(false);
    //            }
    //        }
    //        else
    //        {
    //            foreach (var added in pbc.CategoryIds)
    //            {
    //                pbc.Categories.Add(new PhoneBookCategory { Id = added });
    //            }
    //        }
    //        pbc.CategoryIds = null;
    //        updatedProperties.Add(nameof(PhoneBookContact.Categories));
    //    }
    //    else if (updatedProperties.Contains(nameof(PhoneBookContact.CategoryIds)))
    //        pbc.Categories = [];
    //    if (updatedProperties.Contains(nameof(PhoneBookContact.CategoryIds)))
    //        updatedProperties.Add(nameof(PhoneBookContact.Categories));
    //    if (pbc.Numbers != null)
    //    {
    //        foreach (var nb in pbc.Numbers)
    //        {
    //            if (nb.Id == Guid.Empty)
    //                nb.Id = Guid.NewGuid();
    //        }
    //        updatedProperties.Add(nameof(PhoneBookContact.Number));
    //    }
    //    else if (updatedProperties.Contains(nameof(PhoneBookContact.Number)))
    //        pbc.Numbers = [];

    //    var table = context.GetCollection<PhoneBookContact>();
    //    if (pbc.ManagerId.HasValue)
    //    {
    //        if (lookupItems)
    //        {
    //            var manager = table.FindOne(u => u.Id == pbc.ManagerId);
    //            if (manager != null)
    //                pbc.Manager = manager;
    //            else
    //                pbc.ManagerId = null;
    //        }
    //        else
    //            pbc.Manager = new PhoneBookContact { Id = pbc.ManagerId.Value };
    //    }
    //    else if (updatedProperties.Contains(nameof(PhoneBookContact.ManagerId)))
    //        pbc.Manager = null;
    //    if (updatedProperties.Contains(nameof(PhoneBookContact.ManagerId)))
    //        updatedProperties.Add(nameof(PhoneBookContact.Manager));
    //    if (pbc.AssistantId.HasValue)
    //    {
    //        if (lookupItems)
    //        {
    //            var manager = table.FindOne(u => u.Id == pbc.AssistantId);
    //            if (manager != null)
    //                pbc.Assistant = manager;
    //            else
    //                pbc.Assistant = null;
    //        }
    //        else
    //            pbc.Assistant = new PhoneBookContact { Id = pbc.AssistantId.Value };
    //    }
    //    else if (updatedProperties.Contains(nameof(PhoneBookContact.AssistantId)))
    //        pbc.Assistant = null;
    //    if (updatedProperties.Contains(nameof(PhoneBookContact.AssistantId)))
    //        updatedProperties.Add(nameof(PhoneBookContact.Assistant));
    //    if (lookupItems)
    //    {
    //        if (pbc.ManagerId == null && !string.IsNullOrEmpty(pbc.ManagerUserId)) // from import
    //        {
    //            var lowerCaseUserId = pbc.ManagerUserId.ToLower();
    //            var manager = table.FindOne(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId);
    //            if (manager != null)
    //                pbc.Manager = manager;
    //            else
    //                Log($"Unable to look up {pbc.ManagerUserId} in phonebook contact {pbc.UserId}", 3, context.User);
    //        }
    //        if (pbc.AssistantId == null && !string.IsNullOrEmpty(pbc.AssistantUserId))
    //        {
    //            var lowerCaseUserId = pbc.AssistantUserId.ToLower();
    //            pbc.AssistantId = table.FindOne(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId)?.Id;
    //            if (pbc.AssistantId == null)
    //                Log($"Unable to look up {pbc.AssistantUserId} in phonebook contact {pbc.UserId}", 3, context.User);
    //        }
    //        if (pbc.Secretary != null)
    //        {
    //            foreach (var secretary in pbc.Secretary)
    //            {
    //                if (secretary.Id == Guid.Empty && !string.IsNullOrEmpty(secretary.UserId))
    //                {
    //                    var lowerCaseUserId = secretary.UserId.ToLower();
    //                    secretary.Id = table.FindOne(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId)?.Id ?? Guid.Empty;
    //                    if (secretary.Id == Guid.Empty) // not found
    //                        Log($"Unable to look up {secretary.UserId} in phonebook contact {pbc.UserId}", 3, context.User);
    //                }
    //                //if (secretary.Id != Guid.Empty)
    //                //{
    //                //    pbc.Secretary.Add(secretary);
    //                //    //dbObj.Secretary.Add(new PhoneBookContactSecretary { PhoneBookContact = dbObj, SecretaryId = secretary.Id });
    //                //}
    //            }
    //        }
    //        if (pbc.SeeAlso != null)
    //        {
    //            foreach (var seeAlso in pbc.SeeAlso)
    //            {
    //                if (seeAlso.Id == Guid.Empty && !string.IsNullOrEmpty(seeAlso.UserId))
    //                {
    //                    var lowerCaseUserId = seeAlso.UserId.ToLower();
    //                    seeAlso.Id = table.FindOne(u => u.UserId != null && u.UserId.ToLower() == lowerCaseUserId)?.Id ?? Guid.Empty;
    //                    if (seeAlso.Id == Guid.Empty) // not found
    //                        Log($"Unable to look up {seeAlso.UserId} in phonebook contact {pbc.UserId}", 3, context.User);
    //                }
    //                //if (seeAlso.Id != Guid.Empty)
    //                //{
    //                //    //dbObj.SeeAlso.Add(new PhoneBookContactSeeAlso { PhoneBookContact = dbObj, SeeAlsoId = seeAlso.Id });
    //                //}
    //            }
    //        }
    //    }
    //    pbc.LastUpdate = DateTime.Now;
    //    pbc.LastUpdateBy = context.User.UserId;
    //}

}
