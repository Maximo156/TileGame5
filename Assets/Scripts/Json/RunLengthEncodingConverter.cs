using Newtonsoft.Json;
using System;
using Unity.Collections;
using UnityEngine;

public class NativeSliceRunLengthEncodingConverter<T> : JsonConverter<NativeSlice<T>> where T : unmanaged
{
    public override NativeSlice<T> ReadJson(JsonReader reader, Type objectType, NativeSlice<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        int index = 0;
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            var count = serializer.Deserialize<int>(reader);
            reader.Read();
            var val = serializer.Deserialize<T>(reader);
            for(int i = 0; i < count; i++)
            {
                existingValue[index++] = val;
            }
        }
        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, NativeSlice<T> value, JsonSerializer serializer)
    {
        var length = value.Length;

        writer.WriteStartArray();
        if (length > 0)
        {
            T cur = value[0];
            int count = 1;
            for (int i = 1; i < value.Length; i++)
            {
                var next = value[i];
                if(next.Equals(cur))
                {
                    count++;
                    continue;
                }
                writer.WriteValue(count);
                writer.WriteValue(cur);
                cur = next;
                count = 1;
            }
            writer.WriteValue(count);
            writer.WriteValue(cur);
        }
        writer.WriteEndArray();
    }
}
