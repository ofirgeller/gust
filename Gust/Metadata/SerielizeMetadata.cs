using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Gust.Metadata
{
    public class MetadataSerielizer
    {
        class CamelCaseExceptDictionaryKeysResolver : CamelCasePropertyNamesContractResolver
        {
            protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
            {
                var contract = base.CreateDictionaryContract(objectType);

                contract.DictionaryKeyResolver = propertyName => propertyName;

                return contract;
            }
        }

        static JsonSerializerSettings DefaultSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCaseExceptDictionaryKeysResolver(),
                NullValueHandling = NullValueHandling.Ignore,                 
            };
        }

        public static string ToJson(MetadataExtractor.Metadata metadata, bool pretty = false)
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
