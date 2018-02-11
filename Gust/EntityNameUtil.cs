using System;
using System.Collections.Generic;
using System.Linq;

namespace Gust
{
    public static class EntityNameUtil
    {
        public static string ShortTypeNameFromLongName(string longName)
        {
            return longName.Split('.').Last();
        }

        public static string ToCamelCase(string name)
        {
            return Char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        public static string JsTypeNameFromType(Type type)
        {
            return type.Name + ":#" + type.Namespace;
        }

        /// <summary>
        ///  We do not include Y since it is not allways a vowel
        /// </summary>
        static List<char> EnglishVowels = new List<char> { 'A', 'E', 'I', 'O', 'U' };

        /// <summary>
        /// Correct only for the simple cases. words like "person" => people would need
        /// some kind of override
        /// </summary>
        public static string Pluralize(string singular)
        {
            if (string.IsNullOrWhiteSpace(singular))
            {
                return singular;
            }

            if (singular.EndsWith("s"))
            {
                return singular + "es";
            }

            if (singular.EndsWith("y"))
            {
                var lastCharIndex = singular.Length - 1;

                /// boy => boys
                if (lastCharIndex > 0 && EnglishVowels.Contains(singular.ElementAtOrDefault(lastCharIndex - 1)))
                {
                    return singular + "s";
                }

                ///  city => cities
                return singular.Substring(0, lastCharIndex) + "ies";
            }

            return singular + "s";
        }
    }
}
