using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldConfig : MonoBehaviour
{
    static WorldConfig instance;

    public static int MaxLightLevel => instance.m_MaxLightLevel;
    public static int AnimalsPerChunk => instance.m_AnimalsPerChunk;
    public static int HostilesPerChunk => instance.m_HostilesPerChunk;
    public static int ChunkWidth => instance.m_ChunkWidth;
    public static int ChunkGenDistance => instance.m_ChunkGenDistance;
    public static int ChunkTickDistance => instance.m_ChunkTickDistance;
    public static int TickMs => instance.m_ChunkTickDistance;
    public static List<ItemStack> StartingHotbar => instance.m_StartingHotbar;

    public int m_MaxLightLevel = 16;
    public int m_AnimalsPerChunk = 5;
    public int m_HostilesPerChunk = 5;
    public int m_ChunkWidth;
    public int m_ChunkGenDistance;
    public int m_ChunkTickDistance;
    public int m_TickMs;
    public List<ItemStack> m_StartingHotbar = new();

    private void Awake()
    {
        instance = this;
    }
}
