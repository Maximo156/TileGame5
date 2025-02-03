using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Chunk
{
    public HashSet<IAI> ais = new();
    GameObject EntityContainer;
    Transform parent;
    public void SetParent(Transform parent)
    {
        this.parent = parent;
    }

    public void AddChild(IAI ai)
    {
        if(EntityContainer == null)
        {
            EntityContainer = new GameObject($"{ChunkPos} Container");
            EntityContainer.transform.parent = parent;
            EnableContainer(false);
        }
        ais.Add(ai);
        ai.Transform.parent = EntityContainer.transform;
        ai.OnDespawn += RemoveChild;
    }

    public void RemoveChild(IAI ai)
    {
        ais.Remove(ai);
        ai.OnDespawn -= RemoveChild;
    }

    public void EnableContainer(bool enable)
    {
        EntityContainer?.SetActive(enable);
    }

    public bool SpawnAI()
    {
        var diff = WorldSettings.AnimalsPerChunk - ais.Count(ai => ai.Natural);
        if (diff > 0)
        {
            int tries = 0;
            int spawned = 0;
            while (tries < 3 && spawned < diff)
            {
                tries++;
                spawned += TrySpawnNatural();
            }
            return spawned > 0;
        }
        return false;
    }

    public int TrySpawnNatural()
    {
        var pos = FindOpenBlock();
        if (pos == null)
        {
            return 0;
        }
        var biome = curGenerator.biomes.GetBiome(BlockPos + pos.Value);
        var info = biome?.NaturalMobs?.SelectRandomWeighted(m => m.Weight, m => m);
        if (info == null) return 0;

        var amount = Random.Range(info.MinCount, info.MaxCount);
        for(int i = 0; i<amount; i++)
        {
            Object.Instantiate(info.Prefab, (pos.Value + BlockPos).ToVector3Int(), Quaternion.identity, null).GetComponent<AI>();
        }
        return amount;
    }

    Vector2Int? FindOpenBlock()
    {
        Vector2Int? pos;
        int count = 0;
        do
        {
            count++;
            pos = Utilities.BFS(Utilities.RandomVector2Int(0, width),
                loc =>
                {
                    if (loc.x < 0 || loc.x >= width || loc.y < 0 || loc.y >= width)
                    {
                        return null;
                    }
                    return blocks[loc.x, loc.y];
                },
                slice => slice != null && slice.WallBlock == null,
                slice => slice == null,
                out _,
                100)?.position;
        } while (pos != null && count <= 3);
        return pos;
    }
}
