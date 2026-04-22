using System;

class Program
{
    static void Main(string[] args)
    {
        var server = new GameServer();
        AppDomain.CurrentDomain.ProcessExit += (s, e) => server.Stop();

        server.Start(11000);
        Console.WriteLine("UDP server started on port 11000");
        Console.WriteLine("Press 'ESC' to stop safely.");

        while (true)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Escape) break;
        }

        server.Stop();
        Console.WriteLine("Server stopped and state saved.");
    }   
}
