using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEntityManager : MonoBehaviour
{
    public GameObject ItemEntityPrefab;

    public float DespawnSeconds;

    Queue<(float expiry, ItemEntity entity)> entities = new Queue<(float expiry, ItemEntity entity)>();

    public void FixedUpdate()
    {
        while (entities.Count > 0 && ShouldDequeue(entities.Peek()))
        {
            var dequeued = entities.Dequeue().entity;
            if (dequeued != null)
            {
                Destroy(dequeued.gameObject);
            }
        }
    }

    bool ShouldDequeue((float expiry, ItemEntity entity) entry)
    {
        return entry.entity == null || Time.time > entry.expiry;
    }

    private void SpawnItemPriv(Vector2Int pos, ItemStack stack, bool randomMovement = true)
    {
        var itemEntity = Instantiate(ItemEntityPrefab, pos + new Vector2(0.5f, 0.5f), Quaternion.identity, transform).GetComponent<ItemEntity>();
        itemEntity.stack = stack;
        itemEntity.sr.sprite = stack.Item.Sprite;
        itemEntity.sr.size = new Vector2(100, 100);
        if (randomMovement)
        {
            itemEntity.rb.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }
        entities.Enqueue((Time.time + DespawnSeconds, itemEntity));
    }

    public static void SpawnItem(Vector2Int pos, ItemStack stack, bool randomMovement = true)
    {
         ChunkManager.CurRealm.EntityContainer.ItemManager.SpawnItemPriv(pos, stack, randomMovement);
    }
}
