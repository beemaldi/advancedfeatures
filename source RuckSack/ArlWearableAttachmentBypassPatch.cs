using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AttributeRenderingLibrary;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RuckSack
{
    internal static class ArlWearableAttachmentBypassPatch
    {
        internal static bool IsTargetRucksack(ItemStack? stack)
        {
            return IsTargetRucksack(stack?.Collectible);
        }

        internal static bool IsTargetRucksack(CollectibleObject? collectible)
        {
            AssetLocation? code = collectible?.Code;
            if (code == null) return false;
            if (!string.Equals(code.Domain, "aldiclasses", StringComparison.OrdinalIgnoreCase)) return false;

            return code.Path != null &&
                   (string.Equals(code.Path, "rucksack", StringComparison.OrdinalIgnoreCase) ||
                    code.Path.StartsWith("rucksack-", StringComparison.OrdinalIgnoreCase));
        }

        internal static bool HasDynamicAttachmentShapes(CollectibleObject collectible)
        {
            JsonObject? attachable = collectible.Attributes?["STFA_attachableToEntity"];
            if (attachable == null || !attachable.Exists) return false;

            JsonObject? attachedShape = attachable["attachedShape"];
            JsonObject? attachedShapeBySlotCode = attachable["attachedShapeBySlotCode"];

            return attachedShape != null && attachedShape.Exists ||
                   attachedShapeBySlotCode != null && attachedShapeBySlotCode.Exists;
        }

        internal static string BuildNormalKey(ItemStack itemstack)
        {
            return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
        }
    }

    [HarmonyPatch(typeof(ItemShapeTexturesFromAttributes))]
    internal static class ArlItemShapeTexturesFromAttributesOnLoadedPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(ItemShapeTexturesFromAttributes.OnLoaded))]
        private static IEnumerable<CodeInstruction> OnLoadedTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo warn = AccessTools.Method(typeof(LoggerUtil), nameof(LoggerUtil.Warn), new[] { typeof(ICoreAPI), typeof(object), typeof(string) });
            MethodInfo replacement = AccessTools.Method(typeof(ArlItemShapeTexturesFromAttributesOnLoadedPatch), nameof(WarnUnsupportedWearable));

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(warn))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    continue;
                }

                yield return instruction;
            }
        }

        private static void WarnUnsupportedWearable(ICoreAPI api, object caller, string format)
        {
            if (caller is ItemShapeTexturesFromAttributes item &&
                ArlWearableAttachmentBypassPatch.IsTargetRucksack(item) &&
                ArlWearableAttachmentBypassPatch.HasDynamicAttachmentShapes(item))
            {
                return;
            }

            LoggerUtil.Warn(api, caller, format);
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
