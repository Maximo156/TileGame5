using EntityStatistics;
using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class EnemySensor : MonoBehaviour
{
    public event Action OnEnemyEnter;

    public LayerMask enemyLayer;

    private EntityStats stats;

    public void Init(EntityStats stats)
    {
        this.stats = stats;
        stats.OnStatChanged += OnStatChange;
    }

    public void Start()
    {
        GetComponent<CircleCollider2D>().radius = stats.GetStat(EntityStats.Stat.ViewDistance);
    }

    void OnStatChange(EntityStats.Stat stat)
    {
        if(stat == EntityStats.Stat.ViewDistance)
        {
            GetComponent<CircleCollider2D>().radius = stats.GetStat(EntityStats.Stat.ViewDistance);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (LayerMatch(enemyLayer, collision.gameObject.layer))
        {
            OnEnemyEnter?.Invoke();
        }
    }

    private bool LayerMatch(LayerMask layerMask, int layer)
    {
        return ((1 << layer) & layerMask) != 0;
    }
}
