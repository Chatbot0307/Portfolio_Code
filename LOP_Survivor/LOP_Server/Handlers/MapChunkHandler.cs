
using GameSync;
using Island;
using System;
using System.Net;

/// <summary>
/// 특정 좌표의 청크 데이터를 요청받아 전송하거나, 서버가 능동적으로 전송할 때 사용됩니다.
/// </summary>
public class MapChunkHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var request = msg.MapChunkData;
        if (request == null)
            return;

        var chunk = ServerMapManager.Instance.GetOrLoadChunk(request.X, request.Z);
        if (chunk == null)
        {
            Console.WriteLine($"[Server] Failed to get chunk ({request.X}, {request.Z})");
            return;
        }

        SendChunk(chunk, sender, ctx);
        Console.WriteLine($"[Server] Sent Chunk ({chunk.X}, {chunk.Z}) to {msg.PlayerId}");
    }

    public static void SendChunk(ServerChunk chunk, IPEndPoint ep, ServerContext ctx)
    {
        var response = new GameMessage
        {
            PlayerId = "SERVER",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            MapChunkData = new MapChunkData
            {
                X = chunk.X,
                Z = chunk.Z
            }
        };

        response.MapChunkData.Blocks.AddRange(VoxelData.Flatten(chunk.Blocks));
        ctx.Send(response, ep);
    }
}
