
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RuckSack
{
    internal static class RuckSackQuartzLightSystem
    {
        private const string TypesTreeKey = "types";
        private const string QuartzAttachedKey = "quartz";
        private const string QuartzTexKey = "quartztex";
        private const string QuartzAttachedValue = "attached";

        private static ICoreClientAPI? capi;
        private static bool clientHooked;

        private static ICoreServerAPI? sapi;
        private static bool serverHooked;
        private static readonly Dictionary<long, QuartzLightState> desiredStates = new Dictionary<long, QuartzLightState>();
        private static readonly Dictionary<long, PlayerQuartzPointLight> activeLights = new Dictionary<long, PlayerQuartzPointLight>();
        private static readonly Dictionary<(int X, int Y, int Z), GroundStorageQuartzPointLight> activeGroundStorageLights = new Dictionary<(int X, int Y, int Z), GroundStorageQuartzPointLight>();
        private static readonly Dictionary<long, QuartzLightState> serverStates = new Dictionary<long, QuartzLightState>();
        private static readonly Dictionary<string, ServerHook> serverHooks = new Dictionary<string, ServerHook>(StringComparer.OrdinalIgnoreCase);

        internal static void InitClient(ICoreClientAPI api)
        {
            capi = api;

            if (clientHooked) return;
            clientHooked = true;

            api.Event.PlayerEntitySpawn += OnClientPlayerEntitySpawn;
            api.Event.PlayerEntityDespawn += OnClientPlayerEntityDespawn;
            api.Event.LeftWorld += OnClientLeftWorld;
        }

        internal static void InitServer(ICoreServerAPI api)
        {
            sapi = api;

            if (serverHooked) return;
            serverHooked = true;

            api.Event.PlayerNowPlaying += OnServerPlayerNowPlaying;
            api.Event.PlayerDisconnect += OnServerPlayerDisconnect;
            api.Event.PlayerLeave += OnServerPlayerDisconnect;
            api.Event.PlayerRespawn += OnServerPlayerRespawn;
        }

        internal static void Dispose()
        {

            if (capi != null)
            {
                try
                {
                    capi.Event.PlayerEntitySpawn -= OnClientPlayerEntitySpawn;
                    capi.Event.PlayerEntityDespawn -= OnClientPlayerEntityDespawn;
                    capi.Event.LeftWorld -= OnClientLeftWorld;
                }
                catch
                {

                }

                RemoveAllClientLights();
                RemoveAllClientGroundStorageLights();

                desiredStates.Clear();
                activeLights.Clear();
                activeGroundStorageLights.Clear();

                capi = null;
                clientHooked = false;
            }
            if (sapi != null)
            {
                try
                {
                    sapi.Event.PlayerNowPlaying -= OnServerPlayerNowPlaying;
                    sapi.Event.PlayerDisconnect -= OnServerPlayerDisconnect;
                    sapi.Event.PlayerLeave -= OnServerPlayerDisconnect;
                    sapi.Event.PlayerRespawn -= OnServerPlayerRespawn;
                }
                catch
                {

                }

                UnhookAllServerPlayers();
                serverStates.Clear();

                sapi = null;
                serverHooked = false;
            }
        }

        internal static void OnQuartzLightStatePacket(RuckSackQuartzLightStatePacket packet)
        {
            if (packet == null) return;

            long entityId = packet.EntityId;

            QuartzLightState state = new QuartzLightState();
            state.Active = packet.Active;
            state.H = ClampLightH(packet.H);
            state.S = ClampLightS(packet.S);
            state.V = ClampLightV(packet.V);

            desiredStates[entityId] = state;
            if (capi?.World != null)
            {
                try
                {
                    Entity? ent = capi.World.GetEntityById(entityId);
                    if (ent != null)
                    {
                        ApplyClientStateToEntity(ent, state);
                    }
                }
                catch
                {

                }
            }
        }
        private static void OnClientPlayerEntitySpawn(IClientPlayer player)
        {
            if (player == null) return;
            if (player.Entity == null) return;

            long entityId;
            try
            {
                entityId = player.Entity.EntityId;
            }
            catch
            {
                return;
            }

            if (desiredStates.TryGetValue(entityId, out QuartzLightState state))
            {
                ApplyClientStateToEntity(player.Entity, state);
            }
        }

        private static void OnClientPlayerEntityDespawn(IClientPlayer player)
        {
            if (player == null) return;
            if (player.Entity == null) return;

            long entityId;
            try
            {
                entityId = player.Entity.EntityId;
            }
            catch
            {
                return;
            }

            RemoveClientLight(entityId);
        }

        private static void OnClientLeftWorld()
        {
            RemoveAllClientLights();
            RemoveAllClientGroundStorageLights();
            desiredStates.Clear();
        }

        private static void ApplyClientStateToEntity(Entity entity, QuartzLightState state)
        {
            if (capi == null) return;

            long entityId;
            try
            {
                entityId = entity.EntityId;
            }
            catch
            {
                return;
            }

            if (!state.Active)
            {
                RemoveClientLight(entityId);
                return;
            }

            Vec3f rgb = HsvBytesToRgbVec3f(state.H, state.S, state.V);

            if (activeLights.TryGetValue(entityId, out PlayerQuartzPointLight? light))
            {
                light.SetColor(rgb);
                return;
            }

            PlayerQuartzPointLight newLight = new PlayerQuartzPointLight(entity, rgb);

            try
            {
                capi.Render.AddPointLight(newLight);
                activeLights[entityId] = newLight;
            }
            catch
            {

            }
        }

        private static void RemoveClientLight(long entityId)
        {
            if (capi == null) return;

            if (!activeLights.TryGetValue(entityId, out PlayerQuartzPointLight? light)) return;

            try
            {
                capi.Render.RemovePointLight(light);
            }
            catch
            {

            }

            activeLights.Remove(entityId);
        }

        private static void RemoveAllClientLights()
        {
            if (capi == null) return;

            foreach (PlayerQuartzPointLight light in activeLights.Values)
            {
                try
                {
                    capi.Render.RemovePointLight(light);
                }
                catch
                {

                }
            }

            activeLights.Clear();
        }
        internal static void ClientUpdateGroundStorageLight(int x, int y, int z, ItemStack? stack)
        {
            if (capi == null) return;

            if (!TryGetQuartzLightRgbFromRuckSackStack(stack, out Vec3f? rgb))
            {
                ClientRemoveGroundStorageLight(x, y, z);
                return;
            }

            var key = (X: x, Y: y, Z: z);

            if (activeGroundStorageLights.TryGetValue(key, out GroundStorageQuartzPointLight? light))
            {
                light.SetColor(rgb);
                return;
            }

            GroundStorageQuartzPointLight newLight = new GroundStorageQuartzPointLight(x, y, z, rgb);

            try
            {
                capi.Render.AddPointLight(newLight);
                activeGroundStorageLights[key] = newLight;
            }
            catch
            {

            }
        }

        internal static void ClientRemoveGroundStorageLight(int x, int y, int z)
        {
            if (capi == null) return;

            var key = (X: x, Y: y, Z: z);

            if (!activeGroundStorageLights.TryGetValue(key, out GroundStorageQuartzPointLight? light)) return;

            try
            {
                capi.Render.RemovePointLight(light);
            }
            catch
            {

            }

            activeGroundStorageLights.Remove(key);
        }

        private static void RemoveAllClientGroundStorageLights()
        {
            if (capi == null) return;

            foreach (GroundStorageQuartzPointLight light in activeGroundStorageLights.Values)
            {
                try
                {
                    capi.Render.RemovePointLight(light);
                }
                catch
                {

                }
            }

            activeGroundStorageLights.Clear();
        }

        internal static bool TryGetQuartzLightRgbFromRuckSackStack(ItemStack? stack, out Vec3f? rgb)
        {
            rgb = default;

            if (stack == null) return false;
            if (!IsTargetRuckSack(stack)) return false;

            if (!TryGetQuartzTypeFromRuckSackStack(stack, out string? quartzType)) return false;

            byte h, s, v;
            ResolveQuartzTypeToHsv(quartzType, out h, out s, out v);
            rgb = HsvBytesToRgbVec3f(h, s, v);
            return true;
        }
        private static void OnServerPlayerNowPlaying(IServerPlayer player)
        {
            if (player == null) return;
            if (sapi == null) return;

            HookServerPlayerInventories(player);
            foreach (KeyValuePair<long, QuartzLightState> kvp in serverStates)
            {
                QuartzLightState st = kvp.Value;
                if (!st.Active) continue;
                RuckSackNetworking.SendQuartzLightStateToPlayer(player, kvp.Key, true, st.H, st.S, st.V);
            }
            RecomputeAndBroadcast(player);
        }

        private static void OnServerPlayerRespawn(IServerPlayer player)
        {
            if (player == null) return;
            RecomputeAndBroadcast(player);
        }

        private static void OnServerPlayerDisconnect(IServerPlayer player)
        {
            if (player == null) return;

            UnhookServerPlayerInventories(player);

            long entityId;
            try
            {
                entityId = player.Entity?.EntityId ?? 0;
            }
            catch
            {
                entityId = 0;
            }

            if (entityId != 0)
            {
                if (serverStates.Remove(entityId))
                {

                    RuckSackNetworking.BroadcastQuartzLightState(entityId, false, 0, 0, 0);
                }
            }
        }

        private static void HookServerPlayerInventories(IServerPlayer player)
        {
            if (sapi == null) return;

            string uid = player.PlayerUID;
            if (string.IsNullOrEmpty(uid)) return;
            if (serverHooks.ContainsKey(uid)) return;

            ServerHook hook = new ServerHook();
            hook.PlayerUid = uid;
            IInventory? backpackInv = null;
            try
            {
                backpackInv = player.InventoryManager?.GetOwnInventory("backpack");
            }
            catch
            {
                backpackInv = null;
            }

            if (backpackInv != null)
            {
                hook.BackpackInv = backpackInv;

                Action<int> handler = _ => OnServerPlayerAnyInventorySlotModified(uid);
                hook.BackpackHandler = handler;
                backpackInv.SlotModified += handler;
            }
            IInventory? characterInv = null;
            try
            {
                characterInv = player.InventoryManager?.GetOwnInventory("character");
            }
            catch
            {
                characterInv = null;
            }

            if (characterInv != null)
            {
                hook.CharacterInv = characterInv;

                Action<int> handler = _ => OnServerPlayerAnyInventorySlotModified(uid);
                hook.CharacterHandler = handler;
                characterInv.SlotModified += handler;
            }

            serverHooks[uid] = hook;
        }

        private static void UnhookServerPlayerInventories(IServerPlayer player)
        {
            string uid = player.PlayerUID;
            if (string.IsNullOrEmpty(uid)) return;

            if (!serverHooks.TryGetValue(uid, out ServerHook? hook)) return;

            try
            {
                if (hook.BackpackInv != null && hook.BackpackHandler != null)
                {
                    hook.BackpackInv.SlotModified -= hook.BackpackHandler;
                }
            }
            catch
            {

            }

            try
            {
                if (hook.CharacterInv != null && hook.CharacterHandler != null)
                {
                    hook.CharacterInv.SlotModified -= hook.CharacterHandler;
                }
            }
            catch
            {

            }

            serverHooks.Remove(uid);
        }

        private static void UnhookAllServerPlayers()
        {
            foreach (ServerHook hook in serverHooks.Values)
            {
                try
                {
                    if (hook.BackpackInv != null && hook.BackpackHandler != null)
                    {
                        hook.BackpackInv.SlotModified -= hook.BackpackHandler;
                    }
                }
                catch
                {

                }

                try
                {
                    if (hook.CharacterInv != null && hook.CharacterHandler != null)
                    {
                        hook.CharacterInv.SlotModified -= hook.CharacterHandler;
                    }
                }
                catch
                {

                }
            }

            serverHooks.Clear();
        }

        private static void OnServerPlayerAnyInventorySlotModified(string playerUid)
        {
            if (sapi?.World == null) return;

            IServerPlayer? player;
            try
            {
                player = sapi.World.PlayerByUid(playerUid) as IServerPlayer;
            }
            catch
            {
                player = null;
            }

            if (player == null) return;

            RecomputeAndBroadcast(player);
        }

        private static void RecomputeAndBroadcast(IServerPlayer player)
        {
            if (player == null) return;

            long entityId;
            try
            {
                entityId = player.Entity?.EntityId ?? 0;
            }
            catch
            {
                entityId = 0;
            }

            if (entityId == 0) return;

            QuartzLightState newState = ComputeQuartzLightState(player);

            bool changed = true;
            if (serverStates.TryGetValue(entityId, out QuartzLightState oldState))
            {
                changed = oldState.Active != newState.Active || oldState.H != newState.H || oldState.S != newState.S || oldState.V != newState.V;
            }

            serverStates[entityId] = newState;

            if (changed)
            {
                if (newState.Active)
                {
                    RuckSackNetworking.BroadcastQuartzLightState(entityId, true, newState.H, newState.S, newState.V);
                }
                else
                {
                    RuckSackNetworking.BroadcastQuartzLightState(entityId, false, 0, 0, 0);
                }
            }
        }

        private static QuartzLightState ComputeQuartzLightState(IServerPlayer player)
        {
            QuartzLightState state = new QuartzLightState();
            if (TryFindQuartzRuckSackInInventory(player, "backpack", out string? quartzType))
            {
                state.Active = true;
                ResolveQuartzTypeToHsv(quartzType, out state.H, out state.S, out state.V);
                return state;
            }

            if (TryFindQuartzRuckSackInInventory(player, "character", out quartzType))
            {
                state.Active = true;
                ResolveQuartzTypeToHsv(quartzType, out state.H, out state.S, out state.V);
                return state;
            }

            state.Active = false;
            state.H = 0;
            state.S = 0;
            state.V = 0;
            return state;
        }

        private static bool TryFindQuartzRuckSackInInventory(IServerPlayer player, string invClassName, out string? quartzType)
        {
            quartzType = null;

            if (player == null) return false;
            if (player.InventoryManager == null) return false;

            IInventory? inv;
            try
            {
                inv = player.InventoryManager.GetOwnInventory(invClassName);
            }
            catch
            {
                inv = null;
            }

            if (inv == null) return false;

            foreach (ItemSlot slot in inv)
            {
                if (slot is not ItemSlotBackpack) continue;

                ItemStack? stack = slot?.Itemstack;
                if (!IsTargetRuckSack(stack)) continue;

                if (TryGetQuartzTypeFromRuckSackStack(stack, out string? type))
                {
                    quartzType = type;
                    return true;
                }
            }

            return false;
        }

        private static bool IsTargetRuckSack(ItemStack? stack)
        {
            if (stack?.Collectible?.Code == null) return false;

            string? domain = stack.Collectible.Code.Domain;
            if (domain == null || !domain.Equals("aldiclasses", StringComparison.OrdinalIgnoreCase)) return false;

            string? path = stack.Collectible.Code.Path;
            if (path == null) return false;

            return path.StartsWith("rucksack", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetQuartzTypeFromRuckSackStack(ItemStack stack, out string? quartzType)
        {
            quartzType = null;

            ITreeAttribute? typesTree = stack.Attributes?.GetTreeAttribute(TypesTreeKey);
            if (typesTree == null) return false;

            string? quartzFlag = typesTree.GetString(QuartzAttachedKey, null);
            if (quartzFlag == null || !quartzFlag.Equals(QuartzAttachedValue, StringComparison.OrdinalIgnoreCase)) return false;
            string? qtzTex = typesTree.GetString(QuartzTexKey, null);
            if (string.IsNullOrEmpty(qtzTex)) return false;

            string token = ExtractLastPathSegment(qtzTex);
            if (string.IsNullOrEmpty(token)) return false;

            quartzType = token;
            return true;
        }

        private static string ExtractLastPathSegment(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            string s = path.Replace('\\', '/');
            int idx = s.LastIndexOf('/');
            if (idx >= 0 && idx < s.Length - 1) return s.Substring(idx + 1);

            return s;
        }
        private static void ResolveQuartzTypeToHsv(string? quartzType, out byte h, out byte s, out byte v)
        {
            h = 0;
            s = 0;
            v = 13;

            if (string.IsNullOrEmpty(quartzType)) return;

            string t = NormalizeQuartzTypeToken(quartzType);
            if (t == "quartz") { h = 0; s = 0; v = 13; return; }
            if (t == "smokyquartz") { h = 2; s = 2; v = 13; return; }
            if (t == "rosyquartz") { h = 53; s = 3; v = 13; return; }
            if (t == "olivine") { h = 22; s = 7; v = 13; return; }
            if (t == "cinnabar") { h = 0; s = 7; v = 13; return; }
            if (t == "amethyst") { h = 52; s = 7; v = 13; return; }
            if (t == "sulfur") { h = 11; s = 7; v = 13; return; }
            if (t == "lapislazuli") { h = 39; s = 7; v = 13; return; }
            if (t == "sylvite") { h = 4; s = 4; v = 13; return; }
            if (t == "clearquartz") { h = 0; s = 0; v = 13; return; }
            if (t == "smoky") { h = 2; s = 2; v = 13; return; }
            if (t == "rosequartz") { h = 53; s = 3; v = 13; return; }
            if (t == "pink") { h = 53; s = 3; v = 13; return; }
            if (t == "blue") { h = 39; s = 7; v = 13; return; }
            if (t == "red") { h = 0; s = 7; v = 13; return; }
            if (t == "yellow") { h = 11; s = 7; v = 13; return; }
            if (t == "green") { h = 22; s = 7; v = 13; return; }
            if (t == "emerald") { h = 22; s = 7; v = 13; return; }
        }

        private static string NormalizeQuartzTypeToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";

            string t = token.Replace('\\', '/').Trim().ToLowerInvariant();
            int slash = t.LastIndexOf('/');
            if (slash >= 0 && slash < t.Length - 1)
            {
                t = t.Substring(slash + 1);
            }
            if (t.StartsWith("up-", StringComparison.OrdinalIgnoreCase)) t = t.Substring(3);
            else if (t.StartsWith("down-", StringComparison.OrdinalIgnoreCase)) t = t.Substring(5);
            if (t.EndsWith("-up", StringComparison.OrdinalIgnoreCase)) t = t.Substring(0, t.Length - 3);
            else if (t.EndsWith("-down", StringComparison.OrdinalIgnoreCase)) t = t.Substring(0, t.Length - 5);
            if (t == "clearquartz") return "quartz";
            if (t == "rosequartz") return "rosyquartz";

            return t;
        }

        private static Vec3f HsvBytesToRgbVec3f(byte h, byte s, byte v)
        {
            if (h > 63) h = 63;
            if (s > 7) s = 7;
            if (v > 31) v = 31;

            int blockh = (int)(h / 63f * 255f);
            int blocks = (int)(s / 7f * 255f);
            int blockv = (int)(capi!.World.BlockLightLevels[v] * 255f);

            int col = ColorUtil.HsvToRgba(blockh, blocks, blockv);

            Vec3f color = new Vec3f();
            ColorUtil.ToRGBVec3f(col, ref color);

            return new Vec3f(color.X * v, color.Y * v, color.Z * v);
        }

        private static byte ClampLightH(int value)
        {
            if (value < 0) return 0;
            if (value > 63) return 63;
            return (byte)value;
        }

        private static byte ClampLightS(int value)
        {
            if (value < 0) return 0;
            if (value > 7) return 7;
            return (byte)value;
        }

        private static byte ClampLightV(int value)
        {
            if (value < 0) return 0;
            if (value > 31) return 31;
            return (byte)value;
        }

        private struct QuartzLightState
        {
            public bool Active;
            public byte H;
            public byte S;
            public byte V;
        }

        private sealed class ServerHook
        {
            public string? PlayerUid;

            public IInventory? BackpackInv;
            public Action<int>? BackpackHandler;

            public IInventory? CharacterInv;
            public Action<int>? CharacterHandler;
        }

        private sealed class PlayerQuartzPointLight : IPointLight
        {
            private readonly Entity entity;
            private Vec3f color;

            public PlayerQuartzPointLight(Entity entity, Vec3f color)
            {
                this.entity = entity;
                this.color = color;
            }

            public Vec3f Color => color;

            public Vec3d Pos
            {
                get
                {
                    return new Vec3d(entity.Pos.X, entity.Pos.Y + entity.SelectionBox.Y2 * 0.65, entity.Pos.Z);
                }
            }

            public void SetColor(Vec3f rgb)
            {
                color = rgb;
            }
        }

        private sealed class GroundStorageQuartzPointLight : IPointLight
        {
            private readonly Vec3d pos;
            private Vec3f color;

            public GroundStorageQuartzPointLight(int x, int y, int z, Vec3f color)
            {

                pos = new Vec3d(x + 0.5, y + 0.5, z + 0.5);
                this.color = color;
            }

            public Vec3f Color => color;

            public Vec3d Pos => pos;

            public void SetColor(Vec3f rgb)
            {
                color = rgb;
            }
        }
    }
}
