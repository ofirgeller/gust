using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Gust.Persist
{
    public class EntityErrorsException : Exception
    {
        public EntityErrorsException(IEnumerable<EntityError> entityErrors)
        {
            EntityErrors = entityErrors.ToList();
            StatusCode = HttpStatusCode.Forbidden;
        }

        public EntityErrorsException(String message, IEnumerable<EntityError> entityErrors)
          : base(message)
        {
            EntityErrors = entityErrors.ToList();
            StatusCode = HttpStatusCode.Forbidden;
        }


        public HttpStatusCode StatusCode { get; set; }
        public List<EntityError> EntityErrors { get; protected set; }
    }
}