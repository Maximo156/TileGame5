using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    static WorldSettings instance;
    public static int ChunkWidth => instance.m_ChunkWidth;
    public static int ChunkGenDistance => instance.m_ChunkGenDistance;
    public static int ChunkTickDistance => instance.m_ChunkTickDistance;
    public static int TickMs => instance.m_ChunkTickDistance;


    public int m_ChunkWidth;
    public int m_ChunkGenDistance;
    public int m_ChunkTickDistance;
    public int m_TickMs;

    private void Awake()
    {
        instance = this;
    }
}
