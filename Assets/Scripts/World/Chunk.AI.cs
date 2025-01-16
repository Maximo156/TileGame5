using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Chunk
{
    public HashSet<IStepable> ais = new();
    GameObject EntityContainer;
    Transform parent;
    public void SetParent(Transform parent)
    {
        this.parent = parent;
    }

    public void AddChild(IStepable ai)
    {
        if(EntityContainer == null)
        {
            EntityContainer = new GameObject($"{ChunkPos} Container");
            EntityContainer.transform.parent = parent;
        }
        ais.Add(ai);
        ai.Transform.parent = EntityContainer.transform;
    }
}
