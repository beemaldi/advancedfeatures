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
            IgnitionBangleClientInput.Init(api);
        }

        public static void InitServer(ICoreServerAPI api)
        {
            RuckSackNetworking.InitServer(api);
        }

        public static void DisposeClient()
        {
            RuckSackClientInput.Dispose();
            IgnitionBangleClientInput.Dispose();
            RuckSackNetworking.DisposeClient();
        }

        public static void DisposeServer()
        {
            RuckSackNetworking.DisposeServer();
            IgnitionBangleCaster.Reset();
        }

        public static void Dispose()
        {
            DisposeClient();
            DisposeServer();
        }
    }
}
