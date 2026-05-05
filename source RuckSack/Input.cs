
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
    }
}
