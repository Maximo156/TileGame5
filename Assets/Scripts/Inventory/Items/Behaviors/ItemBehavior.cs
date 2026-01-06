using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[Serializable]
public abstract class ItemBehaviour
{
    #region Static Caching
    private static List<Type> cachedTypes;
    public static IReadOnlyList<Type> Types
    {
        get
        {
            if (cachedTypes == null)
                BuildCache();

            return cachedTypes;
        }
    }

    static ItemBehaviour()
    {
        BuildCache();

        // Fired when scripts recompile
        CompilationPipeline.compilationFinished += _ => BuildCache();

        // Fired when domain reload happens
        AssemblyReloadEvents.afterAssemblyReload += BuildCache;
    }

    private static void BuildCache()
    {
        cachedTypes = Utilities.GetAllConcreteSubclassesOf<ItemBehaviour>();
    }
    #endregion

    [HideInInspector]
    public string name;

    public ItemBehaviour() 
    {
        name = GetType().Name;
    }
}

public abstract class ItemBehaviourState
{
    public event Action OnStateChange;

    protected void TriggerStateChange()
    {
        OnStateChange?.Invoke();
    }
}

public interface IStatefulItemBehaviour
{
    public ItemBehaviourState GetNewState();
}

public interface IStateStringProvider
{
    public string GetStateString(Item item);
}