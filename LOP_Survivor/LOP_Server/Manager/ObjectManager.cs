//using GameSync;
//using System.Collections.Concurrent;
//using System.Linq;
//using System.Threading;

//public class ObjectManager
//{
//    private static ObjectManager instance;
//    public static ObjectManager Instance => instance ??= new ObjectManager();

//    private int _nextMonsterId = 1000;
//    private readonly ConcurrentStack<int> reusableIds = new();

//    public int IssueId() => reusableIds.TryPop(out int id) ? id : Interlocked.Increment(ref _nextMonsterId);
//    public void RecycleId(int id) { if (id > 1000) reusableIds.Push(id); }

//    public void RebalanceAuthority(ServerContext ctx)
//    {
//        var clients = ctx.Clients.Keys.ToList();
//        if (clients.Count == 0) return;

//        int index = 0;
//        foreach (var netId in ctx.NetworkObjectDetails.Keys.Where(id => id > 1000))
//        {
//            string targetOwner = clients[index % clients.Count];
//            ctx.NetworkObjects[netId] = targetOwner;
//            ctx.NetworkObjectDetails[netId].OwnerPlayerId = targetOwner;

//            ctx.Broadcast(new GameMessage
//            {
//                TransferOwnership = new TransferOwnership { NetworkId = netId, NewOwnerPlayerId = targetOwner }
//            });
//            index++;
//        }
//    }
//}