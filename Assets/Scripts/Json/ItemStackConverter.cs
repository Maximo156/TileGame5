using Newtonsoft.Json;
using System;
using UnityEngine;

public class ItemStackConverter : JsonConverter<ItemStack>
{
    public override ItemStack ReadJson(JsonReader reader, Type objectType, ItemStack existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            return null;
        }
        string id = null;
        int count = -1;
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = reader.Value.ToString();
                reader.Read();

                switch (propertyName)
                {
                    case nameof(existingValue.Item):
                        id = reader.Value.ToString();
                        break;
                    case nameof(existingValue.Count):
                        count = serializer.Deserialize<int>(reader);
                        break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }
        }

        if (id == null || count == -1)
        {
            Debug.LogError("Invalid item encoding");
            return null;
        }

        return new ItemStack(ItemRepository.GetItem(id), count);
    }

    public override void WriteJson(JsonWriter writer, ItemStack value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Item));
        writer.WriteValue(value.Item.Identifier);

        writer.WritePropertyName(nameof(value.Count));
        writer.WriteValue(value.Count);

        writer.WriteEndObject();
    }
}
