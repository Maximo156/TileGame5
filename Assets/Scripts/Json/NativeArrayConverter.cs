using Newtonsoft.Json;
using System;
using Unity.Collections;
using UnityEngine;

public class NativeArrayConverter<T> : JsonConverter<NativeArray<T>> where T : unmanaged
{
    public override NativeArray<T> ReadJson(JsonReader reader, Type objectType, NativeArray<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, NativeArray<T> value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
