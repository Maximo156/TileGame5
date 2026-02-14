using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters.Math;
using UnityEngine;

public class DefaultJsonSettings
{
    public static JsonSerializerSettings settings;
    static DefaultJsonSettings()
    {
        settings = new JsonSerializerSettings();
        settings.Converters.Add(new ChunkConverter());
        settings.Converters.Add(new Vector2IntConverter());
        settings.Converters.Add(new DictionaryConverter<Vector2Int, BlockState>());
        settings.Converters.Add(new DictionaryConverter<Vector2Int, BlockItemStack>());
        settings.TypeNameHandling = TypeNameHandling.Auto;
    }
}
