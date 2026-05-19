using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace RuckSack
{
    [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMergableQuantity))]
    public static class NoRepair_GetMergableQuantity_Patch
    {
        public static bool Prefix(
            ItemStack sinkStack,
            ItemStack sourceStack,
            EnumMergePriority priority,
            ref EnumHandling handling,
            ref int __result)
        {
            if (priority == EnumMergePriority.DirectMerge &&
                sinkStack.Collectible.HasBehavior<NoRepairClothesBehavior>() &&
                (sourceStack.ItemAttributes?["clothingRepairStrength"].AsFloat(0) ?? 0) > 0)
            {
                handling = EnumHandling.PreventDefault;
                __result = 0;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.TryMergeStacks))]
    public static class NoRepair_TryMergeStacks_Patch
    {
        public static bool Prefix(ItemStackMergeOperation op, ref EnumHandling handling)
        {
            if (op.CurrentPriority == EnumMergePriority.DirectMerge &&
                op.SinkSlot.Itemstack.Collectible.HasBehavior<NoRepairClothesBehavior>() &&
                (op.SourceSlot.Itemstack.ItemAttributes?["clothingRepairStrength"].AsFloat(0) ?? 0) > 0)
            {
                handling = EnumHandling.PreventDefault;
                op.MovedQuantity = 0;
                return false;
            }

            return true;
        }
    }
}
