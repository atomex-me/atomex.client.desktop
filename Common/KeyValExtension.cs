using System.Collections.Generic;
using System.Linq;

namespace Atomex.Client.Desktop.Common
{
    public static class KeyValExtension
    {
        public static T GetValueByKey<T>(IEnumerable<KeyValuePair<string, T>> collection, string value)
        {
            return collection
                .Where(kv => kv.Key == value)
                .ToList()
                [0].Value;
        }

        public static string GetKeyByValue<T>(IEnumerable<KeyValuePair<string, T>> collection, T value)
        {
            return collection.Where(kv => kv.Value.Equals(value))
                .ToList()
                [0].Key;
        }
    }
}