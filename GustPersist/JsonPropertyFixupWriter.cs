using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;

namespace Gust.Persist
{
    public class JsonPropertyFixupWriter : JsonTextWriter
    {
        public JsonPropertyFixupWriter(TextWriter textWriter)
          : base(textWriter)
        {
            _isDataType = false;
        }

        public override void WritePropertyName(string name)
        {
            if (name.StartsWith("@"))
            {
                name = name.Substring(1);
            }
            name = ToCamelCase(name);
            _isDataType = name == "type";
            base.WritePropertyName(name);
        }

        public override void WriteValue(string value)
        {
            if (_isDataType && !value.StartsWith("Edm."))
            {
                base.WriteValue("Edm." + value);
            }
            else
            {
                base.WriteValue(value);
            }
        }

        static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
            {
                return s;
            }
            var str = char.ToLower(s[0], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (s.Length > 1)
            {
                str = str + s.Substring(1);
            }
            return str;
        }

        bool _isDataType;
    }
}