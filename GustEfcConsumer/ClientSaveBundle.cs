﻿using Gust;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace GustEfcConsumer
{
    /// <summary>
    /// This class is the scheme of the data the client sends when saving changes.
    /// it contains helper methods for testing how gurst handles diffrent payloads
    /// The javascript client sends the data in this form.
    /// </summary>
    public class ClientSaveBundle
    {
        JsonSerializer JsonSerializer;
        List<EntityAspect> EntityAspects { get; set; } = new List<EntityAspect>();
        SaveOptions SaveOptions { get; set; } = new SaveOptions { Tag = JObject.Parse("{}") };

        public ClientSaveBundle(bool pascalCase = false)
        {
            JsonSerializer = new JsonSerializer { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

            if (!pascalCase)
            {
                JsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
        }

        public void AddEntity(EntityAspect entityAspect)
        {
            EntityAspects.Add(entityAspect);
        }

        public string ToJson()
        {
            var payload = new JObject();

            var entitiesArray = new JArray();
            foreach (var item in EntityAspects)
            {
                var jEntity = EntityAndEntityAspectToJObject(item.Entity, item);
                entitiesArray.Add(jEntity);
            }

            payload.Add("entities", entitiesArray);
            payload.Add("saveOptions", JObject.FromObject(SaveOptions, JsonSerializer));

            return payload.ToString();
        }

        /// <summary>
        /// The schame we need to create has the entity aspect object as a property of the entity itself
        /// since this cannot be done with static types we only combine the two when creating the json
        /// </summary>
        public JObject EntityAndEntityAspectToJObject(object entity, EntityAspect entityAspect)
        {
            var jEntity = JObject.FromObject(entity, JsonSerializer);
            jEntity.Add("entityAspect", JObject.FromObject(entityAspect, JsonSerializer));
            return jEntity;
        }

    }

    public class EntityGeneratedKey
    {
        public string PropertyName { get; set; } = "Id";
        public string AutoGeneratedKeyType { get; set; } = "Identity";
    }

    /// <summary>
    /// For each entity changed the client sends this scheme.
    /// Has some helper method to mutate the state for testing
    /// </summary>
    public class EntityAspect
    {
        public string EntityTypeName { get; set; }
        public string DefaultResourceName { get; set; }
        public EntityState EntityState { get; set; }

        /// <summary>
        /// The values of the updated properties before the current update (if the operation is not an update this should be null or empty)
        /// this data is comming from the client and is not verified and should not be trusted to be correct in terms of security
        /// </summary>
        public Dictionary<string, object> OriginalValuesMap { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// When an entity is being created this are the instractions for creating the id or using the client provided id
        /// </summary>
        public EntityGeneratedKey AutoGeneratedKey { get; set; }

        internal object Entity { get; }

        public EntityAspect(object entity, EntityState state)
        {
            Entity = entity;
            var type = entity.GetType();
            EntityTypeName = $"{type.Name}:#{type.Namespace}";
            DefaultResourceName = type.Name + "s";
            EntityState = state;

            AutoGeneratedKey = new EntityGeneratedKey();
        }

        /// <summary>
        /// Changes the value of the property belonging to the entity 
        /// and adds the old value to the original values map if this is the first change of that
        /// property
        /// </summary>
        public void ChangeValue(string prop, object value)
        {
            var originalVal = Entity.GetType().GetProperty(prop).GetValue(Entity);
            Entity.GetType().GetProperty(prop).SetValue(Entity, value, null);
            if (!OriginalValuesMap.ContainsKey(prop))
            {
                OriginalValuesMap[prop] = originalVal;
            }
        }
    }
}

