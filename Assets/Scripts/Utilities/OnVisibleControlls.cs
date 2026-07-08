
using System.Collections.Generic;
using UnityEngine;

public interface IDisablable
{
    public void Disable();
    public void Enable();
}

public class OnVisibleControlls : MonoBehaviour
{
    public bool debug;
    public List<Behaviour> Disable;
    List<IDisablable> Disablable = new();

    private void Awake()
    {
        foreach (var c in GetComponents<Behaviour>())
        {
            if(c is IDisablable d)
            {
                Disablable.Add(d);
            }
        }
    }

    private void Start()
    {
        OnBecameInvisible();
    }

    private void OnBecameInvisible()
    {
        foreach (var component in Disablable)
        {
            component.Disable();
            if (debug)
            {
                print($"Disabling {component.GetType()} on {name}");
            }
        }
        foreach (var component in Disable)
        {
            component.enabled = false;
            if(debug)
            {
                print($"Disabling {component.GetType()} on {name}");
            }
        }
    }

    private void OnBecameVisible()
    {
        foreach (var component in Disable)
        {
            component.enabled = true;
            if (debug)
            {
                print($"Enabling {component.GetType()} on {name}");
            }
        }
        foreach (var component in Disablable)
        {
            component.Enable();
            if (debug)
            {
                print($"Enabling {component.GetType()} on {name}");
            }
        }
    }
}
