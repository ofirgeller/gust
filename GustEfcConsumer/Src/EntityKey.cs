
namespace GustEfc.Src
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
        public string EntityTypeName;
        public object KeyValue;
    }
}