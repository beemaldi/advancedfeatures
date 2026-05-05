
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace RuckSack
{
    internal static class RuckSackBackpackSlotLimitPatcher
    {
        private static bool patchedPlayerInventoryManager;
        private static bool patchedHudInventory;
        private static readonly object patchLock = new();

        internal static void Reset()
        {
            lock (patchLock)
            {
                patchedPlayerInventoryManager = false;
                patchedHudInventory = false;
            }
        }

        internal static void Apply(Harmony harmony)
        {
            if (harmony == null) return;

            lock (patchLock)
            {
                if (!patchedPlayerInventoryManager)
                {
                    bool anyPatched = false;

                    MethodInfo? tryTransferTo = AccessTools.Method(
                        typeof(PlayerInventoryManager),
                        nameof(PlayerInventoryManager.TryTransferTo),
                        new[] { typeof(ItemSlot), typeof(ItemSlot), typeof(ItemStackMoveOperation).MakeByRefType() }
                    );

                    MethodInfo? getBestSuitedSlot_New = AccessTools.Method(
                        typeof(PlayerInventoryManager),
                        nameof(PlayerInventoryManager.GetBestSuitedSlot),
                        new[] { typeof(ItemSlot), typeof(bool), typeof(ItemStackMoveOperation), typeof(List<ItemSlot>) }
                    );
                    MethodInfo? getBestSuitedSlot_Old = AccessTools.Method(
                        typeof(PlayerInventoryManager),
                        nameof(PlayerInventoryManager.GetBestSuitedSlot),
                        new[] { typeof(ItemSlot), typeof(ItemStackMoveOperation), typeof(List<ItemSlot>) }
                    );

                    try
                    {
                        if (tryTransferTo != null)
                        {
                            harmony.Patch(
                                tryTransferTo,
                                prefix: new HarmonyMethod(typeof(RuckSackBackpackSlotLimitPatcher), nameof(TryTransferTo_Prefix))
                            );
                            anyPatched = true;
                        }

                        if (getBestSuitedSlot_New != null)
                        {
                            harmony.Patch(
                                getBestSuitedSlot_New,
                                prefix: new HarmonyMethod(typeof(RuckSackBackpackSlotLimitPatcher), nameof(GetBestSuitedSlot_New_Prefix))
                            );
                            anyPatched = true;
                        }

                        if (getBestSuitedSlot_Old != null)
                        {
                            harmony.Patch(
                                getBestSuitedSlot_Old,
                                prefix: new HarmonyMethod(typeof(RuckSackBackpackSlotLimitPatcher), nameof(GetBestSuitedSlot_Old_Prefix))
                            );
                            anyPatched = true;
                        }
                    }
                    catch
                    {
                        
                        anyPatched = false;
                    }

                    if (anyPatched)
                    {
                        patchedPlayerInventoryManager = true;
                    }
                }
                if (!patchedHudInventory)
                {
                    bool anyPatched = false;
                    MethodInfo? inventoryBaseActivateSlot = ResolveInventoryBaseActivateSlot();
                    MethodInfo? inventoryBaseTryFlipItems = ResolveInventoryBaseTryFlipItems();

                    try
                    {
                        if (inventoryBaseActivateSlot != null)
                        {
                            harmony.Patch(
                                inventoryBaseActivateSlot,
                                prefix: new HarmonyMethod(typeof(RuckSackBackpackSlotLimitPatcher), nameof(InventoryBase_ActivateSlot_Prefix))
                            );
                            anyPatched = true;
                        }

                        if (inventoryBaseTryFlipItems != null)
                        {
                            harmony.Patch(
                                inventoryBaseTryFlipItems,
                                prefix: new HarmonyMethod(typeof(RuckSackBackpackSlotLimitPatcher), nameof(InventoryBase_TryFlipItems_Prefix))
                            );
                            anyPatched = true;
                        }
                    }
                    catch
                    {
                        
                        anyPatched = false;
                    }

                    if (anyPatched)
                    {
                        patchedHudInventory = true;
                    }
                }
            }
        }

        private static MethodInfo? ResolveInventoryBaseActivateSlot()
        {
            
            MethodInfo? mi = AccessTools.Method(
                typeof(InventoryBase),
                "ActivateSlot",
                new[] { typeof(int), typeof(ItemSlot), typeof(ItemStackMoveOperation).MakeByRefType() }
            );
            if (mi != null) return mi;
            foreach (MethodInfo m in typeof(InventoryBase).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!m.Name.EndsWith(".ActivateSlot", StringComparison.Ordinal)) continue;

                ParameterInfo[] p = m.GetParameters();
                if (p.Length != 3) continue;
                if (p[0].ParameterType != typeof(int)) continue;
                if (p[1].ParameterType != typeof(ItemSlot)) continue;
                if (p[2].ParameterType != typeof(ItemStackMoveOperation).MakeByRefType()) continue;

                return m;
            }

            return null;
        }

        private static MethodInfo? ResolveInventoryBaseTryFlipItems()
        {
            
            MethodInfo? mi = AccessTools.Method(
                typeof(InventoryBase),
                "TryFlipItems",
                new[] { typeof(int), typeof(ItemSlot) }
            );
            if (mi != null) return mi;
            foreach (MethodInfo m in typeof(InventoryBase).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!m.Name.EndsWith(".TryFlipItems", StringComparison.Ordinal)) continue;

                ParameterInfo[] p = m.GetParameters();
                if (p.Length != 2) continue;
                if (p[0].ParameterType != typeof(int)) continue;
                if (p[1].ParameterType != typeof(ItemSlot)) continue;

                return m;
            }

            return null;
        }

        private static bool TryTransferTo_Prefix(
            PlayerInventoryManager __instance,
            ItemSlot sourceSlot,
            ItemSlot targetSlot,
            ref ItemStackMoveOperation op,
            ref object __result)
        {
            if (sourceSlot?.Itemstack == null || targetSlot == null) return true;
            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(sourceSlot.Itemstack)) return true;
            if (targetSlot is not ItemSlotBackpack) return true;
            if (targetSlot.Inventory == null) return true;
            if (sourceSlot.Inventory != targetSlot.Inventory)
            {
                if (HasAnyRuckSackInBackpackInventory(targetSlot.Inventory, excludeA: null, excludeB: null))
                {
                    __result = null;
                    return false;
                }
            }

            return true;
        }

        private static bool InventoryBase_ActivateSlot_Prefix(
            InventoryBase __instance,
            int slotId,
            ItemSlot sourceSlot,
            ref ItemStackMoveOperation op,
            ref object __result)
        {
            
            if (sourceSlot?.Itemstack == null) return true;
            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(sourceSlot.Itemstack)) return true;

            ItemSlot? targetSlot;
            try
            {
                targetSlot = __instance[slotId];
            }
            catch
            {
                return true;
            }

            if (targetSlot is not ItemSlotBackpack) return true;
            if (targetSlot.Inventory == null) return true;
            if (sourceSlot.Inventory == targetSlot.Inventory) return true;
            if (HasAnyRuckSackInBackpackInventory(targetSlot.Inventory, excludeA: null, excludeB: null))
            {
                __result = null;
                return false;
            }

            return true;
        }

        private static bool InventoryBase_TryFlipItems_Prefix(
            InventoryBase __instance,
            int targetSlotId,
            ItemSlot sourceSlot,
            ref object __result)
        {
            ItemSlot? targetSlot;
            try
            {
                targetSlot = __instance[targetSlotId];
            }
            catch
            {
                return true;
            }

            if (targetSlot == null || sourceSlot == null) return true;
            if (sourceSlot.Itemstack != null && ArlWearableAttachmentBypassPatch.IsTargetRucksack(sourceSlot.Itemstack) && targetSlot is ItemSlotBackpack)
            {
                if (targetSlot.Inventory != null && sourceSlot.Inventory != targetSlot.Inventory)
                {
                    if (HasAnyRuckSackInBackpackInventory(targetSlot.Inventory, excludeA: null, excludeB: null))
                    {
                        __result = null;
                        return false;
                    }
                }
            }
            if (targetSlot.Itemstack != null && ArlWearableAttachmentBypassPatch.IsTargetRucksack(targetSlot.Itemstack) && sourceSlot is ItemSlotBackpack)
            {
                if (sourceSlot.Inventory != null && targetSlot.Inventory != sourceSlot.Inventory)
                {
                    if (HasAnyRuckSackInBackpackInventory(sourceSlot.Inventory, excludeA: null, excludeB: null))
                    {
                        __result = null;
                        return false;
                    }
                }
            }

            return true;
        }

        private static void GetBestSuitedSlot_New_Prefix(
            PlayerInventoryManager __instance,
            ItemSlot sourceSlot,
            bool onlyPlayerInventory,
            ItemStackMoveOperation op,
            ref List<ItemSlot> skipSlots)
        {
            AddBackpackSlotsToSkipIfSecondRucksackWouldBeEquipped(__instance, sourceSlot, ref skipSlots);
        }

        private static void GetBestSuitedSlot_Old_Prefix(
            PlayerInventoryManager __instance,
            ItemSlot sourceSlot,
            ItemStackMoveOperation op,
            ref List<ItemSlot> skipSlots)
        {
            AddBackpackSlotsToSkipIfSecondRucksackWouldBeEquipped(__instance, sourceSlot, ref skipSlots);
        }

        private static void AddBackpackSlotsToSkipIfSecondRucksackWouldBeEquipped(
            PlayerInventoryManager invMan,
            ItemSlot sourceSlot,
            ref List<ItemSlot> skipSlots)
        {
            if (sourceSlot?.Itemstack == null) return;
            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(sourceSlot.Itemstack)) return;

            IInventory? backpackInv = null;
            try
            {
                backpackInv = invMan.GetOwnInventory("backpack");
            }
            catch
            {
                
                return;
            }

            if (backpackInv == null) return;
            if (!HasAnyRuckSackInBackpackInventory(backpackInv, excludeA: null, excludeB: null)) return;

            skipSlots ??= new List<ItemSlot>();
            foreach (ItemSlot slot in backpackInv)
            {
                if (slot is ItemSlotBackpack)
                {
                    skipSlots.Add(slot);
                }
            }
        }

        private static bool HasOtherRuckSackInBackpackInventory(IInventory backpackInv, ItemSlot excludeA, ItemSlot excludeB)
        {
            foreach (ItemSlot slot in backpackInv)
            {
                if (slot == excludeA || slot == excludeB) continue;
                if (slot.Itemstack != null && ArlWearableAttachmentBypassPatch.IsTargetRucksack(slot.Itemstack)) return true;
            }

            return false;
        }

        private static bool HasAnyRuckSackInBackpackInventory(IInventory backpackInv, ItemSlot? excludeA, ItemSlot? excludeB)
        {
            foreach (ItemSlot slot in backpackInv)
            {
                if (excludeA != null && slot == excludeA) continue;
                if (excludeB != null && slot == excludeB) continue;
                if (slot.Itemstack != null && ArlWearableAttachmentBypassPatch.IsTargetRucksack(slot.Itemstack)) return true;
            }

            return false;
        }
    }
}
