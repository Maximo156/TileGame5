using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Goap;
using UnityEngine;

public class FearBehaviour : MonoBehaviour, IHittable
{
    [SerializeField]
    private float decayRate = 1f;
    [SerializeField]
    private float damageToFearMult = 1f;

    private BaseMobBrain brain;

    public float Fear { get; private set; }
    public Transform Attacker { get; private set; }

    private void Awake()
    {
        brain = GetComponent<BaseMobBrain>();
    }

    private void Update()
    {
        TickFear(decayRate * Time.deltaTime);
    }

    public void AddFear(float amount, Transform attacker)
    {
        Fear += amount;

        if (attacker != null)
            Attacker = attacker;
    }

    public void ReduceFear(float amount)
    {
        Fear = Mathf.Max(0f, Fear - amount);
    }

    public void TickFear(float deltaTime)
    {
        Fear = Mathf.Max(0f, Fear - deltaTime);
    }

    public void ClearAttacker()
    {
        Attacker = null;
    }

    public void Hit(HitData info)
    {
        AddFear(info.Damage * damageToFearMult + 20, info.Perpetrator);
        brain.PlayAnimation("Hit", brain.ResumeAgent, true);
    }
}
