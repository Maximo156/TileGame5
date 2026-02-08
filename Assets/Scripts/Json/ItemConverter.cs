using Newtonsoft.Json;
using System;
using UnityEngine;

public class ItemConverter : JsonConverter<Item>
{
    public override Item ReadJson(JsonReader reader, Type objectType, Item existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return ItemRepository.GetItem(reader.Value.ToString());
    }

    public override void WriteJson(JsonWriter writer, Item value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Identifier);
    }
}
