using Newtonsoft.Json;
using OriBot.Storage;

public static class JSON
{
    public static string stringify(dynamic obj)
    {
        if (obj is JObject)
        {
            return obj.ToString();
        }
        return JsonConvert.SerializeObject(obj, Formatting.None);
    }
    public static dynamic parse(string json)
    {
        return JsonConvert.DeserializeObject(json);
    }
}