using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IStepable
{
    public Vector2Int Step(float deltaTime);
}
public class AIManager : MonoBehaviour
{
    List<IStepable> ais = new();

    // Update is called once per frame
    void Update()
    {
        foreach(var ai in ais)
        {
            ai.Step(Time.deltaTime);
        }
    }

    private void RegisterImpl(IStepable newAi)
    {
        ais.Add(newAi);
    }

    public static void Register(IStepable newAi)
    {
        ChunkManager.CurRealm.EntityContainer.AIManager.RegisterImpl(newAi);
    }
}
