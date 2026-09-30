using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RuckSack
{
    [HarmonyPatch(typeof(Block))]
    internal static class IgnitionBangleInteractionHelpPatch
    {
        private const string LangIgnite = "aldiclasses:blockhelp-ignitionbangle-ignite";
        private const string CacheKeyBangleStacks = "aldiclasses:ignitionbangle-helpstacks";

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Block.GetPlacedBlockInteractionHelp))]
        private static void GetPlacedBlockInteractionHelp_Postfix(
            Block __instance,
            IWorldAccessor world,
            BlockSelection selection,
            IPlayer forPlayer,
            ref WorldInteraction[] __result)
        {
            if (world?.Api is not ICoreClientAPI capi) return;
            if (selection?.Position == null) return;
            if (__instance.GetInterface<IIgnitable>(world, selection.Position) == null) return;

            ItemStack[] bangleStacks = ObjectCacheUtil.GetOrCreate(capi, CacheKeyBangleStacks, () => BuildBangleStacks(capi.World));
            if (bangleStacks.Length == 0) return;

            WorldInteraction interaction = new WorldInteraction
            {
                ActionLangCode = LangIgnite,
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
                Itemstacks = bangleStacks,
                GetMatchingStacks = (wi, blockSel, entitySel) => IsIgnitable(capi, blockSel) ? wi.Itemstacks : null
            };

            __result ??= Array.Empty<WorldInteraction>();
            __result = __result.Append(interaction);
        }

        private static bool IsIgnitable(ICoreClientAPI capi, BlockSelection? blockSel)
        {
            IClientWorldAccessor? world = capi.World;
            EntityPlayer? entity = world?.Player?.Entity;
            if (world == null || entity == null || blockSel?.Position == null) return false;

            return IgnitionBangleCaster.GetIgnitableTarget(world, entity, blockSel.Position) != null;
        }

        private static ItemStack[] BuildBangleStacks(IWorldAccessor world)
        {
            List<ItemStack> stacks = new List<ItemStack>();

            foreach (Item item in world.Items)
            {
                if (item?.Code == null) continue;
                if (!item.HasBehavior<CollectibleBehaviorIgnitionBangle>()) continue;

                stacks.Add(new ItemStack(item));
            }

            return stacks.ToArray();
        }
    }
}
