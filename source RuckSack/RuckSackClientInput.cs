using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuckSack
{
    internal static class RuckSackClientInput
    {
        private static ICoreClientAPI? capi;
        private static bool clientHooked;

        internal static void Init(ICoreClientAPI api)
        {
            if (api == null) return;
            if (clientHooked && ReferenceEquals(capi, api)) return;

            Dispose();

            capi = api;
            capi.Event.MouseDown += OnMouseDown;
            capi.Event.LeftWorld += OnLeftWorld;
            clientHooked = true;
        }

        internal static void Dispose()
        {
            if (capi != null && clientHooked)
            {
                try
                {
                    capi.Event.MouseDown -= OnMouseDown;
                    capi.Event.LeftWorld -= OnLeftWorld;
                }
                catch
                {
                }
            }

            capi = null;
            clientHooked = false;
        }

        private static void OnLeftWorld()
        {
            Input.DisposeClient();
        }

        private static void OnMouseDown(MouseEvent e)
        {
            if (e == null) return;
            if (e.Handled) return;

            if (e.Button != EnumMouseButton.Right) return;

            if (capi?.World?.Player?.Entity?.Controls == null) return;
            if (!capi.World.Player.Entity.Controls.Sneak) return;

            BlockSelection blockSel = capi.World.Player.CurrentBlockSelection;
            if (blockSel == null || blockSel.Position == null || blockSel.Face == null) return;
            Block block = capi.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block is not BlockGroundStorage) return;
            if (!RuckSackGroundStorageUtil.TryGetStoredRucksack(capi.World, blockSel.Position, out ItemStack? rucksackStack)) return;

            ItemSlot? activeSlot = capi.World.Player.InventoryManager?.ActiveHotbarSlot;
            if (activeSlot == null) return;
            if (activeSlot.Empty || activeSlot.Itemstack?.Collectible?.Code == null)
            {
                int detachKind = 0;

                bool isTopFace = blockSel.Face == BlockFacing.UP;
                bool isSideFace =
                    blockSel.Face == BlockFacing.NORTH ||
                    blockSel.Face == BlockFacing.SOUTH ||
                    blockSel.Face == BlockFacing.EAST ||
                    blockSel.Face == BlockFacing.WEST;

                if (isTopFace) detachKind = (int)RuckSackAttachKind.Bedroll;
                else if (isSideFace) detachKind = (int)RuckSackAttachKind.Quartz;
                else return;
                if (!IsAttachmentAttached(rucksackStack, detachKind)) return;

                e.Handled = true;

                RuckSackNetworking.SendDetachRequest(
                    blockSel.Position.X,
                    blockSel.Position.Y,
                    blockSel.Position.Z,
                    detachKind
                );

                return;
            }
            string usedItemPath = activeSlot.Itemstack.Collectible.Code.Path;
            bool isLunchbox = usedItemPath.Equals("lunchbox", StringComparison.OrdinalIgnoreCase);
            if (!isLunchbox && !RuckSackVariantResolver.IsBedrollOrEmbracedQuartz(usedItemPath)) return;

            int kind;
            if (isLunchbox)
            {
                kind = (int)RuckSackAttachKind.Lunchbox;
            }
            else
            {
                kind = usedItemPath.StartsWith("bedroll", StringComparison.OrdinalIgnoreCase)
                    ? (int)RuckSackAttachKind.Bedroll
                    : (int)RuckSackAttachKind.Quartz;
            }

            bool isTop = blockSel.Face == BlockFacing.UP;
            bool isSide =
                blockSel.Face == BlockFacing.NORTH ||
                blockSel.Face == BlockFacing.SOUTH ||
                blockSel.Face == BlockFacing.EAST ||
                blockSel.Face == BlockFacing.WEST;

            if (kind == (int)RuckSackAttachKind.Bedroll && !isTop) return;
            if (kind == (int)RuckSackAttachKind.Quartz && !isSide) return;
            if (IsAttachmentAttached(rucksackStack, kind))
            {
                e.Handled = true;
                return;
            }
            e.Handled = true;

            string? variantToken = null;
            if (kind == (int)RuckSackAttachKind.Bedroll || kind == (int)RuckSackAttachKind.Quartz)
            {
                variantToken = kind == (int)RuckSackAttachKind.Bedroll
                    ? RuckSackVariantResolver.TryExtractBedrollTextureBase(activeSlot.Itemstack)
                    : RuckSackVariantResolver.TryExtractQuartzTextureBase(activeSlot.Itemstack);
                if (string.IsNullOrEmpty(variantToken))
                {
                    variantToken = kind == (int)RuckSackAttachKind.Bedroll
                        ? RuckSackTextureTokens.BedrollTextureDefaultToken
                        : RuckSackTextureTokens.QuartzTextureDefaultToken;
                }
            }

            RuckSackNetworking.SendAttachRequest(
                blockSel.Position.X,
                blockSel.Position.Y,
                blockSel.Position.Z,
                kind,
                variantToken
            );
        }

        private static bool IsAttachmentAttached(ItemStack? rucksackStack, int kind)
        {
            if (rucksackStack?.Attributes == null) return false;

            ITreeAttribute? typesTree = rucksackStack.Attributes.GetTreeAttribute("types");
            if (typesTree == null) return false;

            if (kind == (int)RuckSackAttachKind.Bedroll)
            {
                return typesTree.GetString("bedroll", "none").Equals("attached", StringComparison.OrdinalIgnoreCase);
            }

            if (kind == (int)RuckSackAttachKind.Quartz)
            {
                return typesTree.GetString("quartz", "none").Equals("attached", StringComparison.OrdinalIgnoreCase);
            }

            if (kind == (int)RuckSackAttachKind.Lunchbox)
            {
                return typesTree.GetString("lunchbox", "none").Equals("attached", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
