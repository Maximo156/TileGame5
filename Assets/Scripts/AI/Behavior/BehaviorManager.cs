using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public interface IBehavior
{
    public Vector2Int Step(float deltaTime);
}

public class AIBehaviorManager
{
    public IEnumerator RunChunk(IEnumerable<IBehavior> ais)
    {
        yield return null;
        foreach (var ai in ais.Where(ai => ai is not null).ToList())
        {
            ai.Step(Time.deltaTime);
        }
    }
}
