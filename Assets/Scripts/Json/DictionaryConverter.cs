using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DictionaryConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
{
    public override Dictionary<TKey, TValue> ReadJson(JsonReader reader, Type objectType, Dictionary<TKey, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if(existingValue == null)
        {
            existingValue = new Dictionary<TKey, TValue>();
        }
        while(reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) break;
            var kvp = serializer.Deserialize<KeyValuePair<TKey, TValue>>(reader);
            existingValue.Add(kvp.Key, kvp.Value);
        }
        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializer serializer)
    {
        writer.WriteStartArray();

        foreach(var kvp in value)
        {
            serializer.Serialize(writer, kvp);
        }

        writer.WriteEndArray();
    }
}
