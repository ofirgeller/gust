namespace Gust.Keys
{
    public class KeyMapping
    {
        /// <summary>
        /// Must be in the form of "namespace.type"
        /// example: "LZDataBase.Model.Form" where "LZDataBase.Model" is the namespace and "Form" is the class name (the type)
        /// </summary>
        public string EntityTypeName;

        public object TempValue;

        public object RealValue;
    }
}