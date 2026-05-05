using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace RuckSack
{
    [HarmonyPatch(typeof(ItemWearable), "GetMergableQuantity")]
    public static class NoRepair_GetMergableQuantity_Patch
    {
        public static bool Prefix(
            ItemWearable __instance,
            ItemStack sinkStack,
            ItemStack sourceStack,
            EnumMergePriority priority,
            ref int __result)
        {
            if (priority == EnumMergePriority.DirectMerge &&
                sinkStack?.Collectible?.HasBehavior<NoRepairClothesBehavior>() == true)
            {
                float repairStrength =
                    sourceStack?.ItemAttributes?["clothingRepairStrength"].AsFloat() ?? 0f;

                if (repairStrength > 0f)
                {
                    __result = 0;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ItemWearable), "TryMergeStacks")]
    public static class NoRepair_TryMergeStacks_Patch
    {
        public static bool Prefix(ItemWearable __instance, ItemStackMergeOperation op)
        {
            if (op?.SinkSlot?.Itemstack?.Collectible?.HasBehavior<NoRepairClothesBehavior>() == true)
            {
                float repairStrength =
                    op.SourceSlot?.Itemstack?.ItemAttributes?["clothingRepairStrength"].AsFloat() ?? 0f;

                if (repairStrength > 0f)
                {
                    op.MovedQuantity = 0;
                    return false;
                }
            }

            return true;
        }
    }
}