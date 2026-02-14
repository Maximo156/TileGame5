using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public partial class Chunk
{
    public HashSet<IAI> ais = new();
    GameObject EntityContainer;

    public void AddChild(IAI ai)
    {
        if(EntityContainer == null)
        {
            EntityContainer = new GameObject($"{ChunkPos} Container");
            EntityContainer.transform.parent = parentRealm.EntityContainer.transform;
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
        var diff = WorldConfig.AnimalsPerChunk - ais.Count(ai => ai.Natural);
        var naturalSpawned = 0;
        var hostileSpawned = 0;
        if (diff > 0)
        {
            int tries = 0;
            while (tries < 3 && naturalSpawned < diff)
            {
                tries++;
                naturalSpawned += TrySpawnMob(b => b.NaturalMobs);
            }
        }

        if (DayTime.dayTime.IsNight)
        {
            var hostileDif = WorldConfig.HostilesPerChunk - ais.Count(ai => ai.Hostile);
            if (hostileDif > 0)
            {
                int tries = 0;
                while (tries < 3 && hostileSpawned < diff)
                {
                    tries++;
                    hostileSpawned += TrySpawnMob(b => b.HostileMobs);
                }
            }
        }
        return hostileSpawned > 0 || naturalSpawned > 0;
    }

    public int TrySpawnMob(Func<BiomePreset, IEnumerable<BiomePreset.MobSpawnInfo>> getSpawnables)
    {
        var pos = FindOpenBlock();
        if (pos == null)
        {
            return 0;
        }
        var biome = parentRealm.BiomeInfo.GetBiome(BlockPos + pos.Value);
        if(biome == null)
        {
            return 0;
        }
        var info = getSpawnables(biome).SelectRandomWeighted(m => m.Weight, m => m);
        if (info == null) return 0;

        var amount = Random.Range(info.MinCount, info.MaxCount);
        for(int i = 0; i<amount; i++)
        {
            Object.Instantiate(info.Prefab, (pos.Value + BlockPos).ToVector3Int(), Quaternion.identity, null).GetComponent<AI>();
        }
        return amount;
    }

    public void Drop()
    {
        Object.Destroy(EntityContainer);
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
                        return 0;
                    }
                    return data.GetWall(loc.x, loc.y);
                },
                wall => wall != 0,
                wall => wall != 0,
                out _,
                100)?.position;
        } while (pos != null && count <= 3);
        return pos;
    }
}
