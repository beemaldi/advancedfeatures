using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuckSack
{
    public sealed class CollectibleBehaviorResonantAnchorStability : CollectibleBehavior
    {
        private const double StabilityRestoreAmount = 0.5;
        private const float UseDurationSeconds = 6f;

        private ICoreClientAPI? capi;
        private ILoadedSound? translocatingSound;
        private float translocVolume;
        private float translocPitch;
        private float lastSoundSecondsUsed;

        public CollectibleBehaviorResonantAnchorStability(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            StopTranslocatingSound();
            base.OnUnloaded(api);
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
            handling = EnumHandling.PassThrough;

            if (byEntity.World.Side != EnumAppSide.Client) return;
            if (slot.Empty) return;
            if (collObj.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) == null) return;

            StartTranslocatingSound();
        }

        public override bool OnHeldInteractStep(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            ref EnumHandling handling)
        {
            if (slot.Empty || collObj.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) == null)
            {
                handling = EnumHandling.PassThrough;
                return true;
            }

            KeepEatAnimationAlive(byEntity);

            if (byEntity.World.Side != EnumAppSide.Client)
            {
                handling = EnumHandling.PassThrough;
                return true;
            }

            handling = EnumHandling.PreventDefault;

            HandleSoundClient(secondsUsed);
            SpawnEatParticles(secondsUsed, slot, byEntity);

            return secondsUsed <= UseDurationSeconds;
        }

        public override void OnHeldInteractStop(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            ref EnumHandling handling)
        {
            if (slot.Empty || collObj.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) == null)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            bool finished = secondsUsed >= UseDurationSeconds - 0.05f;
            if (!finished)
            {
                handling = EnumHandling.PreventDefault;
                byEntity.StopAnimation("eat");

                if (byEntity.World.Side == EnumAppSide.Client)
                {
                    StopTranslocatingSound();
                }

                return;
            }

            handling = EnumHandling.PassThrough;

            if (byEntity.World.Side == EnumAppSide.Client)
            {
                StopTranslocatingSound();
                return;
            }

            byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, false, 16, 1);

            EntityBehaviorTemporalStabilityAffected temporalStability = byEntity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
            if (temporalStability == null) return;

            temporalStability.OwnStability = Math.Min(1, temporalStability.OwnStability + StabilityRestoreAmount);
        }

        public override bool OnHeldInteractCancel(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            EnumItemUseCancelReason cancelReason,
            ref EnumHandling handled)
        {
            handled = EnumHandling.PassThrough;
            byEntity.StopAnimation("eat");

            if (byEntity.World.Side == EnumAppSide.Client)
            {
                StopTranslocatingSound();
            }

            return true;
        }

        private static void KeepEatAnimationAlive(EntityAgent byEntity)
        {
            if (byEntity.AnimManager != null && !byEntity.AnimManager.IsAnimationActive("eat"))
            {
                byEntity.AnimManager.StartAnimation("eat");
            }
        }

        private void StartTranslocatingSound()
        {
            StopTranslocatingSound();

            translocVolume = 0;
            translocPitch = 0;
            lastSoundSecondsUsed = 0;

            translocatingSound = capi!.World.LoadSound(new SoundParams
            {
                Location = new AssetLocation("sounds/effect/translocate-active.ogg"),
                ShouldLoop = true,
                Position = new Vec3f(),
                RelativePosition = true,
                DisposeOnFinish = false,
                Volume = 0.5f
            });
        }

        private void HandleSoundClient(float secondsUsed)
        {
            if (translocatingSound == null) return;

            float dt = Math.Max(0, secondsUsed - lastSoundSecondsUsed);
            lastSoundSecondsUsed = secondsUsed;

            translocVolume = Math.Min(0.5f, translocVolume + dt / 3);
            translocPitch = Math.Min(translocPitch + dt / 3, 2.5f);

            translocatingSound.SetVolume(translocVolume);
            translocatingSound.SetPitch(translocPitch);

            capi!.World.AddCameraShake(0.043f);

            if (!translocatingSound.IsPlaying && translocVolume > 0)
            {
                translocatingSound.Start();
            }
        }

        private void StopTranslocatingSound()
        {
            if (translocatingSound == null) return;

            translocatingSound.Stop();
            translocatingSound.Dispose();
            translocatingSound = null;
        }

        private void SpawnEatParticles(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (secondsUsed <= 0.5f || (int)(30 * secondsUsed) % 7 != 1) return;

            Vec3d pos = byEntity.Pos.AheadCopy(0.4f).XYZ;
            pos.X += byEntity.LocalEyePos.X;
            pos.Y += byEntity.LocalEyePos.Y - 0.4f;
            pos.Z += byEntity.LocalEyePos.Z;

            byEntity.World.SpawnCubeParticles(pos, slot.Itemstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
        }
    }
}
