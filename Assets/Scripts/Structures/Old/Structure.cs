using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
public class Structure
{
    private class Rotation
    {
        public static Rotation zero = new Rotation();
        public static Rotation ninety = new Rotation() { invX = true, mirror = true, dif = 1 };
        public static Rotation oneeighty = new Rotation() { invX = true, invY = true, dif = 2 };
        public static Rotation twoseventy = new Rotation() { invY = true, mirror = true, dif = 3 };

        public bool invX;
        public bool invY;
        public bool mirror;
        public int dif;
    }

    List<StructureComponent> Centers = new List<StructureComponent>();
    List<StructureComponent> components = new List<StructureComponent>();

    public Structure(List<StructureComponent> Centers, List<StructureComponent> components)
    {
        this.Centers = Centers;
        this.components = components;
    }

    public int ChunkLimit = 32;
    public bool BuildStructure(TownBlockAccessor area, System.Random rand)
    {
        var SelectedCentre = Centers.SelectRandomWeighted(c => 1, c => c, rand);

        var tmp = FindStartingPosition(area, rand, Rotation.zero, SelectedCentre);
        if (tmp == null) return false;
        var start = tmp.Value;
        PlaceBuilding(area, start, Rotation.zero, SelectedCentre);
        FillAnchors(area, rand);
        return true;
    }

    private void FillAnchors(TownBlockAccessor area, System.Random rand)
    {
        while (area.OpenAnchors.Count > 0)
        {
            var (anchorPos, curAnchor) = area.OpenAnchors.Dequeue();

            var compList = components.Where(c => c.HasAnchor(curAnchor.key)).ToList();
            while (compList.Any())
            {
                var matchingComponent = compList.SelectRandomWeighted(c => c.Importance, c => c, rand);
                compList.Remove(matchingComponent);
                var matchingSize = matchingComponent.Bounds.size.ToVector2Int();
                bool placed = false;
                foreach (var matchingAnchor in matchingComponent.GetOpenAnchor(curAnchor.key).Shuffle(rand))
                {
                    if (matchingAnchor == null) continue;
                    var rotationDif = Utilities.modulo(curAnchor.direction - 2 - matchingAnchor.direction, 4);
                    var rotation = rotationDif switch
                    {
                        0 => Rotation.zero,
                        1 => Rotation.ninety,
                        2 => Rotation.oneeighty,
                        3 => Rotation.twoseventy,
                        _ => throw new InvalidOperationException("Attempted to rotate more than 270")
                    };
                    var dirOffset = curAnchor.direction switch
                    {
                        AnchorDirection.Right => Vector2Int.right,
                        AnchorDirection.Up => Vector2Int.up,
                        AnchorDirection.Left => Vector2Int.left,
                        AnchorDirection.Down => Vector2Int.down,
                        _ => throw new InvalidOperationException("Unknown anchor direction")
                    };

                    var anchorOffset = -ApplyRotation(rotation, matchingAnchor.offset, matchingSize);


                    if (BuildingFits(area, (anchorPos + anchorOffset + dirOffset).ToVector3Int(), rotation, matchingComponent))
                    {
                        PlaceBuilding(area, (anchorPos + anchorOffset + dirOffset).ToVector3Int(), rotation, matchingComponent, matchingAnchor.direction);
                        placed = true;
                        break;
                    }
                }
                if (placed) break;
            }
        }
    }

    private Vector3Int? FindStartingPosition(TownBlockAccessor area, System.Random rand, Rotation rot, StructureComponent StartingComponent)
    {
        foreach(var chunk in area.town.Select(kvp => kvp.Value).Shuffle())
        {
            int tries = 0;
            do
            {
                var pos = Utilities.RandomVector2Int(0, area.chunkWidth - Mathf.Max(StartingComponent.Bounds.size.x, StartingComponent.Bounds.size.y), rand).ToVector3Int() + chunk.WorldPos;
                if(BuildingFits(area, pos, rot, StartingComponent))
                {
                    return pos;
                }
            } while (tries++ < 3);
        }
        return null;
    }

    private bool BuildingFits(TownBlockAccessor area, Vector3Int start, Rotation rotation, StructureComponent structure)
    {
        var bounds = !rotation.mirror ? structure.Bounds : new BoundsInt(Vector3Int.zero, new Vector3Int(structure.Bounds.size.y, structure.Bounds.size.x, structure.Bounds.size.z));
        bounds.position = start;
        foreach(var pos in bounds.allPositionsWithin)
        {
            var layer = ChunkManagerV2.GetLayer(pos.ToVector2Int());
            if (!ValidLayer(layer, structure) || 
                !area.TryGetBlock(pos.ToVector2Int(), out var blocks) || blocks.Item1 != null || blocks.Item2 != null) return false;
        }
        return true;
    }

    private bool PlaceBuilding(TownBlockAccessor area, Vector3Int start, Rotation rotation, StructureComponent structure, AnchorDirection? usedAnchorDir = null)
    {
        var bounds = !rotation.mirror ? structure.Bounds : new BoundsInt(Vector3Int.zero, new Vector3Int(structure.Bounds.size.y, structure.Bounds.size.x, structure.Bounds.size.z));
        bounds.position = start;
        foreach (var pos in structure.Bounds.allPositionsWithin)
        {
            var rotatedPosition = ApplyRotation(rotation, pos.ToVector2Int(), structure.Bounds.size.ToVector2Int());
            
            var worldPos = start.ToVector2Int() + rotatedPosition;
            var block = structure.GetBlock(pos.ToVector2Int());
            area.SetBlock(worldPos, structure.GetFloor(pos.ToVector2Int()));
            area.SetBlock(worldPos, block);
            if(block is ChestBlockV2)
            {
                area.SetLootTable(worldPos, structure.lootTable);
            }
            var anchor = structure.GetAnchor(pos.ToVector2Int());
            if(anchor != null && (usedAnchorDir != anchor.direction))
            {
                var anchorInfo = new AnchorInfo()
                {
                    direction = (AnchorDirection)(((int)anchor.direction + rotation.dif) % 4),
                    key = anchor.key
                };
                area.OpenAnchors.Enqueue((worldPos, anchorInfo));
            }
        }
        return true;
    }

    private Vector2Int ApplyRotation(Rotation rot, Vector2Int pos, Vector2Int size)
    {
        var res = new Vector2Int(!rot.invX ? pos.x : size.x - 1 - pos.x, !rot.invY ? pos.y : size.y - 1 - pos.y);
        return !rot.mirror ? res : new Vector2Int(res.y, res.x);
    }
    
    private bool ValidLayer(int layer, StructureComponent structure)
    {
        return layer is 1 or 2 || (structure.AllowMountains && layer is 3 or 4);
    }
}
*/