using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

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
        _entityStats.OnStatsChanged += UpdateRange;
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
            
            itemBody.AddForce((currPos - itemBody.position) * (forceFactor * Time.fixedDeltaTime));
        }

        _pickedUpItems.RemoveAll(x => x == null);
    }
    
    
    //Detect collisions between the GameObjects with Colliders attached
    void OnTriggerEnter2D(Collider2D collider)
    {
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        if (collider.CompareTag("droppedItem"))
        {
            var test = collider.GetComponent<Rigidbody2D>();
            _pickedUpItems.Add(test);
        }
    }
}
