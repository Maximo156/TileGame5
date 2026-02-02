using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using NativeRealm;
using BlockDataRepos;

public class WorldDisplay : MonoBehaviour
{
    public float layerLoadTime = 0.05f;

    public SingleTileMap Water;
    public SingleTileMap Stone;
    public LightTileMap Roof;
    public LightTileMap Darkness;

    public Tilemap Ground;
    public Tilemap Wall;

    public PlacedItem PlacedItemPrefab;

    Stack<PlacedItem> available = new Stack<PlacedItem>();
    Dictionary<Vector2Int, PlacedItem[]> placed = new Dictionary<Vector2Int, PlacedItem[]>();
    TileBase[] nullBuffer;


    private void Start()
    {
        ChunkManager.OnRealmChange += Clear;
        var chunkWidth = WorldSettings.ChunkWidth;
        nullBuffer = Enumerable.Repeat<TileBase>(null, chunkWidth* chunkWidth).ToArray();
    }

    public IEnumerator RenderChunk(TileDisplayCache tileDisplayInfo, bool clear)
    {
        var chunkWidth = WorldSettings.ChunkWidth;
        var chunkPos = tileDisplayInfo.chunkWorldPos;
        var bounds = new BoundsInt(chunkPos.x, chunkPos.y, 0, chunkWidth, chunkWidth, 1);
        Water.SetTiles(tileDisplayInfo.WaterPositions, !clear);
        yield return new WaitForSeconds(layerLoadTime);
        Stone.SetTiles(tileDisplayInfo.StonePositions, !clear);
        yield return new WaitForSeconds(layerLoadTime);
        Roof.SetTiles(tileDisplayInfo.RoofPositions, !clear);
        yield return new WaitForSeconds(layerLoadTime);
        Darkness.SetTiles(tileDisplayInfo.DarknessPositions, !clear);
        yield return new WaitForSeconds(layerLoadTime);
        Ground.SetTilesBlock(bounds, !clear ? tileDisplayInfo.GroundTiles : nullBuffer);
        yield return new WaitForSeconds(layerLoadTime);
        Wall.SetTilesBlock(bounds, !clear ? tileDisplayInfo.WallTiles : nullBuffer);
        yield return new WaitForSeconds(layerLoadTime);
        for(int i = 0; i < tileDisplayInfo.PlacedItems.positions.Length; i++)
        {
            UpdateItems(tileDisplayInfo.PlacedItems.positions[i], !clear ? tileDisplayInfo.PlacedItems.Items[i] : null);
        }
    }

    public void UpdateBlock(Vector2Int pos, NativeBlockSlice block, BlockItemStack state)
    {
        var groundBlock = BlockDataRepo.GetBlock<Ground>(block.groundBlock);
        var wallBlock = BlockDataRepo.GetBlock<Wall>(block.wallBlock);
        var pos3 = pos.ToVector3Int();
        Roof.SetTile(pos3, block.lightLevel, block.roofBlock != 0);
        Ground.SetTile(pos3, groundBlock?.Display);
        Wall.SetTile(pos3, wallBlock?.Display);
        UpdateItems(pos, state?.placedItems);
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

    public void Clear(Realm _, Realm __)
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
