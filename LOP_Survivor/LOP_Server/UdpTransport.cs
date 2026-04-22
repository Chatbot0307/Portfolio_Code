using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

/// <summary>
/// 데이터 송수신 담당.
/// </summary>
public class UdpTransport
{
    private UdpClient udp;
    private bool running;
    public event Action<byte[], IPEndPoint> OnDataReceived;

    public void Start(int port)
    {
        udp = new UdpClient(port);
        running = true;
        _ = ReceiveLoop();
    }

    public void Stop()
    {
        running = false;
        udp?.Close();
    }

    private async Task ReceiveLoop()
    {
        while (running)
        {
            try
            {
                var res = await udp.ReceiveAsync();
                OnDataReceived?.Invoke(res.Buffer, res.RemoteEndPoint);
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset) // 10054 에러
            {
                // 클라이언트 비정상 종료 시 흔히 발생. 로그 남기고 계속 진행.
                Console.WriteLine($"[Warning] UDP Receive Error 10054 (ConnectionReset): Remote host forcibly closed connection. Client likely disconnected.");
                // 특정 클라이언트 IP를 식별하고 관리 목록에서 제거하는 로직을 추가할 수도 있음 (필요하다면)
                continue; // 루프 계속
            }
            catch (ObjectDisposedException)
            {
                // UdpClient가 닫혔으므로 루프 종료
                Console.WriteLine("UdpClient has been disposed. Stopping ReceiveLoop.");
                break; 
            }
            catch (Exception ex)
            {
                // 예상치 못한 다른 에러 로깅
                Console.WriteLine($"[Error] Unexpected error in ReceiveLoop: {ex.ToString()}");
                // 심각한 경우 루프를 중단하거나, 잠시 대기 후 계속할 수 있음
                await Task.Delay(10); // 에러 반복 방지용 짧은 대기
                // break; // 필요 시 루프 중단
            }
        }
    }

    public Task SendAsync(byte[] data, IPEndPoint ep)
    {
        return udp.SendAsync(data, data.Length, ep);
    }
}
