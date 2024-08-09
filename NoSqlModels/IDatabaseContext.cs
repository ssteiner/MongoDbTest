using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoSqlModels;

public interface IDatabaseContext
{
    Task<IOperationResult> AddAudioFile(string ownerId, AudioFileType type, AudioFileStorage audioFile, IUserInformation userInfo);
    Task<IOperationResult> AddAudioFiles(string ownerId, List<AudioFileStorage> audioFiles, IUserInformation userInfo);
    Task<IOperationResult> AddObject<T>(T obj, IUserInformation userInfo) where T : class, IIdItem;
    Task<IOperationResult<int>> BulkDelete<T>(List<string> ids, IUserInformation userInfo) where T : class, IIdItem;
    Task<IOperationResult<int>> BulkUpdateObject<T>(ExtendedMassUpdateParameters<T> parameters, IUserInformation userInfo) where T : class, IIdItem;
    Task<IOperationResult> DeleteObject<T>(string id, IUserInformation userInfo) where T : class, IIdItem;
    Task<IOperationResult<List<T>>> GetAllObjects<T>(IUserInformation userInfo, bool ignorePermissions = false) where T : class, IIdItem;
    Task<IOperationResult<AudioFileStorage>> GetAudioFile(string ownerId, AudioFileType type, IUserInformation userInfo);
    Task<IOperationResult<List<AudioFileStorage>>> GetAudioFiles(string ownerId, IUserInformation userInfo);
    Task<IOperationResult<T>> GetObject<T>(string id, IUserInformation userInfo, bool includeDependencies = false, bool ignoreObjectAccessibility = false, bool ignorePermissions = false, bool includeCredentials = true) where T : class, IIdItem;
    Task<IOperationResult<PluginConfiguration>> GetPluginConfig(Guid id, IUserInformation userInfo);
    Task<IOperationResult> RemoveAudioFile(string ownerId, AudioFileType type, IUserInformation userInfo);
    Task<IOperationResult<int>> RemoveAudioFiles(string ownerId, IUserInformation userInfo);
    IOperationResult<SearchResults<T>> SearchObjects<T>(GenericSearchParameters parameters, IUserInformation userInfo) where T : class, IIdItem;
    Task<IOperationResult> UpdateObject<T>(string id, DeltaBaseObject<T> update, IUserInformation userInfo, bool ignoreUpdateOfInternalFields = true) where T : class, IIdItem;
    Task<IOperationResult> UpdateObject<T>(T obj, IUserInformation userInfo, bool ignoreUpdateOfInternalFields = true) where T : class, IIdItem;
}
