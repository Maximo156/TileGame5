using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayTime : MonoBehaviour
{
    public float GameHoursPerMinute;
    public int GameHoursPerGameDay = 24;
    public LightTileMap Display;
    public float CurGameTime = 6;

    private void Update()
    {
        CurGameTime += GameHoursPerMinute * Time.deltaTime / 60;
        CurGameTime %= GameHoursPerGameDay;
    }

    private void FixedUpdate()
    {
        Display.SetColor(ChunkManager.CurRealm.Generator.GetColor(GameHoursPerGameDay, CurGameTime));
    }
}
