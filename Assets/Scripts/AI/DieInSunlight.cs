using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieInSunlight : MonoBehaviour
{
    public int DamagePerHit = 10;
    HitIngress hitIngress;
    // Start is called before the first frame update
    void Start()
    {
        hitIngress = GetComponent<HitIngress>();
    }

    private void OnEnable()
    {
        StartCoroutine(CheckSunlight());
    }

    private IEnumerator CheckSunlight()
    {
        while (true)
        {
            if (DayTime.dayTime && !DayTime.dayTime.IsNight)
            {
                hitIngress.Hit(new HitData() { Damage = DamagePerHit });
            }
            yield return new WaitForSeconds(1);
        }
    }
}
