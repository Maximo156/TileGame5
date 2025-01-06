using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class WorldDisplay : MonoBehaviour
{
    public float layerLoadTime = 0.05f;

    public SingleTileMap Water;
    public SingleTileMap Stone;
    public SingleTileMap Roof;
    public SingleTileMap Darkness;

    public Tilemap Ground;
    public Tilemap Wall;

    public PlacedItem PlacedItemPrefab;

    Stack<PlacedItem> available = new Stack<PlacedItem>();
    Dictionary<Vector2Int, PlacedItem[]> placed = new Dictionary<Vector2Int, PlacedItem[]>();

    private void Start()
    {
        ChunkManager.OnGeneratorChange += Clear;
    }

    public IEnumerator RenderChunk(TileDisplayCache tileDisplayInfo, bool clear)
    {
        Water.SetTiles(tileDisplayInfo.WaterPositions, !clear);
        yield return new WaitForSeconds(layerLoadTime);
        Stone.SetTiles(tileDisplayInfo.StonePositions, !clear);
        yield return new WaitForSeconds(layerLoadTime);
        Roof.SetTiles(tileDisplayInfo.RoofPositions, !clear);
        yield return new WaitForSeconds(layerLoadTime);
        Ground.SetTiles(tileDisplayInfo.GroundTiles.positions, !clear ? tileDisplayInfo.GroundTiles.tiles : Enumerable.Repeat<TileBase>(null, tileDisplayInfo.GroundTiles.tiles.Length).ToArray());
        yield return new WaitForSeconds(layerLoadTime);
        Wall.SetTiles(tileDisplayInfo.WallTiles.positions, !clear? tileDisplayInfo.WallTiles.tiles : Enumerable.Repeat<TileBase>(null, tileDisplayInfo.GroundTiles.tiles.Length).ToArray());
        yield return new WaitForSeconds(layerLoadTime);
        for(int i = 0; i < tileDisplayInfo.PlacedItems.positions.Length; i++)
        {
            UpdateItems(tileDisplayInfo.PlacedItems.positions[i], !clear ? tileDisplayInfo.PlacedItems.Items[i] : null);
        }
    }

    public void UpdateBlock(Vector2Int pos, BlockSlice block)
    {
        var pos3 = pos.ToVector3Int();
        Roof.SetTile(pos3, block.RoofBlock is not null);
        Ground.SetTile(pos3, block.GroundBlock?.Display);
        Wall.SetTile(pos3, block.WallBlock?.Display);
        UpdateItems(pos, block.PlacedItems);
    }

    public void RefreshTile(Vector2Int pos)
    {
        var pos3 = pos.ToVector3Int();
        Ground.RefreshTile(pos3);
        Wall.RefreshTile(pos3);
    }

    public void UpdateItems(Vector2Int pos, IEnumerable<ItemStack> items)
    {
        if(placed.TryGetValue(pos, out var alreadyPlaced))
        {
            foreach(var item in alreadyPlaced)
            {
                item.gameObject.SetActive(false);
                available.Push(item);
            }
        }
        if (items != null && items.Any())
        {
            placed[pos] = items.Select(i => GetItemInstance(pos, i)).ToArray();
        }
        else
        {
            placed.Remove(pos);
        }
    }

    PlacedItem GetItemInstance(Vector2Int pos, ItemStack item)
    {
        PlacedItem newItem;
        if(available.Count > 0)
        {
            newItem = available.Pop();
        }
        else
        {
            newItem = Instantiate(PlacedItemPrefab.gameObject, transform).GetComponent<PlacedItem>();
        }
        newItem.Render(item);
        newItem.gameObject.SetActive(true);
        newItem.transform.position = pos + Vector2.one * 0.5f;
        return newItem;
    }

    public void Clear()
    {
        Water.ClearAllTiles();
        Stone.ClearAllTiles();
        Roof.ClearAllTiles();
        Darkness.ClearAllTiles();
        Ground.ClearAllTiles();
        Wall.ClearAllTiles();

        foreach (var alreadyPlaced in placed)
        {
            foreach (var item in alreadyPlaced.Value)
            {
                item.gameObject.SetActive(false);
                available.Push(item);
            }
        }
    }
}
