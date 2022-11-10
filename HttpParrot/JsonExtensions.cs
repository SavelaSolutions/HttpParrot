using Newtonsoft.Json;

namespace HttpParrot
{
    internal static class JsonExtensions
    {
        /// <summary>
        /// Does a non-destructive prettify of a JSON string (that is, does not change the formatting of the actual values,
        /// which might otherwise result in loss of number precision).
        /// </summary>
        /// <param name="json">The JSON string to prettify</param>
        /// <returns>The non-destructive prettified JSON string</returns>
        public static string NonDestructiveJsonPrettify(this string json)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                FloatParseHandling = FloatParseHandling.Decimal, // Do not change number of decimals after decimal point
                DateParseHandling = DateParseHandling.None // Do not change the time zone
            };
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json, settings), settings);
        }
    }
}