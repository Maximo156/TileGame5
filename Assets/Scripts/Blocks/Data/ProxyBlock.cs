using BlockDataRepos;
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

    public override BlockData GetBlockData()
    {
        var data = base.GetBlockData();
        data.isProxy = true;
        return data;
    }
}
