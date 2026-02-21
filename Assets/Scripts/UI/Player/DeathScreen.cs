using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenBehaviour : MonoBehaviour
{
    public float AnimationTime;
    public Image img;
    public Gradient gradient;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerRespawner.OnPlayerRespawn += PlayDeathAnimation;
        gameObject.SetActive(false);
    }

    void PlayDeathAnimation(PlayerRespawner respawner, Action callback)
    {
       DeathAnimation(respawner, callback);
    }

    async void DeathAnimation(PlayerRespawner respawner, Action callback)
    {
        var startTime = Time.time;
        var endTime = startTime + AnimationTime;
        bool spawned = false;
        gameObject.SetActive(true);
        while (Time.time < endTime)
        {
            var per = (Time.time - startTime) / AnimationTime;
            img.color = gradient.Evaluate(per);
            if(!spawned && per > .5)
            {
                await respawner.TriggerRespawn(callback);
                spawned = true;
            }
            await Awaitable.NextFrameAsync();
        }
        gameObject.SetActive(false);
    }
}
