using NativeRealm;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComposableBlocks
{
    public abstract class BlockBehaviour
    {

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
        public BlockBehaviourState GetState();
    }

    public interface IInteractableBehaviour
    {
        public bool Interact(Vector2Int worldPos, ref NativeBlockSlice slice, InteractorInfo interactor);
    }

    public interface IConditionalPlaceBehaviour
    {
        public bool CanPlace(Vector2Int Pos, Vector2Int dir);
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
