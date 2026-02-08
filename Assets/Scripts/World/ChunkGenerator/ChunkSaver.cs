using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NativeRealm;
using System;
using Unity.Collections;
using Unity.Mathematics;
using System.Xml.Xsl;
using Unity.Jobs;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class ChunkSaver
{
    static JsonSerializerSettings settings;
    static ChunkSaver()
    {
        settings = new JsonSerializerSettings();
        settings.Converters.Add(new ChunkDataConverter());
        settings.Converters.Add(new ChunkConverter());
    }

    static string DirectoryPath(string realmId) => Path.Join(ChunkManager.DataPath, realmId);

    static string ChunkPath(string realmId, Vector2Int ChunkWorldPosition)
    {
        return Path.Join(DirectoryPath(realmId), $"Chunk-{ChunkWorldPosition.x}x{ChunkWorldPosition.y}");
    }

    public static bool HasSavedVersion(string realmId, Vector2Int ChunkWorldPosition)
    {
        return File.Exists(ChunkPath(realmId, ChunkWorldPosition));
    }

    static void LoadChunks(ChunkLoadRequest request)
    {
        try
        {
            var realmId = request.realmId;
            foreach (var c in request.chunks)
            {
                var v = c.ToVector();
                var chunkData = request.realmData.AddChunk(c);
                var chunk = new Chunk(v, WorldSettings.ChunkWidth, chunkData, null);

                var path = ChunkPath(realmId, v);

                var json = File.ReadAllText(path);

                JsonConvert.PopulateObject(json, chunk, settings);
            }
        }
        catch(Exception ex) 
        { 
            Debug.LogException(ex); 
        }
    }

    static Task SavingTask;
    static Queue<ChunkSaveRequest> toSave = new();

    static public void SaveChunk(string realmId, Chunk chunk)
    {
        toSave.Enqueue(new() { chunk = chunk, realmId = realmId});
        if (SavingTask == null)
        {
            SavingTask = Task.Run(SaveChunks);
        }
    }

    public static void Flush()
    {
        SavingTask?.Wait();
    }

    static void SaveChunks()
    {
        while(toSave.TryDequeue(out var c))
        {
            if (!Directory.Exists(DirectoryPath(c.realmId)))
            {
                Directory.CreateDirectory(DirectoryPath(c.realmId));
            }
            var chunkPath = ChunkPath(c.realmId, c.chunk.ChunkPos);
            Debug.Log("Saving to " + chunkPath);
            try
            {
                var json = JsonConvert.SerializeObject(c.chunk, settings);
                File.WriteAllTextAsync(chunkPath, json);
            } 
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        SavingTask = null;
    }

    public static ChunkLoadRequest LoadChunks(string realmId, NativeList<int2> chunks)
    {
        var realmData = new RealmData(WorldSettings.ChunkWidth, chunks.Length);
        var request = new ChunkLoadRequest()
        {
            realmId = realmId,
            realmData = realmData,
            chunks = chunks,
        };
        request.LoadTask = Task.Run(() => { LoadChunks(request); });
        return request;
    }

    struct ChunkSaveRequest
    {
        public Chunk chunk;
        public string realmId;
    }

    public struct ChunkLoadRequest : IChunkLoadRequest
    {
        public string realmId;

        public NativeList<int2> chunks { get; set; }
        public RealmData realmData { get; set; }
        public Task LoadTask;

        public bool isComplete => LoadTask.IsCompleted;

        public void Complete()
        {
            LoadTask.Wait();
        }

        public void Dispose()
        {
            chunks.Dispose();
        }
    }
}
