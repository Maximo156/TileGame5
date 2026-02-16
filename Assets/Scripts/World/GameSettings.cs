using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static bool NaturalSpawn => instance?.m_NaturalSpawn ?? true;
    public static bool DontUseRecipeInputs => instance?.m_DontUseRecipeInputs ?? true;
    public static bool CreativeMenu => instance?.m_CreativeMenu ?? false;

    public bool m_NaturalSpawn = true;
    public bool m_DontUseRecipeInputs = true;
    public bool m_CreativeMenu = true;

    static GameSettings instance;
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }
}
