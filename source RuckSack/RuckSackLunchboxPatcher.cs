using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace RuckSack
{
    internal static class RuckSackLunchboxPatcher
    {
        private const int ExtraSlots = 2;
        private const string TypesTreeKey = "types";
        private const string LunchboxKey = "lunchbox";
        private const string AttachedValue = "attached";
        private const string SlotPrefix = "slot-";
        private const string LunchboxSlotColor = "#7fdcff";

        private static bool patched;
        private static readonly object patchLock = new();

        internal static void Apply(Harmony harmony)
        {
            if (harmony == null) return;

            lock (patchLock)
            {
                if (patched) return;

                MethodInfo? getQuantitySlots = AccessTools.Method(
                    typeof(CollectibleBehaviorHeldBag),
                    nameof(CollectibleBehaviorHeldBag.GetQuantitySlots),
                    new[] { typeof(ItemStack) }
                );

                MethodInfo? getOrCreateSlots = AccessTools.Method(
                    typeof(CollectibleBehaviorHeldBag),
                    nameof(CollectibleBehaviorHeldBag.GetOrCreateSlots),
                    new[] { typeof(ItemStack), typeof(InventoryBase), typeof(int), typeof(IWorldAccessor) }
                );

                MethodInfo? getTransitionRateMul = AccessTools.Method(
                    typeof(CollectibleObject),
                    nameof(CollectibleObject.GetTransitionRateMul),
                    new[] { typeof(IWorldAccessor), typeof(ItemSlot), typeof(EnumTransitionType) }
                );

                if (getQuantitySlots != null)
                {
                    harmony.Patch(
                        getQuantitySlots,
                        postfix: new HarmonyMethod(typeof(RuckSackLunchboxPatcher), nameof(GetQuantitySlots_Postfix))
                    );
                }

                if (getOrCreateSlots != null)
                {
                    harmony.Patch(
                        getOrCreateSlots,
                        postfix: new HarmonyMethod(typeof(RuckSackLunchboxPatcher), nameof(GetOrCreateSlots_Postfix))
                    );
                }

                if (getTransitionRateMul != null)
                {
                    harmony.Patch(
                        getTransitionRateMul,
                        postfix: new HarmonyMethod(typeof(RuckSackLunchboxPatcher), nameof(GetTransitionRateMul_Postfix))
                    );
                }

                patched = getQuantitySlots != null || getOrCreateSlots != null || getTransitionRateMul != null;
            }
        }

        internal static void Reset()
        {
            lock (patchLock)
            {
                patched = false;
            }
        }

        internal static bool HasLunchboxUpgrade(ItemStack? bagstack)
        {
            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(bagstack)) return false;

            ITreeAttribute? typesTree = bagstack?.Attributes?.GetTreeAttribute(TypesTreeKey);
            if (typesTree == null) return false;

            return typesTree.GetString(LunchboxKey, "none").Equals(AttachedValue, StringComparison.OrdinalIgnoreCase);
        }

        private static void GetQuantitySlots_Postfix(ItemStack bagstack, ref int __result)
        {
            if (HasLunchboxUpgrade(bagstack))
            {
                __result += ExtraSlots;
            }
        }

        private static void GetOrCreateSlots_Postfix(
            CollectibleBehaviorHeldBag __instance,
            ItemStack bagstack,
            InventoryBase parentinv,
            int bagIndex,
            IWorldAccessor world,
            ref List<ItemSlotBagContent> __result)
        {
            if (!HasLunchboxUpgrade(bagstack)) return;

            int baseSlotCount = GetBaseQuantitySlots(bagstack);
            int totalSlotCount = baseSlotCount + ExtraSlots;

            ITreeAttribute slotsTree = GetOrCreateSlotsTree(bagstack);

            for (int slotIndex = baseSlotCount; slotIndex < totalSlotCount; slotIndex++)
            {
                string slotKey = SlotPrefix + slotIndex;
                if (!slotsTree.HasAttribute(slotKey))
                {
                    slotsTree[slotKey] = new ItemstackAttribute(null);
                }

                while (__result.Count <= slotIndex)
                {
                    __result.Add(null);
                }

                ItemSlotBagContent? existingSlot = __result[slotIndex];
                if (existingSlot is RuckSackLunchboxItemSlot lunchboxSlot)
                {
                    lunchboxSlot.HexBackgroundColor = LunchboxSlotColor;
                    lunchboxSlot.CanStoreTags = __instance.GetStorageTags(bagstack);
                    lunchboxSlot.storageType = __instance.GetStorageFlags(bagstack);
                    continue;
                }

                RuckSackLunchboxItemSlot newSlot = new RuckSackLunchboxItemSlot(parentinv, bagIndex, slotIndex, __instance.GetStorageFlags(bagstack));
                newSlot.HexBackgroundColor = LunchboxSlotColor;
                newSlot.CanStoreTags = __instance.GetStorageTags(bagstack);
                newSlot.Itemstack = existingSlot?.Itemstack;
                __result[slotIndex] = newSlot;
            }
        }

        private static void GetTransitionRateMul_Postfix(ItemSlot inSlot, EnumTransitionType transType, ref float __result)
        {
            if (transType == EnumTransitionType.Perish && inSlot is RuckSackLunchboxItemSlot)
            {
                __result *= 0.5f;
            }
        }

        private static int GetBaseQuantitySlots(ItemStack bagstack)
        {
            if (bagstack == null || bagstack.Collectible?.Attributes == null) return 0;
            return bagstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt();
        }

        private static ITreeAttribute GetOrCreateSlotsTree(ItemStack bagstack)
        {
            bagstack.Attributes ??= new TreeAttribute();

            ITreeAttribute? backpackTree = bagstack.Attributes.GetTreeAttribute("backpack");
            if (backpackTree == null)
            {
                bagstack.Attributes["backpack"] = new TreeAttribute();
                backpackTree = bagstack.Attributes.GetTreeAttribute("backpack");
            }

            ITreeAttribute? slotsTree = backpackTree.GetTreeAttribute("slots");
            if (slotsTree == null)
            {
                backpackTree["slots"] = new TreeAttribute();
                slotsTree = backpackTree.GetTreeAttribute("slots");
            }

            return slotsTree;
        }

        private sealed class RuckSackLunchboxItemSlot : ItemSlotBagContent
        {
            public RuckSackLunchboxItemSlot(InventoryBase inventory, int bagIndex, int slotIndex, EnumItemStorageFlags storageType) : base(inventory, bagIndex, slotIndex, storageType)
            {
                HexBackgroundColor = LunchboxSlotColor;
            }

            public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
            {
                return IsFood(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
            }

            public override bool CanHold(ItemSlot sourceSlot)
            {
                return IsFood(sourceSlot) && base.CanHold(sourceSlot);
            }

            private bool IsFood(ItemSlot sourceSlot)
            {
                ItemStack? stack = sourceSlot?.Itemstack;
                if (stack?.Collectible == null) return false;

                TransitionableProperties[]? props = stack.Collectible.GetTransitionableProperties(inventory?.Api?.World, stack, null);
                if (props == null) return false;

                for (int i = 0; i < props.Length; i++)
                {
                    if (props[i]?.Type == EnumTransitionType.Perish)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
