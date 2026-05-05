
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.Server;

namespace RuckSack
{
    public sealed class RuckSackModSystem : ModSystem
    {
        private Harmony? harmony;
        private Harmony? serverHarmony;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            harmony ??= new Harmony("rucksack.arl.wearableattachment.bypass");
            harmony.PatchAll();
            RuckSackBackpackSlotLimitPatcher.Apply(harmony);

            Input.InitClient(api);
            RuckSackQuartzLightSystem.InitClient(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            Input.InitServer(api);
            serverHarmony ??= new Harmony("rucksack.backpackslot.limit.server");
            RuckSackBackpackSlotLimitPatcher.Apply(serverHarmony);
            RuckSackQuartzLightSystem.InitServer(api);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;

            serverHarmony?.UnpatchAll(serverHarmony.Id);
            serverHarmony = null;

            RuckSackBackpackSlotLimitPatcher.Reset();

            RuckSackQuartzLightSystem.Dispose();

            base.Dispose();
        }
    }
}
