using System;
using System.Collections.Generic;

namespace Gust
{
    /// <summary>
    /// A group of entities having the same clr type
    /// </summary>
    public class EntityGroup
    {
        public Type EntitiesClrType;
        public List<EntityInfo> Entities;
    }
}