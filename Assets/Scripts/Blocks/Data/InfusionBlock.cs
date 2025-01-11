using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInfusionBlock", menuName = "Block/InfusionBlock", order = 1)]
public class InfusionBlock : Wall, IInterfaceBlock
{
    public List<ItemCharm> AllowedCharms;
}
