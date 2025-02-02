using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCollisionDetection : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.GetComponent<VisualCollisionDisable>()?.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        collision.GetComponent<VisualCollisionDisable>()?.SetActive(false);
    }
}
