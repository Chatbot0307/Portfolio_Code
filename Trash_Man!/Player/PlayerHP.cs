using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    public static PlayerHP Instance { get; private set; }

    public int playercurhp;
    public int playermaxhp = 20;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

    }

    private void Start()
    {
        playercurhp = playermaxhp;
    }

    public void DiscountHP()
    {
        if (playercurhp <= 0) return;

        playercurhp = Mathf.Max(playercurhp, 0);
        playercurhp--;

        if(playercurhp <= 0)
        {
            PlayerDie();
        }
    }

    private void PlayerDie()
    {
        Debug.Log("Die");
    }
}
