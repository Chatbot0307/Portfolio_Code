using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class Unit : MonoBehaviourPunCallbacks, IPunObservable
{
    public string unitName;

    public int maxHP = 20;
    public int currentHP = 20;

    public bool Attack = false;
    public bool Defense = false;
    public bool Counter = false;

    public bool turnSkip = false;

    public bool PCBehavior = false;

    public PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) 
        {
            if(photonView.IsMine)
            {
                stream.SendNext(currentHP);
                stream.SendNext(Attack);
                stream.SendNext(Defense);
                stream.SendNext(Counter);
                stream.SendNext(turnSkip);
                stream.SendNext(PCBehavior);
            }
        }
        else 
        {
            if(!photonView.IsMine) 
            {
                currentHP = (int)stream.ReceiveNext();
                Attack = (bool)stream.ReceiveNext();
                Defense = (bool)stream.ReceiveNext();
                Counter = (bool)stream.ReceiveNext();
                turnSkip = (bool)stream.ReceiveNext();
                PCBehavior = (bool)stream.ReceiveNext();
            }
        }
    }
}
