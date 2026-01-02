using System.Collections.Generic;
using UnityEngine;
using EntityStatistics;

/// <summary>
/// Pulls the items within a given range
/// </summary>
public class ItemPickup : MonoBehaviour
{
    public float forceFactor = 30f;
    public CircleCollider2D col;
    
    PlayerInventories _playerInventory;
    private List<Rigidbody2D> _pickedUpItems = new();
    private EntityStats _entityStats;

    public void Start()
    {
        _entityStats = GetComponentInParent<EntityStats>();
        col.radius = _entityStats.GetStat(EntityStats.Stat.PickupRange);
        _playerInventory = GetComponentInParent<PlayerInventories>();
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
            if (!itemBody)
                continue;
            
            var itemEntity = itemBody.GetComponent<ItemEntity>();
            if (_playerInventory.MainInv.CanAddItem(itemEntity.stack))
            {
                var dir = (currPos - itemBody.position);
                itemBody.AddForce(dir.normalized * (forceFactor * Time.fixedDeltaTime)/(dir.magnitude));
            }
        }

        _pickedUpItems.RemoveAll(x => !x);
    }
    
    
    //Detect collisions between the GameObjects with Colliders attached
    // TODO: Check tag
    void OnTriggerEnter2D(Collider2D collider)
    {
        var test = collider.GetComponent<Rigidbody2D>();
        _pickedUpItems.Add(test);
    }
}
