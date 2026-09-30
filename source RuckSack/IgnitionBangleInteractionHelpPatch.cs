using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace RuckSack
{
    internal static class IgnitionBangleInteractionHelpPatch
    {
        private const string LangIgnite = "aldiclasses:blockhelp-ignitionbangle-ignite";
        private const string CacheKeyBangleHelp = "aldiclasses:ignitionbangle-help";
        private const float HelpRange = 8f;

        private static ICoreClientAPI? capi;
        private static bool patched;
        private static readonly object patchLock = new();

        private sealed class BangleHelp
        {
            public BangleHelp(CollectibleBehaviorIgnitionBangle? behavior, ItemStack[] stacks)
            {
                Behavior = behavior;
                Stacks = stacks;
            }

            public CollectibleBehaviorIgnitionBangle? Behavior { get; }
            public ItemStack[] Stacks { get; }
        }

        internal static void Apply(Harmony harmony, ICoreClientAPI api)
        {
            if (harmony == null || api == null) return;

            lock (patchLock)
            {
                capi = api;
                if (patched) return;

                MethodInfo? getWorldInteractions = AccessTools.Method(typeof(HudElementInteractionHelp), "getWorldInteractions");
                if (getWorldInteractions == null) return;

                try
                {
                    harmony.Patch(
                        getWorldInteractions,
                        postfix: new HarmonyMethod(typeof(IgnitionBangleInteractionHelpPatch), nameof(GetWorldInteractions_Postfix))
                    );
                    patched = true;
                }
                catch
                {
                    patched = false;
                }
            }
        }

        internal static void Reset()
        {
            lock (patchLock)
            {
                patched = false;
                capi = null;
            }
        }

        private static void GetWorldInteractions_Postfix(Block ___currentBlock, ref WorldInteraction[] __result)
        {
            ICoreClientAPI? api = capi;
            IClientWorldAccessor? world = api?.World;
            if (api == null || world == null || ___currentBlock == null) return;

            EntityPlayer? entity = world.Player?.Entity;
            BlockPos? pos = world.Player?.CurrentBlockSelection?.Position;
            if (entity == null || pos == null) return;
            if (entity.Pos.XYZ.AsBlockPos.DistanceTo(pos) > HelpRange) return;
            if (___currentBlock.GetInterface<IIgnitable>(world, pos) == null) return;

            BangleHelp help = ObjectCacheUtil.GetOrCreate(api, CacheKeyBangleHelp, () => BuildBangleHelp(world));
            CollectibleBehaviorIgnitionBangle? bangle = help.Behavior;
            if (bangle == null || help.Stacks.Length == 0) return;
            if (bangle.IsExcluded(___currentBlock)) return;

            WorldInteraction interaction = new WorldInteraction
            {
                ActionLangCode = LangIgnite,
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
                Itemstacks = help.Stacks,
                GetMatchingStacks = (wi, blockSel, entitySel) => IsIgnitable(api, blockSel, bangle) ? wi.Itemstacks : null
            };

            __result ??= Array.Empty<WorldInteraction>();
            __result = __result.Append(interaction);
        }

        private static bool IsIgnitable(ICoreClientAPI api, BlockSelection? blockSel, CollectibleBehaviorIgnitionBangle bangle)
        {
            IClientWorldAccessor? world = api.World;
            EntityPlayer? entity = world?.Player?.Entity;
            if (world == null || entity == null || blockSel?.Position == null) return false;

            return IgnitionBangleCaster.GetIgnitableTarget(world, entity, blockSel.Position, bangle) != null;
        }

        private static BangleHelp BuildBangleHelp(IWorldAccessor world)
        {
            List<ItemStack> stacks = new List<ItemStack>();
            CollectibleBehaviorIgnitionBangle? firstBehavior = null;

            foreach (Item item in world.Items)
            {
                if (item?.Code == null) continue;

                CollectibleBehaviorIgnitionBangle? behavior = item.GetBehavior<CollectibleBehaviorIgnitionBangle>();
                if (behavior == null) continue;

                firstBehavior ??= behavior;
                stacks.Add(new ItemStack(item));
            }

            return new BangleHelp(firstBehavior, stacks.ToArray());
        }
    }
}
