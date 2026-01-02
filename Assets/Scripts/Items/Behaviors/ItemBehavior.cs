using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[Serializable]
public abstract class ItemBehavior
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

    static ItemBehavior()
    {
        BuildCache();

        // Fired when scripts recompile
        CompilationPipeline.compilationFinished += _ => BuildCache();

        // Fired when domain reload happens
        AssemblyReloadEvents.afterAssemblyReload += BuildCache;
    }

    private static void BuildCache()
    {
        cachedTypes = Utilities.GetAllConcreteSubclassesOf<ItemBehavior>();
    }
    #endregion

    [HideInInspector]
    public string name;

    public ItemBehavior() 
    {
        name = GetType().Name;
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
