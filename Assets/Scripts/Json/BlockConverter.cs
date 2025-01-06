using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class BlockConverter : JsonConverter<Block>
{
    public override Block ReadJson(JsonReader reader, Type objectType, Block existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var id = serializer.Deserialize<string>(reader);
        if(id != null && SORepository.blocks.TryGetValue(id, out var block))
        {
            return block;
        }
        return null;
    }

    public override void WriteJson(JsonWriter writer, Block value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Identifier);
    }
}