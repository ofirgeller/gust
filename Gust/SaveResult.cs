using Gust.Keys;
using System.Collections.Generic;

namespace Gust
{
    /// <summary>
    /// The return type of the response sent to the client after a save request.
    /// </summary>
    public class SaveResult
    {
        public List<object> Entities;
        public List<KeyMapping> KeyMappings;
        public List<EntityKey> DeletedKeys;
        public List<object> Errors;
    }
}