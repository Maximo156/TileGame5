using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    public static int MaxLightLevel => instance.m_MaxLightLevel;
    public static int AnimalsPerChunk => instance.m_AnimalsPerChunk;
    public static int HostilesPerChunk => instance.m_HostilesPerChunk;
    public static bool NaturalSpawn => instance.m_NaturalSpawn;
    public static bool UseRecipeInputs => instance.m_UseRecipeInputs;
    public static bool UseDefaultInventory => instance.m_UseDefaultInventory;
    public static List<ItemStack> StartingHotbar => instance.m_StartingHotbar;



    public int m_MaxLightLevel = 16;
    public int m_AnimalsPerChunk = 5;
    public int m_HostilesPerChunk = 5;
    public bool m_NaturalSpawn = true;
    public bool m_UseRecipeInputs = true;
    public bool m_UseDefaultInventory = false;
    public List<ItemStack> m_StartingHotbar = new();

    static WorldSettings instance;
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }
}
