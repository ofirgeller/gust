using Gust.Configs;
using Gust.Keys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using static Gust.EntityNameUtil;

namespace Gust
{
    public partial class PersistManager<T> : IDisposable where T : DbContext, new()
    {
        public T Context { get; }
        public IDbContextTransaction Transaction;

        static IModel _model;
        static IModel GetModel(T ctx)
        {
            if (_model == null)
            {
                _model = ctx.Model;
            }

            return _model;
        }
        static List<EntitySetInfo> _entitySetsInfo;

        protected SaveWorkState SaveWorkState { get; private set; }
        protected JsonSerializer JsonSerializer { get; private set; }
        public SaveOptions SaveOptions { get; set; }
        public IsolationLevel? TransactionIsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        bool _ownsContext;
        GustConfig _config = new GustConfig();

        static IEnumerable<IEntityType> GetTypesEntityDependsOn(IEntityType et)
        {
            return et.GetNavigations().Where(n => n.IsDependentToPrincipal())
                                      .Select(i => i.GetTargetType());
        }

        /// <summary>
        /// Returns a description of each entity in the context, sorted according to 
        /// the order the entities should be saved
        /// </summary>
        public static List<EntitySetInfo> GetEntitySetsInfo(T ctx)
        {
            var model = GetModel(ctx);

            var entityTypes = model.GetEntityTypes();

            var typesAndDependencies = entityTypes.OrderBy(et => et.Name)
                .Select(et =>
                {
                    var dependencies = GetTypesEntityDependsOn(et).Select(e => e.Name);
                    var depenednciesHashset = new HashSet<string>(dependencies);

                    return (et, dependencies);

                }).ToList();

            var entitiesInfo = new List<EntitySetInfo>();
            var alreadySaved = new HashSet<string>();

            /// Will holt or throw since we are removing an item each time or throwing if we did not
            /// find an item
            while (typesAndDependencies.Count > 0)
            {
                var nextToSave = typesAndDependencies.First((t) =>
                {
                    return t.dependencies.Except(alreadySaved).Count() == 0;
                });

                var jsName = JsTypeNameFromType(nextToSave.et.ClrType);

                alreadySaved.Add(nextToSave.et.Name);
                typesAndDependencies.Remove(nextToSave);
                var entitySetInfo = new EntitySetInfo(jsName, nextToSave.et.ClrType, nextToSave.et);
                entitiesInfo.Add(entitySetInfo);
            }

            return entitiesInfo;
        }

        /// <summary>
        /// Returns a description of each entity in the context, sorted according to 
        /// the order the entities should be saved.
        /// </summary>
        public List<EntitySetInfo> GetEntitySetsInfo()
        {
            if (_entitySetsInfo == null)
            {
                _entitySetsInfo = GetEntitySetsInfo(Context);
            }

            return _entitySetsInfo;
        }

        public PersistManager(T context = null, bool ownsContext = true)
        {
            Context = context ?? CreateContext();
            _ownsContext = ownsContext;
            if (_ownsContext)
            {
                if (Context.Database.CurrentTransaction == null)
                {
                    Transaction = Context.Database.BeginTransaction();
                }
            }
        }

        /// <summary>
        /// When the manager needs a data context and does not already have one 
        /// this method be called in order to create one
        /// If you want to have the context in any other way except calling the default ctor
        /// you can override this method
        /// </summary>
        public virtual T CreateContext()
        {
            var ctx = new T();
            return ctx;
        }

        public DbConnection GetDbConnection()
        {
            return Context.Database.GetDbConnection();
        }

        protected JsonSerializer CreateJsonSerializer()
        {
            var serializerSettings = GustConfig.Default.GetJsonSerializerSettingsForSave();
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            return jsonSerializer;
        }

        static Dictionary<string, object> JsonToDictionary(dynamic json)
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

        object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }

            return null;
        }

        /// <summary>
        /// Allowes the subclass to change/add/remove the entities that are about to be saved.
        /// You can mutate the dictionary and the entities in it to be what you would like gust to save.
        /// removed entities will not be included in the save result
        /// </summary>
        protected virtual void BeforeSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap)
        {
        }

        /// <summary>
        /// Called after entities are saved but before the transaction is commited (if the save is inside a transaction). 
        /// </summary>
        protected virtual void AfterSaveEntities(
            Dictionary<Type, List<EntityInfo>> saveMap,
            Dictionary<(Type, object), KeyMapping> keyMappings,
            List<EntityKey> deletedKeys)
        {

        }

        /// <summary>
        /// Override and return false in cases you want the entity to not be saved. you can also throw an exception and it
        /// will not be caught by gust (and will stop the save operation).
        /// this method is called once per entity before any of the entities are saved. if you return false the entity
        /// will simple not be added to the data context, no other adaptations happen.
        /// </summary>
        protected virtual bool BeforeSaveEntity(EntityInfo entityInfo)
        {
            return true;
        }

        public SaveResult SaveChanges(string saveBundle)
        {
            var ser = CreateJsonSerializer();

            var jSaveBundle = JObject.Parse(saveBundle);

            var saveOptions = new SaveOptions();

            if (jSaveBundle.TryGetValue("saveOptions", out var saveOptionsJson))
            {
                SaveOptions = ser.Deserialize<SaveOptions>(new JTokenReader(saveOptionsJson));
            }

            if (!jSaveBundle.TryGetValue("entities", out var entitiesJson))
            {
                throw new Exception("save bundle must contain an array of entities");
            };

            var entitySetsInfo = GetEntitySetsInfo();

            var entitesJsonArray = entitiesJson as JArray;

            var entitiesInfo = entitesJsonArray.Select(jt =>
            {
                return EntityInfoFromJsonToken(jt, ser, entitySetsInfo);

            }).ToList();

            var plannedForDeletion = entitiesInfo.Where(ei => ei.EntityState == EntityState.Deleted).ToList();

            var keyMappings = entitiesInfo
                 .Where(ei => ei.EntityState == EntityState.Added && ei.AutoGeneratedKey?.AutoGeneratedKeyType == AutoGeneratedKeyType.Identity)
                 .ToDictionary(i =>
                 {
                     return (i.Type, i.EntitySetInfo.EntityType.FindPrimaryKey().Properties.First().PropertyInfo.GetValue(i.Entity));
                 },
                 (i) =>
                 {
                     var keyValue = i.EntitySetInfo.EntityType.FindPrimaryKey().Properties.First().PropertyInfo.GetValue(i.Entity);
                     i.AutoGeneratedKey.TempValue = keyValue;
                     return new KeyMapping { TempValue = keyValue, EntityTypeName = i.Type.FullName };
                 });

            var entitiesByType = entitiesInfo.GroupBy(e => e.Type)
                .ToDictionary(i => i.Key, i => i.ToList());

            BeforeSaveEntities(entitiesByType);

            entitiesInfo = entitiesByType.SelectMany(i => i.Value).ToList();

            var entityGroupsAsList = entitiesByType
                .Select(i => new EntityGroup() { EntitiesClrType = i.Key, Entities = i.Value })
                .ToList();

            ///Order the groups from the more basic entities to the entities that depend on them
            var groupsToSave = entitySetsInfo.Select(esi =>
            {
                var group = entityGroupsAsList.FirstOrDefault(eg => eg.EntitiesClrType == esi.ClrType);
                return (esi, group);
            }).Where(i => i.group != null)
            .ToList();

            void SaveGroup(EntityGroup entityGroup, EntityState[] statesToSave)
            {
                var setInfo = entityGroup.Entities.First().EntitySetInfo;
                var entities = entityGroup.Entities.Where(ei => statesToSave.Contains(ei.EntityState)).ToList();

                /// Correct any foreign keys that point to newly added entities that has a temp id
                foreach (var fk in setInfo.EntityType.GetForeignKeys())
                {
                    var principalEntityType = fk.PrincipalEntityType.ClrType;

                    var fkProp = fk.Properties.First();
                    var fkPropInfo = fkProp.PropertyInfo;

                    entities.ForEach(ei =>
                    {
                        var keyValue = fkPropInfo.GetValue(ei.Entity);

                        if (keyMappings.TryGetValue((principalEntityType, keyValue), out var keyMapping))
                        {
                            fkPropInfo.SetValue(ei.Entity, keyMapping.RealValue);
                        }

                    });
                }

                var pkPropInfo = setInfo.EntityType.GetKeys().First().Properties.First().PropertyInfo;

                /// Set temp keys to the default value and Add entities to the ctx
                var defaultKeyValue = GetDefaultValue(pkPropInfo.PropertyType);
                entities.ForEach(ei =>
                {
                    if (ei.AutoGeneratedKey?.AutoGeneratedKeyType == AutoGeneratedKeyType.Identity)
                    {
                        pkPropInfo.SetValue(ei.Entity, defaultKeyValue);
                    }

                    if (BeforeSaveEntity(ei))
                    {
                        Context.Entry(ei.Entity).State = ei.EntityState;
                    }
                    else if (ei.EntityState == EntityState.Deleted)
                    {
                        plannedForDeletion.Remove(ei);
                    }

                });

                Context.SaveChanges();

                /// Go over the entities discovering the real pk and saving them into the keyMappings
                entities.ForEach(ei =>
                {
                    if (ei.AutoGeneratedKey?.AutoGeneratedKeyType == AutoGeneratedKeyType.Identity)
                    {
                        var pkValue = pkPropInfo.GetValue(ei.Entity);
                        keyMappings[(ei.Type, ei.AutoGeneratedKey.TempValue)].RealValue = pkValue;
                    }
                });
            }

            groupsToSave.ForEach(i =>
            {
                SaveGroup(i.group, new EntityState[] { EntityState.Added, EntityState.Modified });
            });

            /// Put the dependents first so we delete them before any entities that they depend on
            groupsToSave.Reverse();

            groupsToSave.ForEach(i =>
            {
                SaveGroup(i.group, new EntityState[] { EntityState.Deleted });
            });

            SaveWorkState = new SaveWorkState(entityGroupsAsList);

            var deletedKeys = plannedForDeletion
                .Select(ei =>
                {
                    var keyValue = ei.EntitySetInfo.EntityType.FindPrimaryKey()
                                     .Properties.First().PropertyInfo.GetValue(ei.Entity);
                    return new EntityKey(ei, keyValue);
                })
                .ToList();

            AfterSaveEntities(entitiesByType, keyMappings, deletedKeys);

            Context.Database.CurrentTransaction?.Commit();

            var entites = entitiesByType.SelectMany(entityGroup => entityGroup.Value.Select(ei => ei.Entity))
                                        .ToList();

            return new SaveResult
            {
                DeletedKeys = deletedKeys,
                KeyMappings = keyMappings.Select(i => i.Value).ToList(),
                Entities = entites,
            };
        }

        public EntityInfo EntityInfoFromJsonToken(JToken e, JsonSerializer ser = null, List<EntitySetInfo> entitySetsInfo = null)
        {
            ser = ser ?? CreateJsonSerializer();
            entitySetsInfo = entitySetsInfo ?? GetEntitySetsInfo();
            var entityVal = e as JObject;

            var aspect = entityVal.GetValue("entityAspect");
            var entityTypeName = aspect.SelectToken("entityTypeName").ToString();
            var entitySetInfo = entitySetsInfo.First(esi => esi.JsName == entityTypeName);

            Enum.TryParse<EntityState>(aspect.SelectToken("entityState").ToString(), out var entityState);
            var autoGeneratedKeyJToken = aspect.SelectToken("autoGeneratedKey");

            AutoGeneratedKey autoGeneratedKey = null;

            if (entityState == EntityState.Added && autoGeneratedKeyJToken?.HasValues == true)
            {
                var keyPropertyName = autoGeneratedKeyJToken.SelectToken("propertyName").ToString();
                Enum.TryParse<AutoGeneratedKeyType>(autoGeneratedKeyJToken.SelectToken("autoGeneratedKeyType").ToString(), out var keyGenType);
                autoGeneratedKey = new AutoGeneratedKey { PropertyName = keyPropertyName, AutoGeneratedKeyType = keyGenType };
            }

            var entity = ser.Deserialize(new JTokenReader(entityVal), entitySetInfo.ClrType);

            var unmappedValuesMap = JsonToDictionary(entityVal.SelectToken("__unmapped"));
            var originalValuesMap = JsonToDictionary(aspect.SelectToken("originalValuesMap"));

            var entityInfo = new EntityInfo(entitySetInfo, entity, entityState, autoGeneratedKey, originalValuesMap, unmappedValuesMap);

            return entityInfo;
        }

        /// <summary>
        /// Used this method to create entityInfo outside of the normal operation of the persist manager.
        /// for example if you created and saved an entity yourself and want the save result to include that entity.
        /// The "EntitySetInfo" property will be automaticly populated by the persist manager
        /// </summary>
        public EntityInfo CreateEntityInfo(object entity,
            EntityState entityState,
            AutoGeneratedKey autoGeneratedKey = null,
            Dictionary<string, object> unmappedValuesMap = null,
            Dictionary<string, object> originalValuesMap = null)
        {
            var clrType = entity.GetType();
            var entitySetsInfo = GetEntitySetsInfo();
            var entitySetInfo = entitySetsInfo.First(esi => esi.ClrType == clrType);

            return CreateEntityInfo(entitySetInfo, entity, entityState, autoGeneratedKey, unmappedValuesMap, originalValuesMap);
        }

        public EntityInfo CreateEntityInfo(EntitySetInfo entitySetInfo, object entity, EntityState entityState, AutoGeneratedKey autoGeneratedKey, Dictionary<string, object> unmappedValuesMap, Dictionary<string, object> originalValuesMap)
        {
            return new EntityInfo
            {
                Entity = entity,
                EntityState = entityState,
                EntitySetInfo = entitySetInfo,
                Type = entitySetInfo.ClrType,
                AutoGeneratedKey = autoGeneratedKey,
                UnmappedValuesMap = unmappedValuesMap,
                OriginalValuesMap = originalValuesMap
            };
        }

        bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && _ownsContext)
                {
                    Context?.Dispose();
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

    }
}
