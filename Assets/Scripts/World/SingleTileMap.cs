using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class SingleTileMap : MonoBehaviour
{
    Tilemap Map;
    public TileBase Tile;

    public void Start()
    {
        Map = GetComponent<Tilemap>();
        ClearAllTiles();
    }

    public void SetTiles(Vector3Int[] locations, bool render)
    {
        Map.SetTiles(locations, Enumerable.Repeat(render ? Tile : null, locations.Length).ToArray());
    }

    public void SetTile(Vector3Int pos, bool render)
    {
        Map.SetTile(pos, render ? Tile : null);
    }

    public void ClearAllTiles()
    {
        Map.ClearAllTiles();
    }
}
