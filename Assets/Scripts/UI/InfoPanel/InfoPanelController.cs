using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanelController : MonoBehaviour
{
    public GameObject Player;
    public IconBar Health;
    public IconBar Mana;
    public IconBar Hunger;

    private void Awake()
    {
        Health.stat = Player.GetComponent<Health>();
        Hunger.stat = Player.GetComponent<Hunger>();
        Mana.stat = Player.GetComponent<Mana>();
    }

}
