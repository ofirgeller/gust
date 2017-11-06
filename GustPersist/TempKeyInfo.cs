﻿using System;
using System.Reflection;

namespace Gust.Persist
{
    /// <summary>
    /// instances of this sent to KeyGenerator
    /// </summary>
    public class TempKeyInfo
    {
        public TempKeyInfo(EntityInfo entityInfo)
        {
            _entityInfo = entityInfo;
        }
        public object Entity => _entityInfo.Entity;
        public object TempValue => _entityInfo.AutoGeneratedKey.TempValue;
        public object RealValue
        {
            get => _entityInfo.AutoGeneratedKey.RealValue;
            set => _entityInfo.AutoGeneratedKey.RealValue = value;
        }

        public PropertyInfo Property => _entityInfo.AutoGeneratedKey.Property;

        EntityInfo _entityInfo;

    }
}