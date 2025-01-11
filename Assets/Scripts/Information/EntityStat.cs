using UnityEngine;
using System.Collections;

public abstract class EntityStat : MonoBehaviour
{
    public delegate void Change();
    public event Change OnChange;

    public float Max = 100;
    public float Starting = 100;
    public float BaseRegen;
    public float TickSeconds = 1;
    public float NegativeChangeWaitSeconds = 0;

    public float current { get; protected set; }

    protected virtual float Regen => BaseRegen;

    private void Awake()
    {
        current = Starting;
        StartCoroutine(Tick());
    }

    protected virtual void Start()
    {
        OnChange?.Invoke();
    }

    float lastNegative = 0;
    public virtual void ChangeStat(float dif)
    {
        lastNegative = dif < 0 ? Time.time : lastNegative;
        current = Mathf.Clamp(current + dif, 0, Max);
        OnChangeStat();
        OnChange?.Invoke();
    }

    protected virtual void OnTick()
    {
        if (Time.time > lastNegative + NegativeChangeWaitSeconds)
        {
            ChangeStat(Regen);
        }
    }

    private IEnumerator Tick()
    {
        while (true)
        {
            OnTick();
            yield return new WaitForSeconds(TickSeconds);
        }
    }

    protected abstract void OnChangeStat();
}
