using System;
using AttributeRenderingLibrary;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuckSack
{
    internal static class ArlWearableAttachmentBypassPatch
    {
        internal static bool IsTargetRucksack(ItemStack? stack)
        {
            AssetLocation? code = stack?.Collectible?.Code;
            if (code == null) return false;
            if (!string.Equals(code.Domain, "aldiclasses", StringComparison.OrdinalIgnoreCase)) return false;

            return code.Path != null &&
                   (string.Equals(code.Path, "rucksack", StringComparison.OrdinalIgnoreCase) ||
                    code.Path.StartsWith("rucksack-", StringComparison.OrdinalIgnoreCase));
        }

        internal static string BuildNormalKey(ItemStack itemstack)
        {
            return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
        }
    }

    [HarmonyPatch(typeof(ItemShapeTexturesFromAttributes))]
    internal static class ArlItemWearableAttachmentBypassPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemShapeTexturesFromAttributes.GenMesh))]
        private static bool GenMeshPrefix(
            ItemShapeTexturesFromAttributes __instance,
            ItemSlot slot,
            ITextureAtlasAPI targetAtlas,
            BlockPos atBlockPos,
            ref MeshData __result)
        {
            ItemStack itemstack = slot.Itemstack!;

            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(itemstack)) return true;

            if (itemstack.ItemAttributes != null && itemstack.ItemAttributes.IsTrue("wearableAttachment"))
            {
                __result = __instance.GetOrCreateMesh(slot, targetAtlas);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemShapeTexturesFromAttributes.GetMeshCacheKey))]
        private static bool GetMeshCacheKeyPrefix(
            ItemSlot slot,
            ref string __result)
        {
            ItemStack itemstack = slot.Itemstack!;

            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(itemstack)) return true;

            __result = ArlWearableAttachmentBypassPatch.BuildNormalKey(itemstack);
            return false;
        }
    }

    [HarmonyPatch(typeof(CollectibleBehaviorShapeTexturesFromAttributes))]
    internal static class ArlBehaviorWearableAttachmentBypassPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleBehaviorShapeTexturesFromAttributes.GenMesh))]
        private static bool GenMeshPrefix(
            CollectibleBehaviorShapeTexturesFromAttributes __instance,
            ItemSlot slot,
            ITextureAtlasAPI targetAtlas,
            BlockPos atBlockPos,
            ref MeshData __result)
        {
            ItemStack itemstack = slot.Itemstack!;

            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(itemstack)) return true;

            if (itemstack.ItemAttributes != null && itemstack.ItemAttributes.IsTrue("wearableAttachment"))
            {
                __result = __instance.GetOrCreateMesh(slot, targetAtlas);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleBehaviorShapeTexturesFromAttributes.GetMeshCacheKey))]
        private static bool GetMeshCacheKeyPrefix(
            ItemSlot slot,
            ref string __result)
        {
            ItemStack itemstack = slot.Itemstack!;

            if (!ArlWearableAttachmentBypassPatch.IsTargetRucksack(itemstack)) return true;

            __result = ArlWearableAttachmentBypassPatch.BuildNormalKey(itemstack);
            return false;
        }
    }
}