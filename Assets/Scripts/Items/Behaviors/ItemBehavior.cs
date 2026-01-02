using System;
using UnityEngine;

public class ItemBehaviourListAttribute : PropertyAttribute
{

}

[Serializable]
public abstract class ItemBehavior
{
    [HideInInspector]
    public string name;

    public ItemBehavior() 
    {
        name = this.GetType().Name;
    }
}

[Serializable]
public class TestBehavior : ItemBehavior
{
    public int a = 1;
}

[Serializable]
public class TestBehavior2 : ItemBehavior
{
    public int b = 1;
}
