//using System;

//public class ServerManager
//{
//    private static ServerManager instance;
//    public static ServerManager Instance => instance ??= new ServerManager();

//    public ServerContext Context { get; private set; }
//    public ObjectManager Objects { get; private set; }
//    public AuthorityHandler Authority { get; private set; }

//    public void Initialize(UdpTransport transport)
//    {
//        Context = new ServerContext(transport);
//        Objects = new ObjectManager(Context);

//        Authority = new AuthorityHandler();

//        Console.WriteLine("[ServerManager] System Initialize complete");
//    }

//    public void OnServerStart() => SaveManager.Load(Context);
//    public void OnServerStop() => SaveManager.Save(Context);
//}