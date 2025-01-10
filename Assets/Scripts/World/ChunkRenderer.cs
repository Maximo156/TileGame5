using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(WorldDisplay))]
public class ChunkRenderer : MonoBehaviour
{
    public static ChunkRenderer Renderer;
    public int ChunkLoadRadius;
    public int ChunkUnloadRadius;
    public bool ShowChunks;
    
    Dictionary<Vector2Int, Chunk> renderedChunks = new Dictionary<Vector2Int, Chunk>();
    HashSet<Vector2Int> toRender = new HashSet<Vector2Int>();
    HashSet<Vector2Int> shouldBeRenderedChunks = new HashSet<Vector2Int>();
    HashSet<Vector2Int> toUnload = new HashSet<Vector2Int>();
    WorldDisplay Display;

    private void Awake()
    {
        Renderer = this;
        Display = GetComponent<WorldDisplay>();
        PlayerMovement.OnPlayerChangedChunks += PlayerChangedChunks;
        Chunk.OnChunkChanged += RefreshChunk;
        Chunk.OnBlockChanged += PlaceTile;
        Chunk.OnBlockRefreshed += RefreshTile;
    }

    private void FixedUpdate()
    {
        if (rendering) return;
        bool rendered = false;
        foreach(var chunkPos in toRender.OrderBy(c => Vector2.Distance(c, curChunk)).Where(c => c!= null)) 
        {
            if (ChunkManager.TryGetChunk(chunkPos, out var chunkToRender))
            {
                toRender.Remove(chunkPos);
                if (!renderedChunks.ContainsKey(chunkPos))
                {
                    RefreshChunk(chunkToRender);
                    rendered = true;
                    break;
                }
            }
        }
        if (!rendered && toUnload.Any())
        {
            var chunkPos = toUnload.First();
            toUnload.Remove(chunkPos);
            if (renderedChunks.TryGetValue(chunkPos, out var chunk))
            {
                UnloadChunk(chunk);
            }
        }
    }

    private Vector2Int curChunk;
    public void PlayerChangedChunks(Vector2Int curChunk)
    {
        this.curChunk = curChunk;
        if (!shouldBeRenderedChunks.Any())
        {
            shouldBeRenderedChunks.Add(curChunk);   
            toRender.Add(curChunk);
        }

        foreach(var chunk in Utilities.Spiral(curChunk, (uint)ChunkLoadRadius))
        {
            if(Vector2.Distance(curChunk, chunk) < ChunkLoadRadius)
            {
                if (!shouldBeRenderedChunks.Contains(chunk)) 
                {
                    toRender.Add(chunk);
                    shouldBeRenderedChunks.Add(chunk);
                }
                toUnload.Remove(chunk);
            }
        }

        foreach(var chunk in shouldBeRenderedChunks.ToList())
        {
            if (Vector2.Distance(curChunk, chunk) > ChunkUnloadRadius)
            {
                toUnload.Add(chunk);
                shouldBeRenderedChunks.Remove(chunk);
                toRender.Remove(chunk);
            }
            else
            {
                toUnload.Remove(chunk);
            }
        }
    }

    private void UnloadChunk(Chunk chunk)
    {
        renderedChunks.Remove(chunk.ChunkPos);
        StartCoroutine(UpdateChunkDisplay(chunk, false));
    }

    public void RefreshChunk(Chunk chunk)
    {
        if (shouldBeRenderedChunks.Contains(chunk.ChunkPos))
        {
            renderedChunks[chunk.ChunkPos] = chunk;
            StartCoroutine(UpdateChunkDisplay(chunk, true));
        }
    }

    bool rendering = false;
    private IEnumerator UpdateChunkDisplay(Chunk chunk, bool load)
    {
        rendering = true;
        yield return Display.RenderChunk(chunk.GetBlocks(), !load);
        chunk.UpdateLighting();
        rendering = false;
    }

    public void PlaceTile(Chunk _, Vector2Int BlockPos, Vector2Int ChunkPos, BlockSlice slice)
    {
        if(renderedChunks.ContainsKey(ChunkPos))
        {
            Display.UpdateBlock(BlockPos, slice);
        }
    }

    public void RefreshTile(Chunk _, Vector2Int BlockPos, Vector2Int ChunkPos)
    {
        if (renderedChunks.ContainsKey(ChunkPos))
        {
            Display.RefreshTile(BlockPos);
        }
    }

    public void OnDrawGizmos()
    {
        if (ShowChunks && Application.isPlaying)
        {
            var chunkWidth = ChunkManager.ChunkWidth;

            foreach (var kvp in renderedChunks)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube((kvp.Key.ToVector3Int() + Vector3.one * 0.5f) * chunkWidth, Vector3.one * chunkWidth);
            }

            foreach (var kvp in shouldBeRenderedChunks)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube((kvp.ToVector3Int() + Vector3.one * 0.5f) * chunkWidth, Vector3.one * chunkWidth * 0.95f);
            }

            foreach (var kvp in toRender)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube((kvp.ToVector3Int() + Vector3.one * 0.5f) * chunkWidth, Vector3.one * chunkWidth * 0.9f);
            }

            foreach (var kvp in toUnload)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube((kvp.ToVector3Int() + Vector3.one * 0.5f) * chunkWidth, Vector3.one * chunkWidth * 0.85f);
            }
        }
    }
}
