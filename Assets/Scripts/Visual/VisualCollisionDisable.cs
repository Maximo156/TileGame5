using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCollisionDisable : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer Renderer;

    private void Awake()
    {
        SetActive(false);
    }

    public void SetActive(bool active)
    {
        if (animator != null)
        {
            animator.enabled = active;
        }
        Renderer.enabled = active;
    }
}
