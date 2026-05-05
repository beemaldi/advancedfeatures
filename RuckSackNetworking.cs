
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RuckSack
{
    internal static class RuckSackNetworking
    {
        private const string ChannelName = "rucksack";

        private static ICoreServerAPI? sapi;

        private static IClientNetworkChannel? cchannel;
        private static IServerNetworkChannel? schannel;

        internal static void InitClient(Vintagestory.API.Client.ICoreClientAPI api)
        {
            RegisterClientNetwork(api);
        }

        internal static void InitServer(ICoreServerAPI api)
        {
            sapi = api;
            RegisterServerNetwork(api);
        }

        internal static void SendAttachRequest(int x, int y, int z, int kind, string? variantToken)
        {
            if (cchannel == null) return;

            cchannel.SendPacket(new RuckSackAttachRequestPacket
            {
                X = x,
                Y = y,
                Z = z,
                Kind = kind,
                Variant = variantToken
            });
        }

        internal static void SendDetachRequest(int x, int y, int z, int kind)
        {
            if (cchannel == null) return;

            cchannel.SendPacket(new RuckSackDetachRequestPacket
            {
                X = x,
                Y = y,
                Z = z,
                Kind = kind
            });
        }

        internal static void BroadcastQuartzLightState(long entityId, bool active, int h, int s, int v)
        {
            if (schannel == null) return;
            if (sapi?.World == null) return;

            var packet = new RuckSackQuartzLightStatePacket
            {
                EntityId = entityId,
                Active = active,
                H = h,
                S = s,
                V = v
            };
            try
            {
                IServerPlayer[] players = (IServerPlayer[])sapi.World.AllOnlinePlayers;
                if (players == null) return;

                for (int i = 0; i < players.Length; i++)
                {
                    IServerPlayer p = players[i];
                    if (p != null)
                    {
                        schannel.SendPacket(packet, p);
                    }
                }
            }
            catch
            {
                
            }
        }

        internal static void SendQuartzLightStateToPlayer(IServerPlayer toPlayer, long entityId, bool active, int h, int s, int v)
        {
            if (schannel == null) return;
            if (toPlayer == null) return;

            schannel.SendPacket(new RuckSackQuartzLightStatePacket
            {
                EntityId = entityId,
                Active = active,
                H = h,
                S = s,
                V = v
            }, toPlayer);
        }

        private static void RegisterClientNetwork(Vintagestory.API.Client.ICoreClientAPI api)
        {
            if (cchannel != null) return;

            cchannel = api.Network
                .RegisterChannel(ChannelName)
                .RegisterMessageType<RuckSackAttachRequestPacket>()
                .RegisterMessageType<RuckSackDetachRequestPacket>()
                .RegisterMessageType<RuckSackQuartzLightStatePacket>()
                .SetMessageHandler<RuckSackQuartzLightStatePacket>(RuckSackQuartzLightSystem.OnQuartzLightStatePacket);
        }

        private static void RegisterServerNetwork(ICoreServerAPI api)
        {
            if (schannel != null) return;

            schannel = api.Network
                .RegisterChannel(ChannelName)
                .RegisterMessageType<RuckSackAttachRequestPacket>()
                .RegisterMessageType<RuckSackDetachRequestPacket>()
                .RegisterMessageType<RuckSackQuartzLightStatePacket>()
                .SetMessageHandler<RuckSackAttachRequestPacket>(OnAttachRequest)
                .SetMessageHandler<RuckSackDetachRequestPacket>(OnDetachRequest);
        }

        private static void OnAttachRequest(IServerPlayer fromPlayer, RuckSackAttachRequestPacket packet)
        {
            if (packet == null) return;
            if (sapi?.World == null) return;
            if (!TryGetAndValidateActiveSlot(fromPlayer, packet.Kind, out ItemSlot? activeSlot)) return;

            BlockPos pos = new BlockPos(packet.X, packet.Y, packet.Z);
            if (sapi.World.BlockAccessor?.GetBlockEntity(pos) is not BlockEntityGroundStorage be) return;
            if (sapi.World.BlockAccessor.GetBlock(pos) is not BlockGroundStorage) return;
            ItemSlot? slot0 = be.Inventory?[0];
            ItemStack? rucksackStack = slot0?.Itemstack;
            string? rucksackPath = rucksackStack?.Collectible?.Code?.Path;

            if (rucksackPath == null || !rucksackPath.StartsWith("rucksack", StringComparison.OrdinalIgnoreCase)) return;
            ItemStack? consumedOneStack = null;
            if (activeSlot?.Itemstack != null)
            {
                consumedOneStack = activeSlot.Itemstack.Clone();
                consumedOneStack.StackSize = 1;
            }

            bool shouldConsume = RuckSackAttachmentApplier.ApplyAttachmentToRucksackStack(rucksackStack, packet.Kind, packet.Variant, consumedOneStack);

            if (shouldConsume)
            {
                ConsumeOneFromActiveSlot(activeSlot);
                RuckSackSounds.PlayAttach(sapi.World, pos, packet.Kind);
            }

            slot0?.MarkDirty();
            be.MarkDirty(true);
        }

        private static void OnDetachRequest(IServerPlayer fromPlayer, RuckSackDetachRequestPacket packet)
        {
            if (packet == null) return;
            if (sapi?.World == null) return;

            if (fromPlayer == null) return;
            if (fromPlayer.Entity?.Controls == null || !fromPlayer.Entity.Controls.Sneak) return;
            ItemSlot? activeSlot = fromPlayer.InventoryManager?.ActiveHotbarSlot;
            if (activeSlot == null) return;
            if (!activeSlot.Empty) return;

            BlockPos pos = new BlockPos(packet.X, packet.Y, packet.Z);
            if (sapi.World.BlockAccessor?.GetBlockEntity(pos) is not BlockEntityGroundStorage be) return;
            if (sapi.World.BlockAccessor.GetBlock(pos) is not BlockGroundStorage) return;
            ItemSlot? slot0 = be.Inventory?[0];
            ItemStack? rucksackStack = slot0?.Itemstack;
            string? rucksackPath = rucksackStack?.Collectible?.Code?.Path;

            if (rucksackPath == null || !rucksackPath.StartsWith("rucksack", StringComparison.OrdinalIgnoreCase)) return;

            if (!RuckSackAttachmentApplier.TryDetachAttachment(rucksackStack, packet.Kind, out string? returnStackBase64)) return;

            ItemStack? returnStack = TryRehydrateReturnStack(returnStackBase64);
            if (returnStack == null)
            {
                
                returnStack = TryCreateFallbackReturnStack(rucksackStack, packet.Kind);
            }

            if (returnStack != null)
            {
                bool gave = fromPlayer.InventoryManager != null && fromPlayer.InventoryManager.TryGiveItemstack(returnStack);
                if (!gave)
                {
                    try
                    {
                        sapi.World.SpawnItemEntity(returnStack, fromPlayer.Entity.Pos.XYZ);
                    }
                    catch
                    {
                        
                    }
                }
            }
            RuckSackSounds.PlayDetach(sapi.World, pos, packet.Kind);

            slot0?.MarkDirty();
            be.MarkDirty(true);
        }

        private static ItemStack? TryRehydrateReturnStack(string? base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            if (sapi?.World == null) return null;

            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                if (bytes == null || bytes.Length == 0) return null;

                ItemStack stack = new ItemStack(bytes);
                if (!stack.ResolveBlockOrItem(sapi.World)) return null;

                return stack;
            }
            catch
            {
                return null;
            }
        }

        private static ItemStack? TryCreateFallbackReturnStack(ItemStack rucksackStack, int kind)
        {
            if (sapi?.World == null) return null;
            ITreeAttribute? typesTree = rucksackStack.Attributes?.GetTreeAttribute("types");

            if (kind == (int)RuckSackAttachKind.Quartz)
            {
                
                string qtzTex = typesTree?.GetString("quartztex", null);
                string token = ExtractLastPathSegment(qtzTex);
                if (!string.IsNullOrEmpty(token))
                {
                    if (TryCreateStackByCode("game:embraced-quartz-" + token, out ItemStack? variantStack))
                    {
                        variantStack.StackSize = 1;
                        return variantStack;
                    }
                }

                if (TryCreateStackByCode("game:embraced-quartz", out ItemStack? baseStack))
                {
                    baseStack.StackSize = 1;
                    return baseStack;
                }

                return null;
            }

            if (kind == (int)RuckSackAttachKind.Bedroll)
            {
                if (TryCreateStackByCode("game:bedroll", out ItemStack? baseStack))
                {
                    baseStack.StackSize = 1;
                    return baseStack;
                }

                return null;
            }

            return null;
        }

        private static string ExtractLastPathSegment(string? path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            string s = path.Replace('\\', '/');
            int idx = s.LastIndexOf('/');
            if (idx >= 0 && idx < s.Length - 1) return s.Substring(idx + 1);

            return s;
        }

        private static bool TryCreateStackByCode(string code, out ItemStack? stack)
        {
            stack = null;
            if (sapi?.World == null) return false;

            try
            {
                AssetLocation loc = new AssetLocation(code);

                Item? item = sapi.World.GetItem(loc);
                if (item != null)
                {
                    stack = new ItemStack(item, 1);
                    return true;
                }

                Block? block = sapi.World.GetBlock(loc);
                if (block != null)
                {
                    stack = new ItemStack(block, 1);
                    return true;
                }
            }
            catch
            {
                
            }

            return false;
        }

        private static bool TryGetAndValidateActiveSlot(IServerPlayer fromPlayer, int kind, out ItemSlot? activeSlot)
        {
            activeSlot = null;

            if (fromPlayer == null) return false;
            if (fromPlayer.Entity?.Controls == null || !fromPlayer.Entity.Controls.Sneak) return false;

            activeSlot = fromPlayer.InventoryManager?.ActiveHotbarSlot;
            if (activeSlot == null || activeSlot.Empty) return false;

            string? codePath = activeSlot.Itemstack?.Collectible?.Code?.Path;
            if (string.IsNullOrEmpty(codePath)) return false;

            if (kind == (int)RuckSackAttachKind.Bedroll)
            {
                return codePath.StartsWith("bedroll", StringComparison.OrdinalIgnoreCase);
            }

            if (kind == (int)RuckSackAttachKind.Quartz)
            {
                if (codePath.Equals("embraced-quartz", StringComparison.OrdinalIgnoreCase)) return true;
                return codePath.StartsWith("embraced-quartz-", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static void ConsumeOneFromActiveSlot(ItemSlot activeSlot)
        {
            if (activeSlot == null || activeSlot.Empty) return;
            if (activeSlot.Itemstack == null) return;

            if (activeSlot.Itemstack.StackSize <= 1)
            {
                activeSlot.Itemstack = null;
            }
            else
            {
                activeSlot.Itemstack.StackSize -= 1;
            }

            activeSlot.MarkDirty();
        }
    }
}
