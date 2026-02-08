using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class ChunkConverter : JsonConverter<Chunk>
{
    public override Chunk ReadJson(JsonReader reader, Type objectType, Chunk existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            return null;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = reader.Value.ToString();
                reader.Read();

                switch (propertyName)
                {
                    case nameof(existingValue.data):
                        serializer.Populate(reader, existingValue.data);
                        break;
                    case nameof(existingValue.BlockStates):
                        existingValue.BlockStates = serializer.Deserialize<Dictionary<Vector2Int, BlockState>>(reader);
                        break;
                    case nameof(existingValue.BlockItems):
                        existingValue.BlockItems = serializer.Deserialize<Dictionary<Vector2Int, BlockItemStack>>(reader);
                        break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }
        }

        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, Chunk value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(value.data));
        serializer.Serialize(writer, value.data);
        writer.WritePropertyName(nameof(value.BlockStates));
        serializer.Serialize(writer, value.BlockStates);
        writer.WritePropertyName(nameof(value.BlockItems));
        serializer.Serialize(writer, value.BlockItems);
        writer.WriteEndObject();
    }
}
