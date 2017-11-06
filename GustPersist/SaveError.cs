using System.Collections.Generic;
using System.Linq;

namespace Gust.Persist
{
    public class SaveError
    {
        public SaveError(IEnumerable<EntityError> entityErrors)
        {
            EntityErrors = entityErrors.ToList();
        }

        public SaveError(string message, IEnumerable<EntityError> entityErrors)
        {
            Message = message;
            EntityErrors = entityErrors.ToList();
        }

        public string Message { get; protected set; }
        public List<EntityError> EntityErrors { get; protected set; }
    }
}