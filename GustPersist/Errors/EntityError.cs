using System;

namespace Gust.Persist
{
    public class EntityError
    {
        public string ErrorName;
        public string EntityTypeName;
        public object[] KeyValues;
        public string PropertyName;
        public string ErrorMessage;
    }
}