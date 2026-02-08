using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLightBlock", menuName = "Block/LightBlock", order = 1)]
public class LightBlock : Wall
{
    [Header("Light Settings")]
    public byte LightLevel;
}
