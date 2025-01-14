using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    ItemEntityManager _itemManager;
    public ItemEntityManager ItemManager
    {
        get
        {
            if (_itemManager is null)
            {
                _itemManager = gameObject.GetComponent<ItemEntityManager>();
            }
            return _itemManager;
        }
    }

    ProjectileManager _projectileManager;
    public ProjectileManager ProjectileManager
    {
        get
        {
            if (_projectileManager is null)
            {
                _projectileManager = gameObject.GetComponent<ProjectileManager>();
            }
            return _projectileManager;
        }
    }

    AIManager _aiManager;
    public AIManager AIManager
    {
        get
        {
            if (_aiManager is null)
            {
                _aiManager = gameObject.GetComponent<AIManager>();
            }
            return _aiManager;
        }
    }
}
