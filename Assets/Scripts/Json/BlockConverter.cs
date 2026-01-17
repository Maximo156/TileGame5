using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using BlockDataRepos;

public class BlockConverter : JsonConverter<Block>
{
    public override Block ReadJson(JsonReader reader, Type objectType, Block existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var id = serializer.Deserialize<ushort>(reader);
        if(BlockDataRepo.TryGetBlock(id, out Block block))
        {
            return block;
        }
        return null;
    }

    public override void WriteJson(JsonWriter writer, Block value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Id);
    }
}