using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gust
{
    /// <summary>
    /// TODO: check that we actualy respect these settings 
    /// </summary>
    public class GustConfig
    {
        public static GustConfig Default { get; set; } = new GustConfig();

        public GustConfig()
        {
            _jsonSerializerSettings = CreateJsonSerializerSettings();
        }

        public static JsonSerializerSettings UpdateWithDefaults(JsonSerializerSettings ss)
        {
            ss.NullValueHandling = NullValueHandling.Include;
            ss.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            ss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            ss.TypeNameHandling = TypeNameHandling.Objects;
            ss.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;

            // Hack is for the issue described in this post:
            // http://stackoverflow.com/questions/11789114/internet-explorer-json-net-javascript-date-and-milliseconds-issue
            ss.Converters.Add(new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy-MM-dd\\THH:mm:ss.fffK"
            });

            // Needed because JSON.NET does not natively support I8601 Duration formats for TimeSpan
            ss.Converters.Add(new TimeSpanConverter());
            ss.Converters.Add(new StringEnumConverter());

            // Default is DateTimeZoneHandling.RoundtripKind - you can change that here.
            // ss.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

            return ss;
        }

        public JsonSerializerSettings GetJsonSerializerSettings()
        {
            return _jsonSerializerSettings;
        }

        /// <summary>
        /// Override to use a specialized JsonSerializer implementation.
        /// </summary>
        protected virtual JsonSerializerSettings CreateJsonSerializerSettings()
        {
            var jsonSerializerSettings = new JsonSerializerSettings();
            return UpdateWithDefaults(jsonSerializerSettings);
        }

        public JsonSerializerSettings GetJsonSerializerSettingsForSave()
        {
            if (_jsonSerializerSettingsForSave == null)
            {
                _jsonSerializerSettingsForSave = CreateJsonSerializerSettingsForSave();
            }
            return _jsonSerializerSettingsForSave;
        }

        /// <summary>
        /// Override to use a specialized JsonSerializer implementation for saving.
        /// Base implementation uses CreateJsonSerializerSettings() then sets TypeNameHandling to None
        /// </summary>
        protected virtual JsonSerializerSettings CreateJsonSerializerSettingsForSave()
        {
            var settings = CreateJsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.None;
            return settings;
        }

        /// <summary>
        /// Returns TransactionSettings.Default.  Override to return different settings.
        /// </summary>
        /// <returns></returns>
        public virtual TransactionSettings GetTransactionSettings()
        {
            return new TransactionSettings();
        }

        JsonSerializerSettings _jsonSerializerSettings = null;
        JsonSerializerSettings _jsonSerializerSettingsForSave = null;
    }
}