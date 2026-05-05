
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuckSack
{
    internal static class RuckSackGroundStorageUtil
    {
        internal static bool TryGetStoredRucksack(IWorldAccessor world, BlockPos pos, out ItemStack? rucksackStack)
        {
            rucksackStack = null;

            if (world.BlockAccessor?.GetBlockEntity(pos) is not BlockEntityGroundStorage be)
            {
                return false;
            }
            ItemStack? stack0 = be.Inventory?[0]?.Itemstack;
            string? path = stack0?.Collectible?.Code?.Path;

            if (path != null && path.StartsWith("rucksack", StringComparison.OrdinalIgnoreCase))
            {
                rucksackStack = stack0;
                return true;
            }

            return false;
        }
    }
}
