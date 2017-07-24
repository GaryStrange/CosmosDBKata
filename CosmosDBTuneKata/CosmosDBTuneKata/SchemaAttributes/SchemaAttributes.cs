using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.Schema
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class IndexAttribute : Attribute
    {

        public bool IsIncluded { get; set; }
        public bool HasEqualtiyQueries { get; set; }

        public bool HasRangeOrOrderByQueries { get; set; }
        private static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetIndexedProperties(Type schemaType)
        {
            return schemaType.GetProperties()
                .Where(prop => prop.IsDefined(typeof(IndexAttribute), false));
        }
        //public static String[] GetPaths(Type schemaType)
        //{
        //    return GetIndexedProperties(schemaType)
        //        .Select(prop => prop.Name)
        //        .ToArray();
        //}

        public static Dictionary<String, IndexAttribute> GetPaths(Type schemaType)
        {
            return GetIndexedProperties(schemaType)
                .ToDictionary(
                prop => prop.Name
                , prop => (IndexAttribute)prop.GetCustomAttributes(typeof(IndexAttribute), false).FirstOrDefault()
                );
        }

    }
}
