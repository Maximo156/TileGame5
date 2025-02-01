using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EntityStatistics;

public class TempStatsDisplay : MonoBehaviour
{
    public EntityStats playerStats;
    public SingleChildLayoutController display;
    private void Awake()
    {
        playerStats.OnStatChanged += OnStatsUpdated;
    }

    private void Start()
    {
        display.Render(playerStats);
    }

    void OnStatsUpdated(EntityStats.Stat _)
    {
        display.Render(playerStats);
    }
}
