using System.Text.Json;

namespace DaybreakGames.Census
{
    public static class JsonElementExtensions
    {
        public static T GetPropertyValue<T>(this JsonElement element, string propertyName)
            where T: class
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                return value.Deserialize<T>();
            }

            return null;
        }
    }
}
