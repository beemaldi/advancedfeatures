
using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RuckSack
{
    [HarmonyPatch(typeof(BlockGroundStorage))]
    internal static class RuckSackGroundStorageInteractionHelpPatch
    {
        private const string LangBedrollAttach = "aldiclasses:rucksackhelp-bedroll-attach";
        private const string LangBedrollDetach = "aldiclasses:rucksackhelp-bedroll-detach";
        private const string LangQuartzAttach = "aldiclasses:rucksackhelp-quartz-attach";
        private const string LangQuartzDetach = "aldiclasses:rucksackhelp-quartz-detach";

        private const string CacheKeyAttachHelpStacks = "aldiclasses:rucksack-attachhelp-stacks-v1";
        private static readonly string[] BedrollColors =
        {
            "black", "brown", "plain", "gray", "green", "blue", "pink", "purple", "red", "yellow", "white", "orange"
        };

        private static readonly string[] QuartzColors =
        {
            "quartz", "smokyquartz", "rosyquartz", "olivine", "cinnabar", "amethyst", "sulfur", "lapislazuli", "sylvite"
        };

        private sealed class CachedAttachHelpStacks
        {
            public ItemStack[] BedrollStacks { get; }
            public ItemStack[] QuartzStacks { get; }

            public CachedAttachHelpStacks(ItemStack[] bedrollStacks, ItemStack[] quartzStacks)
            {
                BedrollStacks = bedrollStacks ?? Array.Empty<ItemStack>();
                QuartzStacks = quartzStacks ?? Array.Empty<ItemStack>();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockGroundStorage.GetPlacedBlockInteractionHelp))]
        private static void GetPlacedBlockInteractionHelp_Postfix(
            IWorldAccessor world,
            BlockSelection selection,
            IPlayer forPlayer,
            ref WorldInteraction[] __result)
        {
            if (world == null) return;
            if (selection?.Position == null) return;
            if (!RuckSackGroundStorageUtil.TryGetStoredRucksack(world, selection.Position, out ItemStack? rucksackStack) || rucksackStack == null)
            {
                return;
            }
            bool bedrollAttached = IsAttachmentAttached(rucksackStack, "bedroll");
            bool quartzAttached = IsAttachmentAttached(rucksackStack, "quartz");
            CachedAttachHelpStacks? cachedStacks = null;
            try
            {
                ICoreAPI? api = forPlayer?.Entity?.Api;
                if (api != null)
                {
                    cachedStacks = ObjectCacheUtil.GetOrCreate(api, CacheKeyAttachHelpStacks, () => BuildAttachHelpStacks(api.World));
                }
            }
            catch
            {
                cachedStacks = null;
            }
            List<WorldInteraction> extra = new List<WorldInteraction>(4);
            extra.Add(new WorldInteraction
            {
                ActionLangCode = bedrollAttached ? LangBedrollDetach : LangBedrollAttach,
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
                Itemstacks = bedrollAttached ? null : (cachedStacks?.BedrollStacks ?? null)
            });
            extra.Add(new WorldInteraction
            {
                ActionLangCode = quartzAttached ? LangQuartzDetach : LangQuartzAttach,
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
                Itemstacks = quartzAttached ? null : (cachedStacks?.QuartzStacks ?? null)
            });

            __result ??= Array.Empty<WorldInteraction>();
            __result = extra.ToArray().Append(__result);
        }

        private static CachedAttachHelpStacks BuildAttachHelpStacks(IWorldAccessor world)
        {
            if (world == null)
            {
                return new CachedAttachHelpStacks(Array.Empty<ItemStack>(), Array.Empty<ItemStack>());
            }

            List<ItemStack> bedrollStacks = new List<ItemStack>(BedrollColors.Length);
            for (int i = 0; i < BedrollColors.Length; i++)
            {
                string color = BedrollColors[i];
                if (string.IsNullOrEmpty(color)) continue;
                AssetLocation code = new AssetLocation("aldiclasses", "bedroll-" + color + "-head-north");
                Block block = world.GetBlock(code);

                if (block != null && block.BlockId != 0)
                {
                    bedrollStacks.Add(new ItemStack(block));
                }
            }
            List<ItemStack> quartzStacks = new List<ItemStack>(QuartzColors.Length);
            for (int i = 0; i < QuartzColors.Length; i++)
            {
                string color = QuartzColors[i];
                if (string.IsNullOrEmpty(color)) continue;

                AssetLocation code = new AssetLocation("aldiclasses", "embraced-quartz-" + color);
                Block block = world.GetBlock(code);

                if (block != null && block.BlockId != 0)
                {
                    quartzStacks.Add(new ItemStack(block));
                }
            }

            return new CachedAttachHelpStacks(
                bedrollStacks.Count > 0 ? bedrollStacks.ToArray() : Array.Empty<ItemStack>(),
                quartzStacks.Count > 0 ? quartzStacks.ToArray() : Array.Empty<ItemStack>()
            );
        }

        private static bool IsAttachmentAttached(ItemStack rucksackStack, string attachKey)
        {
            if (rucksackStack == null) return false;

            try
            {
                ITreeAttribute? typesTree = rucksackStack.Attributes?.GetTreeAttribute("types");
                if (typesTree == null) return false;

                string state = typesTree.GetString(attachKey, "none") ?? "none";
                return state.Equals("attached", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
