using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GustEfc
{
    public static class Util
    {
        public static string ShortTypeNameFromlongName(string longName)
        {
            return longName.Split('.').Last();
        }

        public static string JsTypeNameFromType(Type type)
        {
            return type.Name + ":#" + type.Namespace;
        }
    }
}
