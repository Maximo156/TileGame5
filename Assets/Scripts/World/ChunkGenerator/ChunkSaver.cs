using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class ChunkSaver
{
    static JsonSerializerSettings settings;
    static ChunkSaver()
    {
        typeof(SORepository).TypeInitializer.Invoke(null, null);
        settings = new JsonSerializerSettings();
        settings.Converters.Add(new BlockConverter());
    }
    string DirectoryPath => Path.Join(ChunkManager.DataPath, Identifier);

    public string Identifier { get; }

    public ChunkSaver(string name)
    {
        Identifier = name;
    }

    string ChunkPath(Vector2Int ChunkWorldPosition)
    {
        return Path.Join(DirectoryPath, $"Chunk-{ChunkWorldPosition.x}x{ChunkWorldPosition.y}");
    }

    public bool TryLoadBlockSlices(Vector2Int ChunkWorldPosition, out BlockSlice[,] blocks)
    {
        var path = ChunkPath(ChunkWorldPosition);
        blocks = null;
        if (!File.Exists(path))
        {
            return false;
        }

        var json = File.ReadAllText(path);

        blocks = JsonConvert.DeserializeObject<BlockSlice[,]>(json, settings);
        return true;
    }

    Task SavingTask;
    ConcurrentQueue<Chunk> toSave = new();

    public void SaveChunk(Chunk chunk)
    {
        toSave.Enqueue(chunk);
        if (SavingTask == null)
        {
            SavingTask = Task.Run(SaveChunks);
        }
    }

    void SaveChunks()
    {
        while(toSave.TryDequeue(out var c))
        {
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }
            var json = JsonConvert.SerializeObject(c.blocks, settings);
            File.WriteAllTextAsync(ChunkPath(c.ChunkPos), json);
        }
        SavingTask = null;
    }
}
