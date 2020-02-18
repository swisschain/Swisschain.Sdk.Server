using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    internal static class JExtensions
    {
        public static IEnumerable<JValue> GetLeafValues(this JToken jToken)
        {
            if (jToken is JValue jValue)
            {
                yield return jValue;
            }
            else if (jToken is JArray jArray)
            {
                foreach (var result in GetLeafValuesFromJArray(jArray))
                {
                    yield return result;
                }
            }
            else if (jToken is JProperty jProperty)
            {
                foreach (var result in GetLeafValuesFromJProperty(jProperty))
                {
                    yield return result;
                }
            }
            else if (jToken is JObject jObject)
            {
                foreach (var result in GetLeafValuesFromJObject(jObject))
                {
                    yield return result;
                }
            }
        }

        private static IEnumerable<JValue> GetLeafValuesFromJArray(JArray jArray)
        {
            return jArray.SelectMany(GetLeafValues);
        }

        private static IEnumerable<JValue> GetLeafValuesFromJProperty(JProperty jProperty)
        {
            return GetLeafValues(jProperty.Value);
        }

        private static IEnumerable<JValue> GetLeafValuesFromJObject(JObject jObject)
        {
            return jObject.Children().SelectMany(GetLeafValues);
        }
    }
}