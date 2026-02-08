using NativeRealm;
using Newtonsoft.Json;
using System;
using UnityEngine;

public class ChunkDataConverter : JsonConverter<ChunkData>
{
    public override ChunkData ReadJson(JsonReader reader, Type objectType, ChunkData existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        serializer.Populate(reader, existingValue);
        return existingValue;
    }

    public override bool CanWrite => false;

    // This method will not be called by the serializer
    public override void WriteJson(JsonWriter writer, ChunkData value, JsonSerializer serializer)
    {
        throw new NotImplementedException("This converter is only for reading (deserialization).");
    }
}
