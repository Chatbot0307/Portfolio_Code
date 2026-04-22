using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;

    public float time;

    void Update()
    {
        Timer();
    }

    private void Timer()
    {
        time += Time.deltaTime;
        int m = (int)time / 60;
        int s = (int)(time% 60);
        timeText.text = m.ToString()+" : "+ s.ToString();
    }
}
