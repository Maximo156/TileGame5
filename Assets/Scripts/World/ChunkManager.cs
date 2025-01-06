using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChunkManager : MonoBehaviour
{
    public delegate void GeneratorChange();
    public static event GeneratorChange OnGeneratorChange;

    static ChunkManager Manager;
    public static int ChunkWidth => Manager.chunkWidth;
    public static int MsPerTick => Manager.msPerTick;
    public static string DataPath;

    public ChunkGenerator OverworldGenerator;
    public ChunkGenerator CaveGenerator;

    ChunkGenerator _terrainGenerator;
    public ChunkGenerator terrainGenerator { 
        get => _terrainGenerator; 
        set
        {
            _terrainGenerator = value;
            OnGeneratorChange?.Invoke();
        }
    }
    public int chunkWidth;
    public int chunkGenDistance;
    public int chunkTickDistance;
    public int msPerTick = 30;
    public bool debug;

    readonly Queue<Vector2Int> needGen = new Queue<Vector2Int>();
    Dictionary<Vector2Int, Chunk> LoadedChunks = new Dictionary<Vector2Int, Chunk>();

    public void Awake()
    { 
        Manager = this;
        terrainGenerator = OverworldGenerator;
        DataPath = Application.persistentDataPath;
    }

    // Start is called before the first frame update
    void Start()
    {
        PlayerMovement.OnPlayerChangedChunks += PlayerChangedChunks;
        PortalBlock.OnPortalBlockUsed += PortalUsed;
        Task.Run(() => ChunkTick());
    }


    private CancellationTokenSource AllTaskShutdown = new CancellationTokenSource();
    private Task GenerateNewChunksTask;
    // Update is called once per frame
    void Update()
    {
        if (needGen.Count > 0 && (GenerateNewChunksTask == null || GenerateNewChunksTask.IsCompleted))
        {
            var newTask = Task.Run(() => GenerateNewChunks(needGen.Dequeue(), chunkGenDistance));
            GenerateNewChunksTask = newTask;
        }
    }

    Vector2Int curChunk;
    public void PlayerChangedChunks(Vector2Int curChunk)
    {
        this.curChunk = curChunk;
        needGen.Enqueue(curChunk);
    }

    public async void ChunkTick()
    {
        while (true && !AllTaskShutdown.Token.IsCancellationRequested)
        {
            try
            {
                List<Task> chunkTasks = new List<Task>()
                {
                    Task.Delay(msPerTick)
                };
                for (int x = -chunkTickDistance; x <= chunkTickDistance; x++)
                {
                    for (int y = -chunkTickDistance; y <= chunkTickDistance; y++)
                    {
                        if (LoadedChunks.TryGetValue(new Vector2Int(x, y) + curChunk, out var chunk))
                        {
                            chunkTasks.Add(Task.Run(() => chunk.ChunkTick(AllTaskShutdown.Token)));
                        }
                    }
                }
                await Task.WhenAll(chunkTasks);
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
        }
        Debug.Log("Stopping Tick");
    }

    public async void GenerateNewChunks(Vector2Int curChunk, int dist)
    {
        try 
        {
            foreach (var newChunk in Utilities.Spiral(curChunk, (uint)dist))
            {
                if (AllTaskShutdown.Token.IsCancellationRequested) return;
                if (!LoadedChunks.ContainsKey(newChunk))
                {
                    var chunk = new Chunk(newChunk, ChunkWidth);
                    await chunk.Generate(terrainGenerator);
                    LoadedChunks[newChunk] = chunk;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static bool TryGetBlock(Vector2Int position, out BlockSlice block)
    {
        block = default;
        if (Manager == null) return false;

        var chunkPos = Vector2Int.FloorToInt(new Vector2(position.x, position.y) / ChunkWidth);
        if (Manager.LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            block = chunk.GetBlock(position);
            return true;
        }
        return false;
    }

    private void PortalUsed(ChunkGenerator newDim, PortalBlock exitBlock, Vector2Int worldPos)
    {
        terrainGenerator = newDim;
        Task.Run(async () =>
        {
            try
            {
                foreach (var chunk in new Dictionary<Vector2Int, Chunk>(LoadedChunks))
                {
                    if (AllTaskShutdown.Token.IsCancellationRequested) return;
                    var chunkPos = Vector2Int.FloorToInt((new Vector2(worldPos.x, worldPos.y) / ChunkWidth));
                    await chunk.Value.Generate(terrainGenerator, chunkPos == chunk.Key ? worldPos : null, exitBlock);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        });
    }

    public static float GetMovementSpeed(Vector2Int position)
    {
        if (!TryGetBlock(position, out var block)) return 0;
        return block.MovementSpeed;
    }

    public static bool TryGetChunk(Vector2Int chunk, out Chunk chunkObj)
    {
        return Manager.LoadedChunks.TryGetValue(chunk, out chunkObj);
    }

    public static bool PlaceBlock(Vector2Int position, Block block)
    {
        return PerformChunkAction(position, chunk => chunk.PlaceBlock(position, block));
    }

    public static bool Interact(Vector2Int position)
    {
        return PerformChunkAction(position, chunk => chunk.Interact(position));
    }

    public static bool BreakBlock(Vector2Int position, bool roof, bool drop = true)
    {
        return PerformChunkAction(position, chunk => chunk.BreakBlock(position, roof, drop));
    }

    public static bool PlaceItem(Vector2Int position, ItemStack item)
    {
        return PerformChunkAction(position, chunk => chunk.PlaceItem(position, item));
    }

    public static ItemStack PopItem(Vector2Int position)
    {
        return PerformChunkAction(position, chunk => chunk.PopItem(position));
    }

    private static T PerformChunkAction<T>(Vector2Int position, Func<Chunk, T> action)
    {
        var chunkPos = Vector2Int.FloorToInt(new Vector2(position.x, position.y) / ChunkWidth);
        if (Manager.LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            return action(chunk);
        }
        return default;
    }


    private void OnDestroy()
    {
        AllTaskShutdown.Cancel();
    }

    private void OnDrawGizmos()
    {
        if (debug)
        {
            var structure = terrainGenerator?.Generators.FirstOrDefault(g => g is StructureGenerator);
            if(structure is StructureGenerator s)
            {
                s.DebugDraw();
            }
        }
    }
}
