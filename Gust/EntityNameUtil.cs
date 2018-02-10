using System;
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
    }
}
