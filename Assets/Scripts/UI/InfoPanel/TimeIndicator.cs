using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeIndicator : MonoBehaviour, ITooltipSource
{
    public GameObject dial;

    DayTime daytime;

    public bool TryGetTooltipInfo(out string title, out string body, out IGridSource subGrid)
    {
        var hour = (int)daytime.CurGameTime % 12;
        hour = hour == 0 ? 12 : hour;   
        var minute = (int)(daytime.CurGameTime % 1 * 60);
        title = $"{hour}:{minute:D2}";
        subGrid = null;
        body = string.Empty;
        return true;
    }

    // Start is called before the first frame update
    void Start()
    {
        daytime = DayTime.dayTime;
    }

    // Update is called once per frame
    void Update()
    {
        var time = daytime.CurGameTime;
        var total = daytime.GameHoursPerGameDay;
        dial.transform.rotation = Quaternion.Euler(0, 0, 360*(time/total)-180);
    }
}
