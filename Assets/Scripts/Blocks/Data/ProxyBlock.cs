using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProxyBlock : Wall
{
    static ProxyBlock instance;
    public static ProxyBlock Instance
    {
        get
        {
            if(instance == null)
            {
                instance = CreateInstance<ProxyBlock>();
            }
            return instance;
        }
    }

    public override BlockState GetState()
    {
        return new ProxyState();
    }
}

public class ProxyState : BlockState
{
    public Vector2Int ActualPos { get; set; }
    public override void CleanUp(Vector2Int pos)
    {
        
    }
}
