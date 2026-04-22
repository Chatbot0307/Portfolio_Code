using System;

class Program
{
    static void Main(string[] args)
    {
        var server = new GameServer();
        server.Start(11000);
        Console.WriteLine("UDP server started on port 11000. Press any key to stop.");
        Console.ReadKey();
        server.Stop();
    }   
}
