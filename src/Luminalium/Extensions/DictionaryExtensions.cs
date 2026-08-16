namespace Luminalium.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> enumerable) where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>();
        foreach (var kvp in enumerable)
        {
            dictionary[kvp.Key] = kvp.Value;
        }
        return dictionary;
    }
}