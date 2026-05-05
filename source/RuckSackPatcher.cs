
using System;
using System.Reflection;
using AttributeRenderingLibrary;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuckSack
{
    internal static class RuckSackArlPatcher
    {
        private const string HarmonyId = "rucksack.arlpatches";
        private static bool patched;
        private static Harmony? harmony;

        internal static void ApplyClient(ICoreClientAPI api)
        {
            if (patched) return;
            patched = true;

            harmony ??= new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static bool IsRuckSackStack(ItemStack? stack)
        {
            string? path = stack?.Collectible?.Code?.Path;
            return path != null && path.StartsWith("rucksack", StringComparison.OrdinalIgnoreCase);
        }
        [HarmonyPatch(typeof(CollectibleBehaviorShapeTexturesFromAttributes))]
        internal static class Patch_BehaviorShapeTexturesFromAttributes
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(CollectibleBehaviorShapeTexturesFromAttributes.GenMesh))]
            private static bool GenMesh_Prefix(
                CollectibleBehaviorShapeTexturesFromAttributes __instance,
                ItemSlot slot,
                ITextureAtlasAPI targetAtlas,
                BlockPos atBlockPos,
                ref MeshData __result)
            {
                ItemStack itemstack = slot.Itemstack!;

                if (!IsRuckSackStack(itemstack)) return true;
                __result = __instance.GetOrCreateMesh(slot, targetAtlas);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(CollectibleBehaviorShapeTexturesFromAttributes.GetMeshCacheKey))]
            private static bool GetMeshCacheKey_Prefix(
                CollectibleBehaviorShapeTexturesFromAttributes __instance,
                ItemSlot slot,
                ref string __result)
            {
                ItemStack itemstack = slot.Itemstack!;

                if (!IsRuckSackStack(itemstack)) return true;
                __result = $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
                return false;
            }
        }
        [HarmonyPatch(typeof(ItemShapeTexturesFromAttributes))]
        internal static class Patch_ItemShapeTexturesFromAttributes
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ItemShapeTexturesFromAttributes.GenMesh))]
            private static bool GenMesh_Prefix(
                ItemShapeTexturesFromAttributes __instance,
                ItemSlot slot,
                ITextureAtlasAPI targetAtlas,
                BlockPos atBlockPos,
                ref MeshData __result)
            {
                ItemStack itemstack = slot.Itemstack!;

                if (!IsRuckSackStack(itemstack)) return true;

                __result = __instance.GetOrCreateMesh(slot, targetAtlas);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(ItemShapeTexturesFromAttributes.GetMeshCacheKey))]
            private static bool GetMeshCacheKey_Prefix(
                ItemShapeTexturesFromAttributes __instance,
                ItemSlot slot,
                ref string __result)
            {
                ItemStack itemstack = slot.Itemstack!;

                if (!IsRuckSackStack(itemstack)) return true;

                __result = $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
                return false;
            }
        }
    }
}