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

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            harmony ??= new Harmony("rucksack.arl.wearableattachment.bypass");
            harmony.CreateClassProcessor(typeof(ArlItemShapeTexturesFromAttributesOnLoadedPatch)).Patch();
        }


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterCollectibleBehaviorClass(
                "ResonantAnchorStability",
                typeof(CollectibleBehaviorResonantAnchorStability)
            );
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            harmony ??= new Harmony("rucksack.arl.wearableattachment.bypass");
            harmony.CreateClassProcessor(typeof(ArlItemWearableAttachmentBypassPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ArlBehaviorWearableAttachmentBypassPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RuckSackGroundStorageCollisionRotatePatch)).Patch();
            harmony.CreateClassProcessor(typeof(RuckSackGroundStorageInteractionHelpPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RuckSackGroundStorageQuartzLightPatch)).Patch();
            harmony.CreateClassProcessor(typeof(NoRepair_GetMergableQuantity_Patch)).Patch();
            harmony.CreateClassProcessor(typeof(NoRepair_TryMergeStacks_Patch)).Patch();
            RuckSackBackpackSlotLimitPatcher.Apply(harmony);
            RuckSackLunchboxPatcher.Apply(harmony);

            Input.InitClient(api);
            RuckSackQuartzLightSystem.InitClient(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            Input.InitServer(api);
            serverHarmony ??= new Harmony("rucksack.backpackslot.limit.server");
            serverHarmony.CreateClassProcessor(typeof(NoRepair_GetMergableQuantity_Patch)).Patch();
            serverHarmony.CreateClassProcessor(typeof(NoRepair_TryMergeStacks_Patch)).Patch();
            RuckSackBackpackSlotLimitPatcher.Apply(serverHarmony);
            RuckSackLunchboxPatcher.Apply(serverHarmony);
            RuckSackQuartzLightSystem.InitServer(api);
        }

        public override void Dispose()
        {
            Input.Dispose();

            harmony?.UnpatchAll(harmony.Id);
            harmony = null;

            serverHarmony?.UnpatchAll(serverHarmony.Id);
            serverHarmony = null;

            RuckSackBackpackSlotLimitPatcher.Reset();
            RuckSackLunchboxPatcher.Reset();

            RuckSackQuartzLightSystem.Dispose();

            base.Dispose();
        }
    }
}
