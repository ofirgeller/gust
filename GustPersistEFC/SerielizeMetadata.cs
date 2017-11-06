using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gust.PersistEFC
{
    public class MetadataSerielizer
    {
        static JsonSerializerSettings DefaultSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public static string ToJson(MetadataExtractor2.Metadata metadata, bool pretty = false)
        {
            var settings = DefaultSettings();
            if (pretty)
            {
                settings.Formatting = Formatting.Indented;
            }

            return JsonConvert.SerializeObject(metadata, settings);
        }

    }
}
