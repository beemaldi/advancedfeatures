using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RuckSack
{
    internal static class IgnitionBangleCaster
    {
        internal const int IgniteDelayMs = 200;
        internal const int AnimationDurationMs = 700;
        private const float IgniteBoostSeconds = 10f;
        private const double ReachPadding = 1.5;
        private const int FlashStepsPerBlock = 6;
        private const double HandForwardOffset = 0.5;
        private const double HandSideOffset = 0.4;
        private const double HandDropOffset = 0.65;
        private const double TargetHeight = 0.25;
        private const float CastSoundPitch = 1.4f;
        private const float CastSoundVolume = 0.6f;

        private static readonly AssetLocation IgniteSound = new AssetLocation("sounds/torch-ignite");
        private static readonly HashSet<string> pendingCasts = new HashSet<string>();

        private static readonly SimpleParticleProperties FlashParticles = new SimpleParticleProperties(
            2, 3,
            ColorUtil.ToRgba(255, 90, 255, 110),
            new Vec3d(), new Vec3d(),
            new Vec3f(-0.3f, -0.3f, -0.3f),
            new Vec3f(0.3f, 0.3f, 0.3f),
            0.25f,
            0f,
            0.15f, 0.3f,
            EnumParticleModel.Quad
        )
        {
            VertexFlags = 255
        };

        internal static void Reset()
        {
            pendingCasts.Clear();
        }

        internal static bool TryGetWornBangle(
            IPlayer player,
            [NotNullWhen(true)] out ItemSlot? bangleSlot,
            [NotNullWhen(true)] out CollectibleBehaviorIgnitionBangle? bangle)
        {
            bangleSlot = null;
            bangle = null;

            IInventory? characterInv = player.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (characterInv == null) return false;

            foreach (ItemSlot slot in characterInv)
            {
                CollectibleBehaviorIgnitionBangle? found = slot.Itemstack?.Collectible?.GetBehavior<CollectibleBehaviorIgnitionBangle>();
                if (found == null) continue;

                bangleSlot = slot;
                bangle = found;
                return true;
            }

            return false;
        }

        internal static IIgnitable? GetIgnitableTarget(IWorldAccessor world, EntityAgent byEntity, BlockPos pos, CollectibleBehaviorIgnitionBangle bangle)
        {
            Block block = world.BlockAccessor.GetBlock(pos);
            if (bangle.IsExcluded(block)) return null;

            IIgnitable? ignitable = block.GetInterface<IIgnitable>(world, pos);
            if (ignitable == null) return null;

            EnumIgniteState state = ignitable.OnTryIgniteBlock(byEntity, pos, 0);
            if (state == EnumIgniteState.NotIgnitable || state == EnumIgniteState.NotIgnitablePreventDefault) return null;

            return ignitable;
        }

        internal static void TryCast(ICoreServerAPI sapi, IServerPlayer player, BlockPos pos)
        {
            if (pendingCasts.Contains(player.PlayerUID)) return;
            if (!TryGetValidCast(sapi.World, player, pos, out _, out CollectibleBehaviorIgnitionBangle? bangle, out _)) return;

            string animation = bangle.Animation;

            pendingCasts.Add(player.PlayerUID);
            player.Entity.AnimManager?.StartAnimation(animation);
            SpawnFlash(sapi.World, player.Entity, pos);
            if (bangle.CastSound != null)
            {
                sapi.World.PlaySoundAt(bangle.CastSound, player.Entity, null, CastSoundPitch, 16, CastSoundVolume);
            }

            sapi.Event.RegisterCallback(_ => FinishCast(sapi, player, pos), IgniteDelayMs);
            sapi.Event.RegisterCallback(_ => player.Entity?.AnimManager?.StopAnimation(animation), AnimationDurationMs);
        }

        private static void FinishCast(ICoreServerAPI sapi, IServerPlayer player, BlockPos pos)
        {
            pendingCasts.Remove(player.PlayerUID);

            if (!TryGetValidCast(sapi.World, player, pos, out ItemSlot? bangleSlot, out _, out IIgnitable? ignitable)) return;
            if (ignitable.OnTryIgniteBlock(player.Entity, pos, IgniteBoostSeconds) != EnumIgniteState.IgniteNow) return;

            EnumHandling handling = EnumHandling.PassThrough;
            ignitable.OnTryIgniteBlockOver(player.Entity, pos, IgniteBoostSeconds, ref handling);

            sapi.World.PlaySoundAt(IgniteSound, pos, 0.5);

            ItemStack? bangleStack = bangleSlot.Itemstack;
            if (bangleStack == null) return;

            CollectibleBehaviorIgnitionBangle.SetCharges(bangleStack, CollectibleBehaviorIgnitionBangle.GetCharges(bangleStack) - 1);
            bangleSlot.MarkDirty();
        }

        private static bool TryGetValidCast(
            IWorldAccessor world,
            IServerPlayer player,
            BlockPos pos,
            [NotNullWhen(true)] out ItemSlot? bangleSlot,
            [NotNullWhen(true)] out CollectibleBehaviorIgnitionBangle? bangle,
            [NotNullWhen(true)] out IIgnitable? ignitable)
        {
            ignitable = null;

            if (!TryGetWornBangle(player, out bangleSlot, out bangle)) return false;

            EntityPlayer? entity = player.Entity;
            if (entity == null || !entity.Alive) return false;
            if (player.InventoryManager?.ActiveHotbarSlot?.Empty != true) return false;
            if (CollectibleBehaviorIgnitionBangle.GetCharges(bangleSlot.Itemstack) <= 0) return false;
            if (!IsInReach(player, entity, pos)) return false;
            if (!world.Claims.TryAccess(player, pos, EnumBlockAccessFlags.Use)) return false;

            ignitable = GetIgnitableTarget(world, entity, pos, bangle);
            return ignitable != null;
        }

        private static bool IsInReach(IServerPlayer player, EntityPlayer entity, BlockPos pos)
        {
            Vec3d eye = entity.Pos.XYZ.Add(entity.LocalEyePos);
            double maxDistance = player.WorldData.PickingRange + ReachPadding;

            return eye.DistanceTo(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5) <= maxDistance;
        }

        private static void SpawnFlash(IWorldAccessor world, EntityPlayer entity, BlockPos pos)
        {
            Vec3d eye = entity.Pos.XYZ.Add(entity.LocalEyePos);
            Vec3d target = new Vec3d(pos.X + 0.5, pos.Y + TargetHeight, pos.Z + 0.5);

            Vec3d forward = new Vec3d(target.X - eye.X, 0, target.Z - eye.Z);
            double horizontalDistance = forward.Length();
            if (horizontalDistance > 0.01) forward.Mul(1 / horizontalDistance);

            Vec3d left = new Vec3d(forward.Z, 0, -forward.X);

            Vec3d start = eye.AddCopy(
                forward.X * HandForwardOffset + left.X * HandSideOffset,
                -HandDropOffset,
                forward.Z * HandForwardOffset + left.Z * HandSideOffset);
            Vec3d delta = target.SubCopy(start);

            int steps = Math.Max(2, (int)(delta.Length() * FlashStepsPerBlock));
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                FlashParticles.MinPos.Set(start.X + delta.X * t, start.Y + delta.Y * t, start.Z + delta.Z * t);
                world.SpawnParticles(FlashParticles);
            }
        }
    }
}
