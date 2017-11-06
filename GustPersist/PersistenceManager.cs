using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;
using System.Xml.Linq;

namespace Gust.Persist
{
    public abstract class PersistenceManager
    {
        public IKeyGenerator KeyGenerator { get; set; }

        public static SaveOptions ExtractSaveOptions(dynamic dynSaveBundle)
        {
            var jsonSerializer = CreateJsonSerializer();

            var dynSaveOptions = dynSaveBundle.saveOptions;
            var saveOptions = (SaveOptions)jsonSerializer.Deserialize(new JTokenReader(dynSaveOptions), typeof(SaveOptions));
            return saveOptions;
        }

        public SaveOptions SaveOptions { get; set; }

        public string Metadata()
        {
            lock (_metadataLock)
            {
                if (_jsonMetadata == null)
                {
                    _jsonMetadata = BuildJsonMetadata();
                }

                return _jsonMetadata;
            }
        }

        protected void InitializeSaveState(JObject saveBundle)
        {
            JsonSerializer = CreateJsonSerializer();

            var dynSaveBundle = (dynamic)saveBundle;
            var entitiesArray = (JArray)dynSaveBundle.entities;
            var dynSaveOptions = dynSaveBundle.saveOptions;
            SaveOptions = (SaveOptions)JsonSerializer.Deserialize(new JTokenReader(dynSaveOptions), typeof(SaveOptions));
            SaveWorkState = new SaveWorkState(this, entitiesArray);
        }

        public SaveResult SaveChanges(JObject saveBundle, TransactionSettings transactionSettings = null)
        {
            if (SaveWorkState == null || SaveWorkState.WasUsed)
            {
                InitializeSaveState(saveBundle);
            }

            transactionSettings = transactionSettings ?? GustConfig.Default.GetTransactionSettings();
            try
            {
                if (transactionSettings.TransactionType == TransactionType.DbTransaction)
                {
                    OpenDbConnection();
                    using (var trans = BeginTransaction(transactionSettings.IsolationLevelAs))
                    {
                        try
                        {
                            OpenAndSave(SaveWorkState);
                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
                else
                {
                    OpenAndSave(SaveWorkState);
                }
            }
            catch (EntityErrorsException e)
            {
                SaveWorkState.EntityErrors = e.EntityErrors;
                throw;
            }
            catch (Exception e2)
            {
                if (!HandleSaveException(e2, SaveWorkState))
                {
                    throw;
                }
            }
            finally
            {
                CloseDbConnection();
            }

            return SaveWorkState.ToSaveResult();
        }

        // allows subclasses to plug in own save exception handling
        // either throw an exception here, return false or return true and modify the saveWorkState.
        protected virtual bool HandleSaveException(Exception e, SaveWorkState saveWorkState)
        {
            return false;
        }

        void OpenAndSave(SaveWorkState saveWorkState)
        {
            OpenDbConnection();    // ensure connection is available for BeforeSaveEntities
            saveWorkState.BeforeSave();
            SaveChangesCore(saveWorkState);
            saveWorkState.AfterSave();
        }

        static JsonSerializer CreateJsonSerializer()
        {
            var serializerSettings = GustConfig.Default.GetJsonSerializerSettingsForSave();
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            return jsonSerializer;
        }

        /// <summary>
        /// Should only be called from BeforeSaveEntities and AfterSaveEntities.
        /// </summary>
        /// <returns>Open DbConnection used by the ContextProvider's implementation</returns>
        public abstract IDbConnection GetDbConnection();

        /// <summary>
        /// Internal use only.  Should only be called by ContextProvider during SaveChanges.
        /// Opens the DbConnection used by the ContextProvider's implementation.
        /// Method must be idempotent; after it is called the first time, subsequent calls have no effect.
        /// </summary>
        protected abstract void OpenDbConnection();

        /// <summary>
        /// Internal use only.  Should only be called by ContextProvider during SaveChanges.
        /// Closes the DbConnection used by the ContextProvider's implementation.
        /// </summary>
        protected abstract void CloseDbConnection();

        protected virtual IDbTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            var conn = GetDbConnection();
            EntityTransaction = conn.BeginTransaction(isolationLevel);
            return EntityTransaction;
        }

        protected abstract string BuildJsonMetadata();

        protected abstract void SaveChangesCore(SaveWorkState saveWorkState);

        public virtual object[] GetKeyValues(EntityInfo entityInfo)
        {
            throw new NotImplementedException();
        }

        protected virtual EntityInfo CreateEntityInfo()
        {
            return new EntityInfo();
        }

        public EntityInfo CreateEntityInfo(Object entity, EntityState entityState = EntityState.Added)
        {
            var ei = CreateEntityInfo();
            ei.Entity = entity;
            ei.EntityState = entityState;
            ei.ContextProvider = this;
            return ei;
        }

        public Func<EntityInfo, bool> BeforeSaveEntityDelegate { get; set; }
        public Func<Dictionary<Type, List<EntityInfo>>, Dictionary<Type, List<EntityInfo>>> BeforeSaveEntitiesDelegate { get; set; }
        public Action<Dictionary<Type, List<EntityInfo>>, List<KeyMapping>> AfterSaveEntitiesDelegate { get; set; }

        /// <summary>
        /// The method is called for each entity to be saved before the save occurs.  If this method returns 'false'
        /// then the entity will be excluded from the save.  The base implementation returns the result of BeforeSaveEntityDelegate,
        /// or 'true' if BeforeSaveEntityDelegate is null.
        /// </summary>
        /// <param name="entityInfo"></param>
        /// <returns>true to include the entity in the save, false to exclude</returns>
        protected internal virtual bool BeforeSaveEntity(EntityInfo entityInfo)
        {
            if (BeforeSaveEntityDelegate != null)
            {
                return BeforeSaveEntityDelegate(entityInfo);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Called after BeforeSaveEntity, and before saving the entities to the persistence layer.
        /// Allows adding, changing, and removing entities prior to save.
        /// The base implementation returns the result of BeforeSaveEntitiesDelegate, or the unchanged
        /// saveMap if BeforeSaveEntitiesDelegate is null.
        /// </summary>
        /// <param name="saveMap">A List of EntityInfo for each Type</param>
        /// <returns>The EntityInfo for each entity that should be saved</returns>
        protected internal virtual Dictionary<Type, List<EntityInfo>> BeforeSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap)
        {
            if (BeforeSaveEntitiesDelegate != null)
            {
                return BeforeSaveEntitiesDelegate(saveMap);
            }
            else
            {
                return saveMap;
            }
        }

        /// <summary>
        /// Called after the entities have been saved, and all the temporary keys have been replaced by real keys.
        /// The base implementation calls AfterSaveEntitiesDelegate, or does nothing if AfterSaveEntitiesDelegate is null.
        /// </summary>
        /// <param name="saveMap">The same saveMap that was returned from BeforeSaveEntities</param>
        /// <param name="keyMappings">The mapping of temporary keys to real keys</param>
        protected internal virtual void AfterSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap, List<KeyMapping> keyMappings)
        {
            AfterSaveEntitiesDelegate?.Invoke(saveMap, keyMappings);
        }

        protected internal EntityInfo CreateEntityInfoFromJson(dynamic jo, Type entityType)
        {
            var entityInfo = CreateEntityInfo();

            entityInfo.Entity = JsonSerializer.Deserialize(new JTokenReader(jo), entityType);
            entityInfo.EntityState = (EntityState)Enum.Parse(typeof(EntityState), (String)jo.entityAspect.entityState);
            entityInfo.ContextProvider = this;

            entityInfo.UnmappedValuesMap = JsonToDictionary(jo.__unmapped);
            entityInfo.OriginalValuesMap = JsonToDictionary(jo.entityAspect.originalValuesMap);

            var autoGeneratedKey = jo.entityAspect.autoGeneratedKey;
            if (entityInfo.EntityState == EntityState.Added && autoGeneratedKey != null)
            {
                entityInfo.AutoGeneratedKey = new AutoGeneratedKey(entityInfo.Entity, autoGeneratedKey);
            }
            return entityInfo;
        }

        Dictionary<string, object> JsonToDictionary(dynamic json)
        {
            if (json == null)
            {
                return null;
            }

            var jprops = ((System.Collections.IEnumerable)json).Cast<JProperty>();
            var dict = jprops.ToDictionary(jprop => jprop.Name, jprop =>
            {
                if (jprop.Value is JValue val)
                {
                    return val.Value;
                }
                else if (jprop.Value as JArray != null)
                {
                    return jprop.Value as JArray;
                }
                else
                {
                    return jprop.Value as JObject;
                }
            });
            return dict;
        }

        protected internal Type LookupEntityType(string entityTypeName)
        {
            var delims = new string[] { ":#" };
            var parts = entityTypeName.Split(delims, StringSplitOptions.None);
            var shortName = parts[0];
            var ns = parts[1];

            var typeName = ns + "." + shortName;
            var type = GustConfig.ProbeAssemblies
              .Select(a => a.GetType(typeName, false, true))
              .FirstOrDefault(t => t != null);
            if (type != null)
            {
                return type;
            }
            else
            {
                throw new ArgumentException("Assembly could not be found for " + entityTypeName);
            }
        }

        protected static Lazy<Type> KeyGeneratorType = new Lazy<Type>(() =>
        {
            var typeCandidates = GustConfig.ProbeAssemblies.Concat(new Assembly[] { typeof(IKeyGenerator).Assembly })
             .SelectMany(a => a.GetTypes()).ToList();
            var generatorTypes = typeCandidates.Where(t => typeof(IKeyGenerator).IsAssignableFrom(t) && !t.IsAbstract)
              .ToList();
            if (generatorTypes.Count == 0)
            {
                throw new Exception("Unable to locate a KeyGenerator implementation.");
            }
            return generatorTypes.First();
        });

        /// <summary>Gets the current transaction, if one is in progress.</summary>
        public IDbTransaction EntityTransaction { get; protected set; }

        protected SaveWorkState SaveWorkState { get; private set; }
        protected JsonSerializer JsonSerializer { get; private set; }

        object _metadataLock = new object();
        string _jsonMetadata;

    }
}