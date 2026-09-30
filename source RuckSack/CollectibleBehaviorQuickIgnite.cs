using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuckSack
{
    public sealed class CollectibleBehaviorQuickIgnite : CollectibleBehavior
    {
        private const float IgniteSeconds = 1f;
        private const float ServerToleranceSeconds = 0.2f;
        private const float IgniteBoostSeconds = 10f;

        private static readonly AssetLocation IgniteSound = new AssetLocation("sounds/torch-ignite");
        private static readonly AssetLocation FireBlockCode = new AssetLocation("fire");

        public CollectibleBehaviorQuickIgnite(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handHandling,
            ref EnumHandling handling)
        {
            IIgnitable? ignitable = GetUsableIgnitable(byEntity, blockSel);
            if (ignitable == null) return;

            EnumIgniteState state = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, 0);
            if (state == EnumIgniteState.NotIgnitable) return;

            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventDefault;

            if (state == EnumIgniteState.NotIgnitablePreventDefault) return;

            byEntity.World.PlaySoundAt(IgniteSound, byEntity, GetPlayer(byEntity), false, 16);
        }

        public override bool OnHeldInteractStep(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            ref EnumHandling handling)
        {
            IIgnitable? ignitable = GetUsableIgnitable(byEntity, blockSel);
            if (ignitable == null) return false;

            EnumIgniteState state = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, secondsUsed);
            if (state == EnumIgniteState.NotIgnitable || state == EnumIgniteState.NotIgnitablePreventDefault) return false;

            handling = EnumHandling.PreventDefault;

            SpawnIgniteParticles(secondsUsed, byEntity, blockSel);

            if (byEntity.World.Side == EnumAppSide.Server) return true;

            return secondsUsed < IgniteSeconds;
        }

        public override void OnHeldInteractStop(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            ref EnumHandling handling)
        {
            if (byEntity.World.Side != EnumAppSide.Server) return;
            if (secondsUsed < IgniteSeconds - ServerToleranceSeconds) return;

            IIgnitable? ignitable = GetUsableIgnitable(byEntity, blockSel);
            if (ignitable == null) return;

            float boostedSeconds = secondsUsed + IgniteBoostSeconds;
            if (ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, boostedSeconds) != EnumIgniteState.IgniteNow) return;

            handling = EnumHandling.PreventDefault;

            EnumHandling igniteHandling = EnumHandling.PassThrough;
            ignitable.OnTryIgniteBlockOver(byEntity, blockSel.Position, boostedSeconds, ref igniteHandling);

            if (slot.Empty) return;
            if (collObj.GetMaxDurability(slot.Itemstack) <= 0) return;

            collObj.DamageItem(byEntity.World, byEntity, slot);
        }

        private static IIgnitable? GetUsableIgnitable(EntityAgent byEntity, BlockSelection? blockSel)
        {
            BlockPos? pos = blockSel?.Position;
            if (pos == null) return null;

            IWorldAccessor world = byEntity.World;
            IIgnitable? ignitable = world.BlockAccessor.GetBlock(pos)?.GetInterface<IIgnitable>(world, pos);
            if (ignitable == null) return null;
            if (!world.Claims.TryAccess(GetPlayer(byEntity), pos, EnumBlockAccessFlags.Use)) return null;

            return ignitable;
        }

        private static IPlayer? GetPlayer(EntityAgent byEntity)
        {
            return (byEntity as EntityPlayer)?.Player;
        }

        private static void SpawnIgniteParticles(float secondsUsed, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (byEntity.World is not IClientWorldAccessor) return;
            if (secondsUsed <= 0.25f) return;
            if ((int)(30 * secondsUsed) % 2 != 1) return;

            AdvancedParticleProperties[]? fireParticles = byEntity.World.GetBlock(FireBlockCode)?.ParticleProperties;
            if (fireParticles == null || fireParticles.Length == 0) return;

            Random rand = byEntity.World.Rand;
            Vec3d pos = blockSel.Position.ToVec3d()
                .Add(blockSel.HitPosition)
                .Add(rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125);

            AdvancedParticleProperties props = fireParticles[fireParticles.Length - 1].Clone();
            props.basePos = pos;
            props.Quantity.avg = 0.5f;

            byEntity.World.SpawnParticles(props, GetPlayer(byEntity));
        }
    }
}
