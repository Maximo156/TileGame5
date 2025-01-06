using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewSiblingTile", menuName = "Tile/SiblingTile", order = 1)]
public class SiblingTile : RefreshRuleTile, ISpriteful
{
    public enum SiblingCategory
    {
        Structural
    }

    public SiblingCategory Category = SiblingCategory.Structural;
    public Color m_Color = Color.white;

    public Sprite Sprite => m_DefaultSprite;

    public Color Color => m_Color;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);
        tileData.color = Color;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case TilingRuleOutput.Neighbor.This:
            {
                return tile == this || GetCategory(tile) == Category;
            }
            case TilingRuleOutput.Neighbor.NotThis:
            {
                return !(tile == this || GetCategory(tile) == Category);
            }
        }
        return base.RuleMatch(neighbor, tile);
    }

    SiblingCategory? GetCategory(TileBase tile)
    {
        if(tile is RuleOverrideTile overrideTile)
        {
            return (overrideTile.m_InstanceTile as SiblingTile)?.Category;
        }
        else
        {
            return (tile as SiblingTile)?.Category;
        }
    }
}