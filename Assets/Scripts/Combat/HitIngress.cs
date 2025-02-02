using System;
using System.Linq;
using UnityEngine;

public interface IHittable
{
    public void Hit();
}

public class HitIngress : MonoBehaviour
{
    public event Action OnHit = delegate { };
    public float ImmuneSeconds = 0.3f;
    private Timer immunityTimer;
    // Start is called before the first frame update
    void Start()
    {
        foreach(var hittable in GetComponents<Component>().OfType<IHittable>())
        {
            OnHit += hittable.Hit;
        }
    }

    public void Hit()
    {
        if (immunityTimer?.Expired != false)
        {
            print("Hit");
            immunityTimer = new Timer(ImmuneSeconds);
            OnHit?.Invoke();
        }
    }
}
