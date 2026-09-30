using HarmonyLib;
using Vintagestory.API.Common;

namespace RuckSack
{
    [HarmonyPatch(typeof(InventoryBase), "hasHeatResistantHandGear")]
    public static class HeatResistantWearablePatch
    {
        public static void Postfix(IPlayer player, ref bool __result)
        {
            if (__result || player?.InventoryManager == null) return;

            IInventory inv = player.InventoryManager.GetOwnInventory("character");
            if (inv == null) return;

            foreach (ItemSlot slot in inv)
            {
                if (slot.Itemstack?.Collectible.Attributes?.IsTrue("heatResistantGloves") == true)
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
