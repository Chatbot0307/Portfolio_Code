using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MessagePack;
using Google.Protobuf;
using GameSync;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class LopRPCAttribute : Attribute { }

public class RpcMethodInfo
{
    public MethodInfo Method { get; set; }
}

public class LopRpcSystem
{
    private readonly Dictionary<int, RpcMethodInfo> hashToMethod = new();
    public delegate GameObject NetworkObjectResolver(int networkId);
    private NetworkObjectResolver objectResolver;

    public LopRpcSystem() { }

    public void SetObjectResolver(NetworkObjectResolver resolver)
    {
        objectResolver = resolver;
    }

    public void RegisterRpcComponent(object component)
    {
        var componentType = component.GetType();
        string className = componentType.FullName;

        foreach (var m in componentType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.GetCustomAttribute<LopRPCAttribute>() != null))
        {
            string fullName = $"{className}.{m.Name}";
            int hash = fullName.GetHashCode();

            if (hashToMethod.ContainsKey(hash)) continue;

            hashToMethod[hash] = new RpcMethodInfo { Method = m };
        }
    }


    public void OnReceive(GameMessage msg)
    {
        var rpc = msg.Rpc;
        if (rpc == null) return;

        if (!hashToMethod.TryGetValue(rpc.MethodHash, out var methodInfo)) return;

        GameObject targetObject = objectResolver?.Invoke(rpc.NetworkId);
        if (targetObject == null)
        {
            Debug.LogWarning($"Target GameObject not found for NetworkId: {rpc.NetworkId}");
            return;
        }

        var targetComponent = targetObject.GetComponent(methodInfo.Method.DeclaringType);
        if (targetComponent == null)
        {
            Debug.LogError($"Target Component {methodInfo.Method.DeclaringType.Name} not found on NetworkId {rpc.NetworkId}");
            return;
        }

        var args = MessagePackSerializer.Deserialize<object[]>(rpc.Parameters.ToByteArray());
        methodInfo.Method.Invoke(targetComponent, args);
    }


    public GameMessage CreateRpcMessage(object component, string methodName, params object[] args)
    {
        string className = component.GetType().FullName;
        string fullName = $"{className}.{methodName}";
        int hash = fullName.GetHashCode();

        if (!hashToMethod.ContainsKey(hash))
            throw new Exception($"RPC method not found: {fullName}");

        var networkIdentity = (component as Component)?.GetComponent<NetworkIdentity>();
        if (networkIdentity == null || networkIdentity.NetworkId == 0)
        {
            throw new Exception($"Component {className} does not have a valid NetworkIdentity (ID: {networkIdentity?.NetworkId}).");
        }

        var bytes = MessagePackSerializer.Serialize(args);

        var msg = new GameMessage
        {
            PlayerId = "Placeholder",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Rpc = new RPC
            {
                NetworkId = networkIdentity.NetworkId,
                MethodHash = hash,                    
                Parameters = ByteString.CopyFrom(bytes)
            }
        };
        return msg;
    }
}