using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Direction; 
    public float Power;       
    public NetworkBool IsFiring; 
}