//using System;
//using System.Collections.Concurrent;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading.Tasks;
//using Google.Protobuf;
//using GameSync;

//public class UdpServer
//{
//    private UdpClient udp;
//    private bool running;
//    private ConcurrentDictionary<string, IPEndPoint> clients = new();
//    private ConcurrentDictionary<string, int> lastSeq = new();

//    public void Start(int port)
//    {
//        udp = new UdpClient(port);
//        running = true;
//        _ = ReceiveLoop();
//        Console.WriteLine("Server listening...");
//    }

//    public void Stop()
//    {
//        running = false;
//        udp.Close();
//    }

//    private async Task ReceiveLoop()
//    {
//        while (running)
//        {
//            try
//            {
//                var res = await udp.ReceiveAsync();
//                var ep = res.RemoteEndPoint;
//                var data = res.Buffer;

//                GameMessage msg;
//                try
//                {
//                    msg = GameMessage.Parser.ParseFrom(data);
//                }
//                catch
//                {
//                    Console.WriteLine("Failed to parse incoming packet.");
//                    continue;
//                }

//                // If Join: assign new playerId and reply to this endpoint
//                if (msg.PayloadCase == GameMessage.PayloadOneofCase.Join)
//                {
//                    var newId = Guid.NewGuid().ToString();
//                    clients[newId] = ep;
//                    lastSeq[newId] = 0;

//                    var assign = new GameMessage
//                    {
//                        PlayerId = newId,
//                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
//                        Join = new Join()
//                    };
//                    Send(assign, ep);

//                    var joinBroadcast = new GameMessage
//                    {
//                        PlayerId = newId,
//                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
//                        Join = new Join()
//                    };
//                    BroadcastExcept(joinBroadcast, newId);

//                    foreach (var kv in clients)
//                    {
//                        if (kv.Key == newId) continue;
//                        var existing = new GameMessage
//                        {
//                            PlayerId = kv.Key,
//                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
//                            Join = new Join()
//                        };
//                        Send(existing, ep);
//                    }

//                    Console.WriteLine($"Client joined: {newId}");
//                    continue;
//                }

//                var senderId = msg.PlayerId;
//                if (string.IsNullOrEmpty(senderId) || !clients.ContainsKey(senderId))
//                {
//                    Console.WriteLine($"Unknown sender or missing playerId. Ignored. Ep: {ep}");
//                    continue;
//                }

//                clients[senderId] = ep;

//                switch (msg.PayloadCase)
//                {
//                    case GameMessage.PayloadOneofCase.Position:
//                        var seq = msg.Position.Sequence;
//                        if (!lastSeq.ContainsKey(senderId) || seq > lastSeq[senderId])
//                        {
//                            lastSeq[senderId] = seq;
//                            BroadcastExcept(msg, senderId);
//                        }
//                        else
//                        {
//                            // stale
//                        }
//                        break;

//                    case GameMessage.PayloadOneofCase.AnimState:
//                    case GameMessage.PayloadOneofCase.AnimTrigger:
//                    case GameMessage.PayloadOneofCase.ItemEquip:
//                        BroadcastExcept(msg, senderId);
//                        break;

//                    case GameMessage.PayloadOneofCase.Leave:
//                        clients.TryRemove(senderId, out var _);
//                        lastSeq.TryRemove(senderId, out var _);
//                        var leaveMsg = new GameMessage
//                        {
//                            PlayerId = senderId,
//                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
//                            Leave = new Leave()
//                        };
//                        BroadcastExcept(leaveMsg, senderId);
//                        Console.WriteLine($"Client left: {senderId}");
//                        break;

//                    default:
//                        break;
//                }
//            }
//            catch (ObjectDisposedException) { break; }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Server error: {ex}");
//            }
//        }
//    }

//    private void Send(GameMessage msg, IPEndPoint ep)
//    {
//        try
//        {
//            var data = msg.ToByteArray();
//            udp.SendAsync(data, data.Length, ep);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Send error: {ex}");
//        }
//    }

//    private void BroadcastExcept(GameMessage msg, string exceptPlayerId)
//    {
//        var data = msg.ToByteArray();
//        foreach (var kv in clients)
//        {
//            if (kv.Key == exceptPlayerId) continue;
//            udp.SendAsync(data, data.Length, kv.Value);
//        }
//    }
//}
