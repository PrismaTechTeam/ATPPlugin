using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ATPApi.Repositories
{
    public static class JsonFormat
    {
        public static string Pretty(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json ?? string.Empty;
            try
            {
                JToken token = JToken.Parse(json);
                return token.ToString(Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        public static string PrettyFromObject(object obj)
        {
            if (obj == null) return string.Empty;
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
