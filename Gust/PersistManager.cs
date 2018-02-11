using Gust.Configs;
using Gust.Keys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static Gust.EntityNameUtil;

namespace Gust
{
    public partial class PersistManager<T> where T : DbContext, new()
    {
        public T Context { get; }

        static IModel _model;
        static IModel GetModel(T ctx)
        {
            if (_model == null)
            {
                _model = ctx.Model;
            }

            return _model;
        }

        protected SaveWorkState SaveWorkState { get; private set; }
        protected JsonSerializer JsonSerializer { get; private set; }
        public SaveOptions SaveOptions { get; set; }
        public IsolationLevel? TransactionIsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        bool _ownsConnection;
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
                var entityInfo = new EntitySetInfo(jsName, nextToSave.et.ClrType, nextToSave.et);
                entitiesInfo.Add(entityInfo);
            }

            return entitiesInfo;
        }

        public PersistManager(T context = null, bool ownsConnection = true)
        {
            Context = context ?? CreateContext();
            _ownsConnection = ownsConnection;
        }

        public virtual T CreateContext()
        {
            var ctx = new T();
            return ctx;
        }

        protected JsonSerializer CreateJsonSerializer()
        {
            var serializerSettings = GustConfig.Default.GetJsonSerializerSettingsForSave();
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            return jsonSerializer;
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

        object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }

            return null;
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

            var entitySetsInfo = GetEntitySetsInfo(Context);

            var entitesJsonArray = entitiesJson as JArray;

            var entitiesInfo = entitesJsonArray.Select(e =>
            {
                var entityVal = e as JObject;

                var aspect = entityVal.GetValue("entityAspect");
                var entityTypeName = aspect.SelectToken("entityTypeName").ToString();
                var entitySetInfo = entitySetsInfo.First(esi => esi.JsName == entityTypeName);

                Enum.TryParse<EntityState>(aspect.SelectToken("entityState").ToString(), out var entityState);
                var autoGeneratedKeyJToken = aspect.SelectToken("autoGeneratedKey");

                AutoGeneratedKey autoGeneratedKey = null;

                if (entityState == EntityState.Added && autoGeneratedKeyJToken != null)
                {
                    var keyPropertyName = autoGeneratedKeyJToken.SelectToken("propertyName").ToString();
                    Enum.TryParse<AutoGeneratedKeyType>(autoGeneratedKeyJToken.SelectToken("autoGeneratedKeyType").ToString(), out var keyGenType);
                    autoGeneratedKey = new AutoGeneratedKey { PropertyName = keyPropertyName, AutoGeneratedKeyType = keyGenType };
                }

                var entity = ser.Deserialize(new JTokenReader(entityVal), entitySetInfo.ClrType);

                var unmappedValuesMap = JsonToDictionary(entityVal.SelectToken("__unmapped"));
                var originalValuesMap = JsonToDictionary(entityVal.SelectToken("originalValuesMap"));

                var entityInfo = new EntityInfo
                {
                    Entity = entity,
                    EntityState = entityState,
                    EntitySetInfo = entitySetInfo,
                    Type = entitySetInfo.ClrType,
                    AutoGeneratedKey = autoGeneratedKey,
                    UnmappedValuesMap = unmappedValuesMap,
                    OriginalValuesMap = originalValuesMap
                };

                return entityInfo;

            }).ToList();

            var planedForDeletion = entitiesInfo.Where(ei => ei.EntityState == EntityState.Deleted).ToList();

            var keyMappings = entitiesInfo
                .Where(ei => ei.EntityState == EntityState.Added && ei.AutoGeneratedKey.AutoGeneratedKeyType == AutoGeneratedKeyType.Identity)
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

            var entityGroups = entitiesInfo.GroupBy(e => e.Type)
                .Select(g => new EntityGroup() { EntitiesClrType = g.First().Type, Entities = g.ToList() })
                .ToList();

            var groupsToSave = entitySetsInfo.Select(esi =>
            {
                var group = entityGroups.FirstOrDefault(eg => eg.EntitiesClrType == esi.ClrType);
                return (esi, group);
            }).Where(i => i.group != null)
            .ToList();

            void SaveGroup(EntityGroup entityGroup)
            {
                var setInfo = entityGroup.Entities.First().EntitySetInfo;

                /// Correct any foreign keys that point to newly added entities that has a temp id
                foreach (var fk in setInfo.EntityType.GetForeignKeys())
                {
                    var principalEntityType = fk.PrincipalEntityType.ClrType;

                    var fkProp = fk.Properties.First();
                    var fkPropInfo = fkProp.PropertyInfo;

                    entityGroup.Entities.ForEach(ei =>
                    {
                        var keyValue = fkPropInfo.GetValue(ei.Entity);

                        if (keyMappings.TryGetValue((principalEntityType, keyValue), out var keyMapping))
                        {
                            fkPropInfo.SetValue(ei.Entity, keyMapping.RealValue);
                        }

                    });
                }

                var pkPropInfo = setInfo.EntityType.GetKeys().First().Properties.First().PropertyInfo;

                /// Set temp keys to the default value
                /// Add entities to the ctx
                var defaultKeyValue = GetDefaultValue(pkPropInfo.PropertyType);
                entityGroup.Entities.ForEach(ei =>
                {
                    if (ei.AutoGeneratedKey?.AutoGeneratedKeyType == AutoGeneratedKeyType.Identity)
                    {
                        pkPropInfo.SetValue(ei.Entity, defaultKeyValue);
                    }

                    Context.Entry(ei.Entity).State = ei.EntityState;
                });

                Context.SaveChanges();

                /// Go over the entities discovering the real pk and saving them into the keyMappings
                entityGroup.Entities.ForEach(ei =>
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
                SaveGroup(i.group);
            });

            SaveWorkState = new SaveWorkState(entityGroups);

            var deletedKeys = planedForDeletion
                .Select(ei =>
                {
                    var keyValue = ei.EntitySetInfo.EntityType.FindPrimaryKey()
                                     .Properties.First().PropertyInfo.GetValue(ei.Entity);
                    return new EntityKey(ei, keyValue);
                })
                .ToList();

            return new SaveResult
            {
                DeletedKeys = deletedKeys,
                KeyMappings = keyMappings.Select(i => i.Value).ToList(),
                Entities = entitiesInfo.Select(i => i.Entity).ToList(),
            };
        }
    }
}
