using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace RuckSack
{
    public sealed class CollectibleBehaviorIgnitionBangle : CollectibleBehavior
    {
        private const string ChargesKey = "ignitionCharges";
        private const int DefaultMaxCharges = 250;
        private const string DefaultAnimation = "holdinglanternlefthand";
        private const string DefaultRefillItem = "game:gear-temporal";
        private const string DefaultCastSound = "game:sounds/player/strike";

        private ICoreAPI? api;
        private AssetLocation refillItem = new AssetLocation(DefaultRefillItem);
        private AssetLocation[] ignitableBlocks = Array.Empty<AssetLocation>();

        internal int MaxCharges { get; private set; } = DefaultMaxCharges;
        internal string Animation { get; private set; } = DefaultAnimation;
        internal AssetLocation? CastSound { get; private set; } = new AssetLocation(DefaultCastSound);

        public CollectibleBehaviorIgnitionBangle(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            MaxCharges = properties["maxCharges"].AsInt(DefaultMaxCharges);
            Animation = properties["animation"].AsString(DefaultAnimation);
            refillItem = new AssetLocation(properties["refillItem"].AsString(DefaultRefillItem));

            string? castSound = properties["castSound"].AsString(DefaultCastSound);
            CastSound = string.IsNullOrEmpty(castSound) ? null : new AssetLocation(castSound);

            string?[] codes = properties["ignitableBlocks"].AsArray(Array.Empty<string>());
            List<AssetLocation> locations = new List<AssetLocation>(codes.Length);
            for (int i = 0; i < codes.Length; i++)
            {
                string? code = codes[i];
                if (string.IsNullOrEmpty(code)) continue;

                locations.Add(new AssetLocation(code));
            }

            ignitableBlocks = locations.ToArray();
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
        }

        internal static int GetCharges(ItemStack? stack)
        {
            return stack?.Attributes.GetInt(ChargesKey) ?? 0;
        }

        internal static void SetCharges(ItemStack stack, int charges)
        {
            stack.Attributes.SetInt(ChargesKey, Math.Max(0, charges));
        }

        internal bool CanIgnite(Block block)
        {
            if (block?.Code == null) return false;

            for (int i = 0; i < ignitableBlocks.Length; i++)
            {
                if (WildcardUtil.Match(ignitableBlocks[i], block.Code)) return true;
            }

            return false;
        }

        public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority, ref EnumHandling handling)
        {
            if (priority != EnumMergePriority.DirectMerge) return 0;
            if (!IsRefillItem(sourceStack)) return 0;

            handling = EnumHandling.PreventDefault;
            return 1;
        }

        public override void TryMergeStacks(ItemStackMergeOperation op, ref EnumHandling handling)
        {
            if (op.CurrentPriority != EnumMergePriority.DirectMerge) return;
            if (!IsRefillItem(op.SourceSlot.Itemstack)) return;

            handling = EnumHandling.PreventDefault;

            ItemStack? bangleStack = op.SinkSlot.Itemstack;
            if (bangleStack == null) return;

            int charges = GetCharges(bangleStack);
            if (charges + MaxCharges / 2 < MaxCharges)
            {
                SetCharges(bangleStack, charges + MaxCharges);
                op.MovedQuantity = 1;
                op.SourceSlot.TakeOut(1);
                op.SinkSlot.MarkDirty();
                return;
            }

            (api as ICoreClientAPI)?.TriggerIngameError(this, "ignitionbanglefull", Lang.Get("aldiclasses:ingameerror-ignitionbangle-full"));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            int charges = GetCharges(inSlot.Itemstack);
            dsc.AppendLine(Lang.Get("aldiclasses:ignitionbangle-charges", charges, MaxCharges));
            if (charges <= 0)
            {
                dsc.AppendLine(Lang.Get("aldiclasses:ignitionbangle-refill"));
            }
        }

        private bool IsRefillItem(ItemStack? stack)
        {
            return stack?.Collectible?.Code != null && stack.Collectible.Code.Equals(refillItem);
        }
    }
}
