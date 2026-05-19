using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace RuckSack
{
    public static class Input
    {
        public static void InitClient(ICoreClientAPI api)
        {
            RuckSackNetworking.InitClient(api);
            RuckSackClientInput.Init(api);
        }

        public static void InitServer(ICoreServerAPI api)
        {
            RuckSackNetworking.InitServer(api);
        }

        public static void DisposeClient()
        {
            RuckSackClientInput.Dispose();
            RuckSackNetworking.DisposeClient();
        }

        public static void DisposeServer()
        {
            RuckSackNetworking.DisposeServer();
        }

        public static void Dispose()
        {
            DisposeClient();
            DisposeServer();
        }
    }
}
