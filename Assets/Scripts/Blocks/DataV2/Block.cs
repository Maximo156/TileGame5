using NativeRealm;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using BlockDataRepos;
using Newtonsoft.Json;

namespace ComposableBlocks
{
    [JsonConverter(typeof(BlockConverter))]
    public abstract class Block : ScriptableObject, ISpriteful
    {
        [ReadOnlyProperty]
        public ushort Id = 0;

        public TileBase Display;
        public int HitsToBreak;
        public float MovementModifier = 0;
        public List<ItemStack> Drops;

        [field: SerializeField]
        public Sprite Sprite { get; set; }

        [field: SerializeField]
        public Color Color { get; set; } = Color.white;

        [SerializeReference] 
        public List<BlockBehaviour> Behaviors = new List<BlockBehaviour>();

        public virtual bool OnBreak(Vector2Int worldPos, BreakInfo info)
        {
            if (!info.dontDrop)
            {
                Utilities.DropItems(worldPos, Drops);
            }
            foreach(var b in Behaviors)
            {
                if(b is IOnBreakBehaviour breakable)
                {
                    breakable.OnBreak(worldPos, info);
                }
            }
            info.state?.CleanUp(worldPos);
            return true;
        }

        public string GetDisplayName()
        {
            return name.Replace("Block", "").Replace("Item", "").SplitCamelCase();
        }

        public bool TryGetBehavior<T>(out T behaviour) where T : class
        {
            behaviour = Behaviors.FirstOrDefault(b => typeof(T).IsAssignableFrom(b.GetType())) as T;

            return behaviour != null;
        }

        public T GetBehavour<T>() where T : class
        {
            return Behaviors.FirstOrDefault(b => typeof(T).IsAssignableFrom(b.GetType())) as T;
        }

        public IEnumerable<T> GetAllBehavours<T>() where T : class
        {
            return Behaviors.Select(b => b as T).Where(b => b != null);
        }

        public bool TryGetState(out BlockState state)
        {
            var stateful = Behaviors.Where(b => b is IStatefulBlockBehaviour);
            if(stateful.Count() == 0)
            {
                state = null;
                return false;
            }
            state = new BlockState(this, stateful);
            return true;
        }

        public byte GetSimpleState()
        {
            if(TryGetBehavior<ISimpleStateBlockBehaviour>(out var b))
            {
                return b.GetState();
            }
            return 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Id == 0)
            {
                FixupBlockId();
            }
        }
        private void OnEnable()
        {
            if (Id == 0)
            {
                FixupBlockId();
            }
        }

        void FixupBlockId()
        {
            var tmp = Resources.LoadAll<Block>("ScriptableObjects/Blocks").OrderBy(b => b.name).ToList();
            var ordered = tmp.OrderBy(b => b.Id).ToList();
            var set = new HashSet<ushort>(ordered.Select(b => b.Id));
            for (ushort i = 1; i <= ordered.Count; i++)
            {
                if (!set.Contains(i))
                {
                    SetBlockId(this, i);
                    return;
                }
            }
        }

        public static void SetBlockId(Block block, ushort id, bool save = true)
        {
            block.Id = id;
            EditorUtility.SetDirty(block);

            if (save)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
#endif
    }

    public class BlockState
    {
        public event Action OnStateUpdated;

        [JsonProperty]
        readonly IReadOnlyDictionary<Type, BlockBehaviourState> States;
        readonly IReadOnlyList<ITickableBehaviourState> Ticks;

        public void CleanUp(Vector2Int worldPos)
        {
            foreach (var state in States.Values)
            {
                state.CleanUp(worldPos);
            }
        }

        [JsonConstructor]
        public BlockState(IReadOnlyDictionary<Type, BlockBehaviourState> States)
        {
            this.States = States;
            Ticks = States.Values.Where(s => s is ITickableBehaviourState).Select(s => s as ITickableBehaviourState).ToList(); 
            foreach (var state in States.Values)
            {
                state.OnStateUpdated += TriggerStateUpdate;
            }
        }

        public BlockState(Block block, IEnumerable<BlockBehaviour> behaviours): this(behaviours.Select(b => (b as IStatefulBlockBehaviour).GetState(block)).ToDictionary(b => b.GetType(), b => b))
        {
        }

        public T GetState<T>() where T : BlockBehaviourState
        {
            return States[typeof(T)] as T;
        }

        void TriggerStateUpdate()
        {
            OnStateUpdated?.Invoke();
        }

        public void Tick()
        {
            foreach(var state in Ticks)
            {
                state.Tick();
            }
        }
    }

    public struct BreakInfo
    {
        public BlockState state;
        public NativeBlockSlice slice;
        public bool dontDrop;
    }
}
