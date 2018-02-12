using Gust.Keys;
using System.Collections.Generic;

namespace Gust
{
    /// <summary>
    /// The return type of the response sent to the client after a save request.
    /// </summary>
    public class SaveResult
    {
        /// <summary>
        /// All entities that were updated, created or deleted. 
        /// </summary>
        public List<object> Entities;

        /// <summary>
        /// All changes that were made to any of the keys belonging to any of the entities.
        /// Most commonly the mapping will be between the temporary id the client used and the id the
        /// DB gave the entity when it was created
        /// </summary>
        public List<KeyMapping> KeyMappings;

        /// <summary>
        /// The keys of all the entities that were deleted as a result of the save operation
        /// </summary>
        public List<EntityKey> DeletedKeys;

        /// <summary>
        /// Any errors that were encountered while performing the save operation
        /// </summary>
        public List<object> Errors;
    }
}