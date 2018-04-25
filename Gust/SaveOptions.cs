using Newtonsoft.Json.Linq;

namespace Gust
{
    /// <summary>
    /// Extra options the client can send along with the save payload. currently Tag is used as a flag to "force" save 
    /// past some data integrity protections.
    /// TODO:strongly type it according to actual usege 
    /// </summary>
    public class SaveOptions
    {
        public JObject Tag { get; set; }
    }
}