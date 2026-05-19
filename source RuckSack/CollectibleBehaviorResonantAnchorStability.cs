using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace RuckSack
{
    public sealed class CollectibleBehaviorResonantAnchorStability : CollectibleBehavior
    {
        private const double StabilityRestoreAmount = 0.5;

        public CollectibleBehaviorResonantAnchorStability(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStop(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;

            if (byEntity.World.Side != EnumAppSide.Server) return;
            if (secondsUsed < 0.95f) return;
            if (slot.Empty) return;
            if (collObj.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) == null) return;

            EntityBehaviorTemporalStabilityAffected temporalStability = byEntity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
            if (temporalStability == null) return;

            temporalStability.OwnStability = Math.Min(1, temporalStability.OwnStability + StabilityRestoreAmount);
        }
    }
}
