using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private TextMeshProUGUI timeText;
    public PlayerStats player;

    public float time;

    void Start()
    {
        
    }

    void Update()
    {
        Timear();
    }

    private void Timear()
    {
        time += Time.deltaTime;
        int h = (int)time / 60;
        int m = (int)(time% 60);
        timeText.text = "Time: " + h.ToString()+" : "+ m.ToString();
    }
   

}
