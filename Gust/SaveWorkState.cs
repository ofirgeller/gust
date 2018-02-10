using Gust.Errors;
using Gust.Keys;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gust
{
    public class SaveWorkState
    {
        public SaveWorkState(List<EntityGroup> entitiesArray)
        {
            EntityInfoGroups = entitiesArray;
            SaveMap = EntityInfoGroups.ToDictionary(i => i.EntitiesClrType);
        }

        protected List<EntityGroup> EntityInfoGroups;
        public Dictionary<Type, EntityGroup> SaveMap { get; set; }
        public List<KeyMapping> KeyMappings;
        public List<EntityError> EntityErrors;
        public bool WasUsed { get; internal set; }
    }
}