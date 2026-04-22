using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Button exitButton;

    void Start()
    {
        // 버튼 클릭 이벤트 등록
        startButton.onClick.AddListener(OnStartButtonClicked);
        settingButton.onClick.AddListener(OnOptionsButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    void OnStartButtonClicked()
    {
        CircleEffect.Instance.LoadScene("KJH");
    }

    void OnOptionsButtonClicked()
    {
        if (settingButton != null)
            settingButton.transform.localScale = Vector3.zero;

        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            settingPanel.transform.localScale = Vector3.zero; 
            settingPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
    }


    void OnExitButtonClicked()
    {
        Application.Quit();
    }
}
