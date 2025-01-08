using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    public static int MaxLightLevel => instance.m_MaxLightLevel;
    public int m_MaxLightLevel = 16;

    static WorldSettings instance;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }
}
