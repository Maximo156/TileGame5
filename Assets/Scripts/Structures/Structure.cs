using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Terrain/Structure", order = 1)]
public class Structure: ScriptableObject
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

    public int minComponentCount = 1;
    public int maxComponentCount = 2;
    public List<BuildingInformation> Centers = new List<BuildingInformation>();
    public List<BuildingInformation> components = new List<BuildingInformation>();

    public Dictionary<Vector2Int, BlockSlice[,]> Generate(Vector2Int startPos, BiomeInfo biomes, int chunkWidth, System.Random rand, IEnumerable<Vector2Int> SurroundingPoints)
    {
        var area = new StructureBuilder(chunkWidth, startPos, SurroundingPoints);
        var SelectedCentre = Centers.SelectRandom(rand);
        
        var tmp = FindStartingPosition(startPos, area, rand, Rotation.zero, SelectedCentre, biomes);
        if (tmp == null) return new();
        var start = tmp.Value;
        PlaceBuilding(area, start, Rotation.zero, SelectedCentre, rand);
        FillAnchors(area, rand, biomes);
        return area.GetDicts();
    }

    private void FillAnchors(StructureBuilder area, System.Random rand, BiomeInfo biomes)
    {
        int targetComponentCount = rand.Next(minComponentCount, maxComponentCount);
        int placedComponents = 0;
        while (area.OpenAnchors.Count > 0)
        {
            var (anchorPos, curAnchor) = area.OpenAnchors.SelectRandom(rand);
            area.OpenAnchors.Remove(anchorPos);

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


                    if (BuildingFits(area, (anchorPos + anchorOffset + dirOffset), rotation, matchingComponent, biomes))
                    {
                        PlaceBuilding(area, (anchorPos + anchorOffset + dirOffset), rotation, matchingComponent, rand, matchingAnchor.direction);
                        placed = true;
                        placedComponents++;
                        if (placedComponents >= targetComponentCount) return;
                        break;
                    }
                }
                if (placed) break;
            }
        }
    }

    private Vector2Int? FindStartingPosition(Vector2Int start, StructureBuilder area, System.Random rand, Rotation rot, BuildingInformation StartingComponent, BiomeInfo biomes)
    {
        var info = Utilities.BFS(start, v => v, pos => BuildingFits(area, pos, rot, StartingComponent, biomes), info => false, out var _);
        if(info != null)
        {
            return info?.position;
        }
        return null;
    }

    private bool BuildingFits(StructureBuilder area, Vector2Int start, Rotation rotation, BuildingInformation structure, BiomeInfo biomes)
    {
        var bounds = !rotation.mirror ? structure.Bounds : new BoundsInt(Vector3Int.zero, new Vector3Int(structure.Bounds.size.y, structure.Bounds.size.x, structure.Bounds.size.z));
        bounds.position = start.ToVector3Int();
        foreach (var pos in bounds.allPositionsWithin)
        {
            var biomeInfo = biomes.GetBiome(pos.ToVector2Int());
            if (!ValidLayer(biomeInfo, structure) ||
                !area.TryGetBlock(pos.ToVector2Int(), out var block) || block?.HasBlock() == true)
            {
                return false;
            }
        }
        return true;
    }

    private bool PlaceBuilding(StructureBuilder area, Vector2Int start, Rotation rotation, BuildingInformation structure, System.Random rand, AnchorDirection? usedAnchorDir = null)
    {
        var bounds = !rotation.mirror ? structure.Bounds : new BoundsInt(Vector3Int.zero, new Vector3Int(structure.Bounds.size.y, structure.Bounds.size.x, structure.Bounds.size.z));
        bounds.position = start.ToVector3Int();
        foreach (var pos in structure.Bounds.allPositionsWithin)
        {
            var rotatedPosition = ApplyRotation(rotation, pos.ToVector2Int(), structure.Bounds.size.ToVector2Int());

            var worldPos = start + rotatedPosition;
            var block = new BlockSlice(structure.GetSlice(pos.ToVector2Int()));
            area.SetBlock(worldPos, block);
            if (block.State is CrateState crateState)
            {
                crateState.AdditionalDrops = structure.GenerateLootEntry(rand);
            }
        }
        foreach(var (pos, anchor) in structure.GetAnchors(rand).Where(anchorTuple => usedAnchorDir != anchorTuple.anchor.direction))
        {
            var rotatedPosition = ApplyRotation(rotation, pos, structure.Bounds.size.ToVector2Int());
            var worldPos = start + rotatedPosition;
            var anchorInfo = new AnchorInfo()
            {
                direction = (AnchorDirection)(((int)anchor.direction + rotation.dif) % 4),
                key = anchor.key
            };
            area.OpenAnchors.Add(worldPos, anchorInfo);
        }
        return true;
    }

    private Vector2Int ApplyRotation(Rotation rot, Vector2Int pos, Vector2Int size)
    {
        var res = new Vector2Int(!rot.invX ? pos.x : size.x - 1 - pos.x, !rot.invY ? pos.y : size.y - 1 - pos.y);
        return !rot.mirror ? res : new Vector2Int(res.y, res.x);
    }

    private bool ValidLayer(BiomePreset layer, BuildingInformation structure)
    {
        if (layer is null) return false;
        return true;// layer.WallBlock is null || structure.AllowMountains;
    }
}
