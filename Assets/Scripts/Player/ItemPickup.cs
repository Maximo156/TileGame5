using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using EntityStatistics;

public class ItemPickup : MonoBehaviour
{
    public float forceFactor = 30f;
    public CircleCollider2D col;
    
    private List<Rigidbody2D> _pickedUpItems = new();
    private EntityStats _entityStats;

    public void Start()
    {
        _entityStats = GetComponentInParent<EntityStats>();
        col.radius = _entityStats.GetStat(EntityStats.Stat.PickupRange);
        _entityStats.OnStatChanged += UpdateRange;
    }

    private void UpdateRange(EntityStats.Stat changedStat)
    {
        if (changedStat == EntityStats.Stat.PickupRange)
        {
            col.radius = _entityStats.GetStat(EntityStats.Stat.PickupRange);
        }
    }

    private void FixedUpdate()
    {
        var currPos = transform.position.ToVector2();
        foreach (var itemBody in _pickedUpItems)
        {
            if (itemBody == null)
                continue;
            var dir = (currPos - itemBody.position);
            itemBody.AddForce(dir.normalized * (forceFactor * Time.fixedDeltaTime)/(dir.magnitude));
        }

        _pickedUpItems.RemoveAll(x => x == null);
    }
    
    
    //Detect collisions between the GameObjects with Colliders attached
    void OnTriggerEnter2D(Collider2D collider)
    {
        if(!enabled) return;
        var test = collider.GetComponent<Rigidbody2D>();
        _pickedUpItems.Add(test);
    }
}
