
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuckSack
{
    [HarmonyPatch(typeof(BlockEntityGroundStorage))]
    internal static class RuckSackGroundStorageQuartzLightPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityGroundStorage.Initialize))]
        private static void Initialize_Postfix(BlockEntityGroundStorage __instance, ICoreAPI api)
        {
            if (__instance == null || api == null) return;
            if (api.Side != EnumAppSide.Client) return;

            TryUpdateLight(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityGroundStorage.FromTreeAttributes))]
        private static void FromTreeAttributes_Postfix(BlockEntityGroundStorage __instance, ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (__instance?.Api == null) return;
            if (__instance.Api.Side != EnumAppSide.Client) return;

            TryUpdateLight(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityGroundStorage.OnBlockRemoved))]
        private static void OnBlockRemoved_Postfix(BlockEntityGroundStorage __instance)
        {
            if (__instance?.Api == null) return;
            if (__instance.Api.Side != EnumAppSide.Client) return;

            TryRemoveLight(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityGroundStorage.OnBlockUnloaded))]
        private static void OnBlockUnloaded_Postfix(BlockEntityGroundStorage __instance)
        {
            if (__instance?.Api == null) return;
            if (__instance.Api.Side != EnumAppSide.Client) return;

            TryRemoveLight(__instance);
        }

        private static void TryUpdateLight(BlockEntityGroundStorage be)
        {
            BlockPos? pos;
            try
            {
                pos = be.Pos;
            }
            catch
            {
                return;
            }

            if (pos == null) return;

            ItemStack? found = null;

            try
            {
                if (be.Inventory != null && !be.Inventory.Empty)
                {
                    foreach (ItemSlot slot in be.Inventory)
                    {
                        ItemStack? stack = slot?.Itemstack;
                        if (stack == null) continue;

                        if (RuckSackQuartzLightSystem.TryGetQuartzLightRgbFromRuckSackStack(stack, out _))
                        {
                            found = stack;
                            break;
                        }
                    }
                }
            }
            catch
            {
                
            }

            RuckSackQuartzLightSystem.ClientUpdateGroundStorageLight(pos.X, pos.Y, pos.Z, found);
        }

        private static void TryRemoveLight(BlockEntityGroundStorage be)
        {
            BlockPos? pos;
            try
            {
                pos = be.Pos;
            }
            catch
            {
                return;
            }

            if (pos == null) return;

            RuckSackQuartzLightSystem.ClientRemoveGroundStorageLight(pos.X, pos.Y, pos.Z);
        }
    }
}
