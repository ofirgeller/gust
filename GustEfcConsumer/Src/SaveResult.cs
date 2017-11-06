using System.Collections.Generic;

namespace GustEfc.Src
{
    /// <summary>
    /// Types returned to javascript as Json.
    /// </summary>
    public class SaveResult
    {
        public List<object> Entities;
        public List<KeyMapping> KeyMappings;
        public List<EntityKey> DeletedKeys;
        public List<object> Errors;
    }
}