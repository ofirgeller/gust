using Gust.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Gust.Persist
{
    /// <summary>
    /// TODO: this class needs a complete rewrite. It seems to be doing things related to feature 
    /// detection and Assembly probing. plus might be related to the "Activator" feature where you
    /// can geet breeze to work along side other web api controllers with minimal configurations. 
    /// this was IMHO never a good idea. much better to give good documentation on how to do it
    /// then provide magic bootstraping code.
    /// </summary>
    public class GustConfig
    {
        static GustConfig _default;

        public static GustConfig Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new GustConfig();
                }

                return _default;
            }
        }

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

        public static ReadOnlyCollection<Assembly> ProbeAssemblies
        {
            get
            {
                if (__assemblyCount == 0 || __assemblyCount != __assemblyLoadedCount)
                {
                    // Cache the ProbeAssemblies.
                    __probeAssemblies = new ReadOnlyCollection<Assembly>(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsFrameworkAssembly(a)).ToList());
                    __assemblyCount = __assemblyLoadedCount;
                }
                return __probeAssemblies;
            }
        }

        static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Interlocked.Increment(ref __assemblyLoadedCount);
        }
        static ReadOnlyCollection<Assembly> __probeAssemblies;
        static int __assemblyCount = 0;
        static int __assemblyLoadedCount = 0;

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

        public static bool IsFrameworkAssembly(Assembly assembly)
        {
            var fullName = assembly.FullName;
            if (fullName.StartsWith("Microsoft.")
                || fullName.StartsWith("EntityFramework")
                || fullName.StartsWith("NHibernate"))
            {
                return true;
            }

            var attrs = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false).OfType<AssemblyProductAttribute>();
            var attr = attrs.FirstOrDefault();
            if (attr == null)
            {
                return false;
            }
            var productName = attr.Product;
            return FrameworkProductNames.Any(nm => productName.StartsWith(nm));
        }

        protected static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (Exception ex)
            {
                var msg = string.Empty;
                if (ex is ReflectionTypeLoadException)
                {
                    msg = ((ReflectionTypeLoadException)ex).LoaderExceptions.ToAggregateString(". ");
                }
                Trace.WriteLine("Breeze probing: Unable to execute Assembly.GetTypes() for "
                  + assembly.ToString() + "." + msg);

                return new Type[] { };
            }
        }

        protected static readonly List<string> FrameworkProductNames = new List<string> {
      "Microsoft®",
      "Microsoft (R)",
      "Microsoft ASP.",
      "System.Net.Http",
      "Json.NET",
      "Antlr3.Runtime",
      "Iesi.Collections",
      "WebGrease",
      "Breeze.ContextProvider",
      "Breeze.Core",
      "Breeze.AspNetCore"
    };

        /// <summary>
        /// Returns TransactionSettings.Default.  Override to return different settings.
        /// </summary>
        /// <returns></returns>
        public virtual TransactionSettings GetTransactionSettings()
        {
            return TransactionSettings.Default;
        }

        JsonSerializerSettings _jsonSerializerSettings = null;
        JsonSerializerSettings _jsonSerializerSettingsForSave = null;
    }
}