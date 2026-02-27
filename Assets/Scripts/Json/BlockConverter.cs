using BlockDataRepos;
using ComposableBlocks;
using Newtonsoft.Json;
using System;

public class BlockConverter : JsonConverter<Block>
{
    public override Block ReadJson(JsonReader reader, Type objectType, Block existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return BlockDataRepo.GetBlock<Block>(serializer.Deserialize<ushort>(reader));
    }

    public override void WriteJson(JsonWriter writer, Block value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Id);
    }
}
