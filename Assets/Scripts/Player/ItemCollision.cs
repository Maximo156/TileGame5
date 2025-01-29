using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCollision : MonoBehaviour
{
    IInventoryContainer inventory;
    // Start is called before the first frame update
    void Start()
    {
        inventory = GetComponentInParent<PlayerInventories>();
    }

    //Detect collisions between the GameObjects with Colliders attached
    void OnTriggerEnter2D(Collider2D collision)
    {
        var itemEntity = collision.gameObject.GetComponent<ItemEntity>();
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        if (itemEntity != null)
        {
            var stack = itemEntity.stack;
            itemEntity.stack = inventory.AddItem(stack);

            if (itemEntity.stack == null || itemEntity.stack.Count == 0) Destroy(itemEntity.gameObject);
        }
    }
}
