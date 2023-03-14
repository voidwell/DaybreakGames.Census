using System.Text.Json;

namespace DaybreakGames.Census
{
    public static class JsonElementExtensions
    {
        public static JsonElement TryGetValue(this JsonElement json, string name)
        {
            if (json.ValueKind == JsonValueKind.Undefined)
            {
                return default;
            }

            return json.TryGetProperty(name, out JsonElement value) ? value : default;
        }

        public static string TryGetString(this JsonElement json, string name)
        {
            JsonElement value = json.TryGetValue(name);
            return value.ValueKind == JsonValueKind.Undefined ? null : value.ToString();
        }
    }
}
