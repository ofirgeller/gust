namespace Gust.Keys
{
    public class EntityKey
    {
        public EntityKey() { }
        public EntityKey(object entity, object key)
        {
            var type = entity.GetType();
            EntityTypeName = type.Name + ":#" + type.Namespace;
            KeyValue = key;
        }

        /// <summary>
        /// Needs to be in the form of "Form:#LZDataBase.Model"
        /// example: "Form:#LZDataBase.Model" where "LZDataBase.Model" is the namespace and "Form" is the type (class name)
        /// </summary>
        public string EntityTypeName;

        public object KeyValue;
    }
}