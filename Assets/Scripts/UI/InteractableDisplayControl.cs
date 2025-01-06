using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Obsolete]
public abstract class InteractableDislay : MonoBehaviour
{
    public abstract Type TypeMatch();
    public abstract void DisplayInventory(Vector2 worldPos, IInteractableState state);

    public abstract void Detach();
}

[Obsolete]
public class InteractableDisplayControl : MonoBehaviour
{
    [Serializable]
    public class Display
    {
        public InteractableDislay display;
        public int priority;
    }

    public List<Display> displays;


    private IInteractableState attachedInv;
    
    private void Awake()
    {
        //IUiDisplayState.OnStateInteract += DisplayInventory;
        gameObject.SetActive(false);
        foreach(var display in displays)
        {
            display.display.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        displays = displays.OrderBy(d => d.priority).ToList();
    }

    private void DisplayInventory(Vector2 worldPos, IInteractableState state)
    {
        if (Detach(state))
        {
            return;
        }
        //state.OnBroken += () => Detach(attachedInv);
        attachedInv = state;
        gameObject.SetActive(true);
        bool found = false;
        foreach(var display in displays.Select(d => d.display))
        {
            display.gameObject.SetActive(false);
            if (!found && display.TypeMatch().IsAssignableFrom(state.GetType()))
            {
                display.gameObject.SetActive(true);
                display.DisplayInventory(worldPos, state);
                found = true;
            }
        }
    }

    private bool Detach(IInteractableState inv)
    {
        foreach(var display in displays.Select(d => d.display))
        {
            display.Detach();
        }
        if (inv == attachedInv)
        {
            attachedInv = null;
            transform.SetParent(null);
            gameObject.SetActive(false);
            return true;
        }
        return false;
    }
}
