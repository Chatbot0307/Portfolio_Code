using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public enum BattleState { Start, ResetTurn, DiceRoll, Action, Result, PC1Win, PC2Win }

public class BattleSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView photonView;

    public Text narrationText;

    public GameObject PC1;
    public GameObject PC2;

    public Transform PC1BattleStation;
    public Transform PC2BattleStation;

    Unit PC1Unit;
    Unit PC2Unit;

    public int Dice1;
    public int Dice2;

    public BattleState state;
    public int Turn;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    [PunRPC]
    private void SetupUnit(bool isPC1, int viewID)
    {
        PhotonView target = PhotonView.Find(viewID);
        if (isPC1)
        {
            var PC1var = target.gameObject;
            PC1Unit = PC1var.GetComponent<Unit>();
        }
        else
        {
            var PC2var = target.gameObject;
            PC2Unit = PC2var.GetComponent<Unit>();
        }
    }

    IEnumerator SetupBattle()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameObject PC1Go = PhotonNetwork.Instantiate("PC1", PC1BattleStation.position, PC1BattleStation.rotation);
            photonView.RPC("SetupUnit", RpcTarget.AllBuffered, true, PC1Go.GetComponent<PhotonView>().ViewID);
        }
        else
        {
            GameObject PC2Go = PhotonNetwork.Instantiate("PC2", PC2BattleStation.position, PC2BattleStation.rotation);
            photonView.RPC("SetupUnit", RpcTarget.AllBuffered, false, PC2Go.GetComponent<PhotonView>().ViewID);
        }

        yield return new WaitForSeconds(1f);

        state = BattleState.DiceRoll;
        DiceRoll();
    }

    void ResetTurn()
    {
        Debug.Log("턴 초기화");
        PC1Unit.Attack = false;
        PC2Unit.Attack = false;

        PC1Unit.Defense = false;
        PC2Unit.Defense = false;

        PC1Unit.Counter = false;
        PC2Unit.Counter = false;

        PC1Unit.PCBehavior = false;
        PC2Unit.PCBehavior = false;

        Turn++;
        state = BattleState.DiceRoll;
        Invoke("DiceRoll", 1f);
    }

    void DiceRoll()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Dice1 = Random.Range(1, 7);
            Dice2 = Random.Range(1, 7);

            photonView.RPC("DiceRollSync", RpcTarget.AllBuffered, Dice1, Dice2);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(Dice1);
            stream.SendNext(Dice2);
        }
        else
        {
            Dice1 = (int)stream.ReceiveNext();
            Dice2 = (int)stream.ReceiveNext();
        }
    }

    [PunRPC]
    void DiceRollSync(int dice1, int dice2)
    {
        narrationText.text = $"{dice1} vs {dice2}";

        state = BattleState.Action;
        Invoke("Action", 1f);
    }

    void Action()
    {
        if (PC1Unit.turnSkip) 
        {
            PC1Unit.PCBehavior = true; 
        }

        if (PC2Unit.turnSkip) 
        {
            PC2Unit.PCBehavior = true;
        }

        if (PC1Unit.PCBehavior && PC2Unit.PCBehavior)
        {
            Debug.Log("행동 완료");
            state = BattleState.Result;
            Invoke("Result", 1f);
        }
    }

    public void OnAttackButton()
    {
        Unit targetUnit = PhotonNetwork.IsMasterClient ? PC1Unit : PC2Unit;
        if (state != BattleState.Action) return;
        
        photonView.RPC("TurnAction", RpcTarget.AllBuffered, "Attack", targetUnit.photonView.ViewID);

        targetUnit.PCBehavior = true;
        Action();
    }

    public void OnDefenseButton()
    {
        Unit targetUnit = PhotonNetwork.IsMasterClient ? PC1Unit : PC2Unit;
        if (state != BattleState.Action) return;

        photonView.RPC("TurnAction", RpcTarget.AllBuffered, "Defense", targetUnit.photonView.ViewID);

        targetUnit.PCBehavior = true;
        Action();
    }

    public void OnCounterButton()
    {
        Unit targetUnit = PhotonNetwork.IsMasterClient ? PC1Unit : PC2Unit;
        if (state != BattleState.Action) return;

        photonView.RPC("TurnAction", RpcTarget.AllBuffered, "Counter", targetUnit.photonView.ViewID);

        targetUnit.PCBehavior = true;
        Action();
    }

    [PunRPC]
    public void TurnAction(string action, int viewID)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView == null) return;

        Unit targetUnit = targetView.GetComponent<Unit>();
        if (targetUnit == null) return;

        if (targetView.IsMine)
        {
            switch (action)
            {
                case "Attack":
                    targetUnit.Attack = true;
                    break;
                case "Defense":
                    targetUnit.Defense = true;
                    break;
                case "Counter":
                    targetUnit.Counter = true;
                    break;
            }
        }
    }

    void Result()
    {
        if (PhotonNetwork.LocalPlayer.IsLocal)
        {
            string resultMessage = "";
            Debug.Log($"PC1 상태 - Attack: {PC1Unit.Attack}, Defense: {PC1Unit.Defense}, Counter: {PC1Unit.Counter}, HP: {PC1Unit.currentHP}");
            Debug.Log($"PC2 상태 - Attack: {PC2Unit.Attack}, Defense: {PC2Unit.Defense}, Counter: {PC2Unit.Counter}, HP: {PC2Unit.currentHP}");

            if (PC1Unit.turnSkip)
            {
                PC1Unit.turnSkip = false;

                if (PC2Unit.Attack)
                {
                    PC1Unit.currentHP -= Dice2;
                    resultMessage = $"PC2의 공격 \nPC1HP : {PC1Unit.currentHP}";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            if (PC2Unit.turnSkip)
            {
                PC2Unit.turnSkip = false;

                if (PC1Unit.Attack)
                {
                    PC2Unit.currentHP -= Dice1;
                    resultMessage = $"PC1의 공격 \nPC2HP : {PC2Unit.currentHP}";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            if (PC1Unit.Attack && PC2Unit.Attack)
            {
                Debug.Log("Attack is Both True");
                Debug.Log($"Dice1 = {Dice1} Dice2 = {Dice2}");
                if (Dice1 > Dice2)
                {
                    PC2Unit.currentHP -= Dice1;
                    resultMessage = $"PC1의 공격 \nPC2HP : {PC2Unit.currentHP}";
                }
                else if (Dice1 < Dice2)
                {
                    PC1Unit.currentHP -= Dice2;
                    resultMessage = $"PC2의 공격 \nPC1HP : {PC1Unit.currentHP}";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            else if (PC1Unit.Defense && PC2Unit.Defense)
            {
                resultMessage = "아무일도 일어나지 않았다.";
            }

            else if (PC1Unit.Counter && PC2Unit.Counter)
            {
                resultMessage = "아무일도 일어나지 않았다.";
            }

            else if (PC1Unit.Attack && PC2Unit.Defense)
            {
                if (Dice1 > Dice2)
                {
                    PC2Unit.currentHP -= Dice1 - Dice2;
                    resultMessage = $"PC1의 공격 \nPC2HP : {PC2Unit.currentHP}";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            else if (PC1Unit.Defense && PC2Unit.Attack)
            {
                if (Dice1 < Dice2)
                {
                    PC1Unit.currentHP -= Dice2 - Dice1;
                    resultMessage = $"PC2의 공격 \nPC1HP : {PC1Unit.currentHP}";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            else if (PC1Unit.Counter && PC2Unit.Attack)
            {
                if (Dice1 < Dice2)
                {
                    PC2Unit.currentHP -= Dice2 + Dice1;
                    resultMessage = $"PC1의 반격 \nPC2HP : {PC2Unit.currentHP}";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            else if (PC1Unit.Attack && PC2Unit.Counter)
            {
                if (Dice1 > Dice2)
                {
                    PC1Unit.currentHP -= Dice1 + Dice2;
                    resultMessage = $"PC2의 반격 \nPC1HP : {PC1Unit.currentHP}";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            else if (PC1Unit.Defense && PC2Unit.Counter)
            {
                if (Dice1 > Dice2)
                {
                    PC2Unit.turnSkip = true;
                    resultMessage = "PC2의 흐트러짐 \n다음 한턴을 쉬게된다";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            else if (PC1Unit.Counter && PC2Unit.Defense)
            {
                if (Dice1 < Dice2)
                {
                    PC1Unit.turnSkip = true;
                    resultMessage = "PC1의 흐트러짐 \n다음 한턴을 쉬게된다";
                }
                else
                {
                    resultMessage = "아무일도 일어나지 않았다.";
                }
            }

            photonView.RPC("SyncResultUI", RpcTarget.All, resultMessage, PC1Unit.currentHP, PC2Unit.currentHP);
        }
    }

    [PunRPC]
    void SyncResultUI(string message, int pc1HP, int pc2HP)
    {
        narrationText.text = message;

        PC1Unit.currentHP = pc1HP;
        PC2Unit.currentHP = pc2HP;

        state = BattleState.ResetTurn;
        Invoke("ResetTurn", 3f);
    }

    void PC1win()
    {

    }

    void PC2win()
    {

    }
}