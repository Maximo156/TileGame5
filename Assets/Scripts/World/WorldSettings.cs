using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    public static int MaxLightLevel => instance.m_MaxLightLevel;
    public static int AnimalsPerChunk => instance.m_AnimalsPerChunk;
    public static bool NaturalSpawn => instance.m_NaturalSpawn;

    public int m_MaxLightLevel = 16;
    public int m_AnimalsPerChunk = 5;
    public bool m_NaturalSpawn = true;

    static WorldSettings instance;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }
}
