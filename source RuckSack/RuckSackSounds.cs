using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuckSack
{
    internal static class RuckSackSounds
    {

        private static readonly AssetLocation QuartzSound = new AssetLocation("game:sounds/block/glass");
        private static readonly AssetLocation BedrollSound = new AssetLocation("game:sounds/player/build");
        private static readonly AssetLocation LunchboxSound = new AssetLocation("game:sounds/player/build");

        internal static void PlayAttach(IWorldAccessor world, BlockPos pos, int kind)
        {
            if (world == null || pos == null) return;

            AssetLocation? sound = GetSoundForKind(kind);
            if (sound == null) return;

            world.PlaySoundAt(sound, pos, 0.5);
        }

        internal static void PlayDetach(IWorldAccessor world, BlockPos pos, int kind)
        {
            if (world == null || pos == null) return;

            AssetLocation? sound = GetSoundForKind(kind);
            if (sound == null) return;

            world.PlaySoundAt(sound, pos, 0.5);
        }

        private static AssetLocation? GetSoundForKind(int kind)
        {
            if (kind == (int)RuckSackAttachKind.Quartz) return QuartzSound;
            if (kind == (int)RuckSackAttachKind.Bedroll) return BedrollSound;
            if (kind == (int)RuckSackAttachKind.Lunchbox) return LunchboxSound;
            return null;
        }
    }
}
