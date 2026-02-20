using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEntityManager : MonoBehaviour
{
    public GameObject ItemEntityPrefab;

    public float DespawnSeconds;
    
    PriorityQueue<ItemEntity, float> entities = new PriorityQueue<ItemEntity, float>();

    public void FixedUpdate()
    {
        while (entities.TryPeek(out var entity, out var exp) && ShouldDequeue(exp, entity))
        {
            var dequeued = entities.Dequeue();
            if (dequeued != null)
            {
                Destroy(dequeued.gameObject);
            }
        }
    }

    bool ShouldDequeue(float expiry, ItemEntity entity)
    {
        return entity == null || Time.time > expiry;
    }

    private void SpawnItemPriv(Vector2Int pos, ItemStack stack, bool randomMovement = true, int despawnSeconds = -1)
    {
        var seconds = despawnSeconds == -1 ? DespawnSeconds : despawnSeconds;
        var itemEntity = Instantiate(ItemEntityPrefab, pos + new Vector2(0.5f, 0.5f), Quaternion.identity, transform).GetComponent<ItemEntity>();
        itemEntity.stack = stack;
        itemEntity.sr.sprite = stack.Item.Sprite;
        itemEntity.sr.size = new Vector2(100, 100);
        if (randomMovement)
        {
            itemEntity.rb.linearVelocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }
        entities.Enqueue(itemEntity, Time.time + seconds);
    }

    public static void SpawnItem(Vector2Int pos, ItemStack stack, bool randomMovement = true, int despawnSeconds = -1)
    {
         ChunkManager.CurRealm.EntityContainer.ItemManager.SpawnItemPriv(pos, stack, randomMovement, despawnSeconds);
    }
}
