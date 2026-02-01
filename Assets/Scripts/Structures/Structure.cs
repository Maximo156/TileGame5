using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Terrain/Structure", order = 1)]
public class Structure: ScriptableObject
{
    public int minComponentCount = 1;
    public int maxComponentCount = 2;
    public List<BuildingInformation> Centers = new List<BuildingInformation>();
    public List<BuildingInformation> components = new List<BuildingInformation>();
}
