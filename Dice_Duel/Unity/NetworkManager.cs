using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Main UI")]
    public InputField NickNameInput;
    public GameObject MainUi;

    [Header("Multi UI")]
    public InputField roomInput;
    public Text ListText;
    public Text RoomInfoText;
    public GameObject RoomPanel;
    public GameObject MultiUi;
    public GameObject startButton;

    public PhotonView photonView;

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Update()
    {
        initStartButton();
    }

    #region 서버접속
    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster()
    {
        Debug.Log("서버접속완료");
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        MainUi.SetActive(false);
        MultiUi.SetActive(true);
    }

    public void Disconnect() => PhotonNetwork.Disconnect();

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("연결끊김");
        MainUi.SetActive(true);
        MultiUi.SetActive(false);
    }
    #endregion

    #region 방
    public void CreateRoom() => PhotonNetwork.CreateRoom(roomInput.text, new RoomOptions { MaxPlayers = 2 });
    public override void OnCreatedRoom() => Debug.Log("방만들기완료");

    public void JoinRoom()
    {
        if (PhotonNetwork.JoinRoom(roomInput.text.ToString()))
        {
            Debug.Log(PhotonNetwork.NickName + "님이 들어오셨습니다");
        }
        else
        {
            Debug.Log("방참가실패");
        }
    }

    public override void OnJoinedRoom()
    {
        RoomPanel.SetActive(true);
        RoomRenewal();
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        RoomPanel.SetActive(false);
        RoomRenewal();
    }

    public override void OnCreateRoomFailed(short returnCode, string message) => Debug.Log("방만들기실패");

    public override void OnJoinRoomFailed(short returnCode, string message) => Debug.Log("방참가실패");

    public override void OnPlayerEnteredRoom(Player newPlayer) => RoomRenewal();

    public override void OnPlayerLeftRoom(Player otherPlayer) => RoomRenewal();

    void RoomRenewal()
    {
        ListText.text = "";
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            ListText.text += PhotonNetwork.PlayerList[i].NickName + ((i + 1 == PhotonNetwork.PlayerList.Length) ? "" : "\n");
        RoomInfoText.text = PhotonNetwork.CurrentRoom.Name + " / " + PhotonNetwork.CurrentRoom.PlayerCount + "명 / " + PhotonNetwork.CurrentRoom.MaxPlayers + "최대";
    }
    #endregion

    public void initStartButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public void GameStart() => photonView.RPC("RequestSceneChange", RpcTarget.AllBuffered, "BattleScene");

    [PunRPC]
    private void RequestSceneChange(string sceneName) => PhotonNetwork.LoadLevel(sceneName);
}