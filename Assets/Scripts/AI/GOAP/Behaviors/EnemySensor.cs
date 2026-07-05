using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class EnemySensor : MonoBehaviour
{
    public event Action OnEnemyEnter;
    public event Action OnEnemyExit;

    private int activeEnemies = 0;
    private LayerMask enemyLayer;

    public void Init(CombatConfig config)
    {
        GetComponent<CircleCollider2D>().radius = config.viewRange;
        enemyLayer = config.mask;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (LayerMatch(enemyLayer, collision.gameObject.layer))
        {
            activeEnemies++;
            OnEnemyEnter?.Invoke();
        }
    }
     
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (LayerMatch(enemyLayer, collision.gameObject.layer))
        {
            if (--activeEnemies == 0)
            {
                OnEnemyExit?.Invoke();
            }
        }
    }

    private bool LayerMatch(LayerMask layerMask, int layer)
    {
        return ((1 << layer) & layerMask) != 0;
    }
}
