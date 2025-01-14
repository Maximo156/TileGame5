using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

[RequireComponent(typeof(Tilemap))]
public class LightTileMap : MonoBehaviour
{
    Tilemap Map;
    public TileBase Tile;

    public void Start()
    {
        ChunkManager.OnRealmChange += OnRealmChange;
        Map = GetComponent<Tilemap>();
        ClearAllTiles();
    }

    void OnRealmChange(Realm old, Realm newRealm)
    {
        if (old != null)
        {
            old.OnLightingUpdated -= LightingUpdated;
        }
        newRealm.OnLightingUpdated += LightingUpdated;
    }

    public void SetTiles(Dictionary<Vector3Int, int> locations, bool render)
    {
        Map.SetTiles(locations.Select(kvp => new TileChangeData()
        {
            tile = render ? Tile : null,
            color = GetScaledColor(kvp.Value),
            position = kvp.Key,
            transform = Matrix4x4.identity
        }).ToArray(), true);
    }

    public void SetTile(Vector3Int pos, int lightLevel, bool render)
    {
        Map.SetTile(new TileChangeData()
        {
            tile = render ? Tile : null,
            color = GetScaledColor(lightLevel),
            position = pos,
            transform = Matrix4x4.identity
        }, true);
    }

    public void ClearAllTiles()
    {
        Map.ClearAllTiles();
    }

    public Color GetScaledColor(int lightLevel)
    {
        var alpha = 1 - Mathf.Clamp01((lightLevel / 2) * 1f / WorldSettings.MaxLightLevel);
        return new Color(1, 1, 1, alpha);
    }

    void LightingUpdated(Dictionary<Vector3Int, int> updated)
    {
        foreach(var kvp in updated)
        {
            Map.SetColor(kvp.Key, GetScaledColor(kvp.Value));
        }
    }

    public void SetColor(Color color)
    {
        Map.color = color;
    }
}
