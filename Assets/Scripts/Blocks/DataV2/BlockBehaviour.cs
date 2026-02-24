using NativeRealm;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditor;
using UnityEngine;

namespace ComposableBlocks
{
    [Serializable]
    public abstract class BlockBehaviour
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

        static BlockBehaviour()
        {
            BuildCache();

            // Fired when scripts recompile
            CompilationPipeline.compilationFinished += _ => BuildCache();

            // Fired when domain reload happens
            AssemblyReloadEvents.afterAssemblyReload += BuildCache;
        }

        private static void BuildCache()
        {
            cachedTypes = Utilities.GetAllConcreteSubclassesOf<BlockBehaviour>();
        }
        #endregion

        [HideInInspector]
        public string name;

        public BlockBehaviour()
        {
            name = GetType().Name;
        }
    }

    public interface IInterfaceBlockBehaviour
    {

    }

    public interface ISimpleStateBlockBehaviour
    {
        public byte GetState();
    }

    public interface IStatefulBlockBehaviour
    {
        public BlockBehaviourState GetState(Block baseBlock);
    }

    public interface ILootableBlockBehaviour
    {

    }

    public interface IInteractableBehaviour
    {
        public bool Interact(ref NativeBlockSlice slice, InteractionWorldInfo worldInfo, InteractorInfo interactor);
    }

    public interface IConditionalPlaceBehaviour
    {
        public bool CanPlace(Vector2Int Pos, Vector2Int dir, NativeBlockSlice slice);
    }

    public interface IOnPlaceBehaviour
    {
        public void OnPlace(Vector2Int Pos, Vector2Int dir, ref NativeBlockSlice slice);
    }

    public interface IOnBreakBehaviour
    {
        public void OnBreak(Vector2Int worldPos, BreakInfo info);
    }

    public interface ISimpleTickBlockBehaviour
    {
        public TickInfo GetTickInfo();
    }

    public interface ITickableBehaviourState
    {
        public void Tick();
    }

    public interface IStorageBlockBehaviourState
    {
        public bool AddItemStack(ItemStack stack);
    }

    public struct InteractorInfo
    {
        public PlayerRespawner Respawner;
    }

    public struct InteractionWorldInfo 
    { 
        public Vector2Int WorldPos;
        public Block block;
    }

    public abstract class BlockBehaviourState
    {
        public event Action OnStateUpdated;
        public abstract void CleanUp(Vector2Int worldPos);

        protected void TriggerStateChange()
        {
            OnStateUpdated?.Invoke();
        }
    }
}
