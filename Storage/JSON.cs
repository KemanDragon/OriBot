using Newtonsoft.Json;

public static class JSON {
    public static string stringify(dynamic obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.None);
    }

    public static dynamic parse(string json)
    {
        return JsonConvert.DeserializeObject(json);
    }
}