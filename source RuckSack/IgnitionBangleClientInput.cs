using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace RuckSack
{
    internal static class IgnitionBangleClientInput
    {
        private const long CastCooldownMs = IgnitionBangleCaster.AnimationDurationMs;

        private static ICoreClientAPI? capi;
        private static bool clientHooked;
        private static long nextCastAllowedMs;

        internal static void Init(ICoreClientAPI api)
        {
            if (api == null) return;
            if (clientHooked && ReferenceEquals(capi, api)) return;

            Dispose();

            capi = api;
            capi.Event.MouseDown += OnMouseDown;
            clientHooked = true;
        }

        internal static void Dispose()
        {
            if (capi != null && clientHooked)
            {
                try
                {
                    capi.Event.MouseDown -= OnMouseDown;
                }
                catch
                {
                }
            }

            capi = null;
            clientHooked = false;
            nextCastAllowedMs = 0;
        }

        private static void OnMouseDown(MouseEvent e)
        {
            if (e == null) return;
            if (e.Handled) return;
            if (e.Button != EnumMouseButton.Right) return;

            ICoreClientAPI? api = capi;
            IClientPlayer? player = api?.World?.Player;
            if (api == null || player?.Entity?.Controls == null) return;
            if (!player.Entity.Controls.Sneak) return;
            if (player.InventoryManager?.ActiveHotbarSlot?.Empty != true) return;

            BlockSelection blockSel = player.CurrentBlockSelection;
            if (blockSel?.Position == null) return;
            if (!IgnitionBangleCaster.TryGetWornBangle(player, out ItemSlot? bangleSlot, out CollectibleBehaviorIgnitionBangle? bangle)) return;
            if (IgnitionBangleCaster.GetIgnitableTarget(api.World, player.Entity, blockSel.Position) == null) return;

            e.Handled = true;

            if (CollectibleBehaviorIgnitionBangle.GetCharges(bangleSlot.Itemstack) <= 0)
            {
                api.TriggerIngameError(bangle, "ignitionbangleempty", Lang.Get("aldiclasses:ingameerror-ignitionbangle-empty"));
                return;
            }

            long now = api.World.ElapsedMilliseconds;
            if (now < nextCastAllowedMs) return;
            nextCastAllowedMs = now + CastCooldownMs;

            string animation = bangle.Animation;
            player.Entity.AnimManager?.StartAnimation(animation);
            api.Event.RegisterCallback(_ => api.World?.Player?.Entity?.AnimManager?.StopAnimation(animation), IgnitionBangleCaster.AnimationDurationMs);

            RuckSackNetworking.SendIgniteRequest(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
        }
    }
}
