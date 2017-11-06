using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;

namespace GustEfc.Src
{
    /// <summary>
    /// Metadata about a type of entity
    /// </summary>
    public class EntitySetInfo
    {
        public EntitySetInfo(string jsName, Type clrType, IEntityType entityType)
        {
            JsName = jsName;
            ClrType = clrType;
            EntityType = entityType;
        }

        public string JsName { get; }

        public Type ClrType { get; }

        public IEntityType EntityType { get; }
    }
}
