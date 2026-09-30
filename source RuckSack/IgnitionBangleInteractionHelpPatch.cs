using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace RuckSack
{
    [HarmonyPatch(typeof(Block))]
    internal static class IgnitionBangleInteractionHelpPatch
    {
        private const string LangIgnite = "aldiclasses:blockhelp-ignitionbangle-ignite";
        private const string CacheKeyBangleStacks = "aldiclasses:ignitionbangle-helpstacks";

        private sealed class BangleEntry
        {
            public BangleEntry(CollectibleBehaviorIgnitionBangle behavior, ItemStack stack)
            {
                Behavior = behavior;
                Stack = stack;
            }

            public CollectibleBehaviorIgnitionBangle Behavior { get; }
            public ItemStack Stack { get; }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Block.GetPlacedBlockInteractionHelp))]
        private static void GetPlacedBlockInteractionHelp_Postfix(
            IWorldAccessor world,
            BlockSelection selection,
            IPlayer forPlayer,
            ref WorldInteraction[] __result)
        {
            if (world?.Api is not ICoreClientAPI capi) return;
            if (selection?.Position == null) return;

            BangleEntry[] entries = ObjectCacheUtil.GetOrCreate(capi, CacheKeyBangleStacks, () => BuildBangleEntries(capi.World));
            if (entries.Length == 0) return;

            Block block = world.BlockAccessor.GetBlock(selection.Position);

            List<ItemStack> stacks = new List<ItemStack>(entries.Length);
            CollectibleBehaviorIgnitionBangle? bangle = null;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].Behavior.CanIgnite(block)) continue;

                bangle ??= entries[i].Behavior;
                stacks.Add(entries[i].Stack);
            }

            if (bangle == null) return;

            WorldInteraction interaction = new WorldInteraction
            {
                ActionLangCode = LangIgnite,
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
                Itemstacks = stacks.ToArray(),
                GetMatchingStacks = (wi, blockSel, entitySel) => IsIgnitable(capi, blockSel, bangle) ? wi.Itemstacks : null
            };

            __result ??= Array.Empty<WorldInteraction>();
            __result = __result.Append(interaction);
        }

        private static bool IsIgnitable(ICoreClientAPI capi, BlockSelection? blockSel, CollectibleBehaviorIgnitionBangle bangle)
        {
            IClientWorldAccessor? world = capi.World;
            EntityPlayer? entity = world?.Player?.Entity;
            if (world == null || entity == null || blockSel?.Position == null) return false;

            return IgnitionBangleCaster.GetIgnitableTarget(world, entity, blockSel.Position, bangle) != null;
        }

        private static BangleEntry[] BuildBangleEntries(IWorldAccessor world)
        {
            List<BangleEntry> entries = new List<BangleEntry>();

            foreach (Item item in world.Items)
            {
                if (item?.Code == null) continue;

                CollectibleBehaviorIgnitionBangle? behavior = item.GetBehavior<CollectibleBehaviorIgnitionBangle>();
                if (behavior == null) continue;

                entries.Add(new BangleEntry(behavior, new ItemStack(item)));
            }

            return entries.ToArray();
        }
    }
}
