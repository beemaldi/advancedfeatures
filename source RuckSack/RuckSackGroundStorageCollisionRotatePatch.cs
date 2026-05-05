
using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuckSack
{
    [HarmonyPatch(typeof(BlockEntityGroundStorage))]
    internal static class RuckSackGroundStorageCollisionRotatePatch
    {
        private const float EastWestForwardOffset = -0.1925f; 

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityGroundStorage.GetCollisionBoxes))]
        private static void GetCollisionBoxes_Postfix(BlockEntityGroundStorage __instance, ref Cuboidf[] __result)
        {
            if (!ShouldAdjustForRucksack(__instance, __result, out float meshAngle)) return;
            __result = RotateAndMaybeOffsetBoxes(__result, meshAngle);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityGroundStorage.GetSelectionBoxes))]
        private static void GetSelectionBoxes_Postfix(BlockEntityGroundStorage __instance, ref Cuboidf[] __result)
        {
            if (!ShouldAdjustForRucksack(__instance, __result, out float meshAngle)) return;
            __result = RotateAndMaybeOffsetBoxes(__result, meshAngle);
        }

        private static bool ShouldAdjustForRucksack(BlockEntityGroundStorage be, Cuboidf[]? boxes, out float meshAngle)
        {
            meshAngle = 0f;

            if (be == null) return false;
            if (boxes == null || boxes.Length == 0) return false;

            ItemStack? stack0 = null;
            try
            {
                stack0 = be.Inventory?[0]?.Itemstack;
            }
            catch
            {
                
            }

            string? path = stack0?.Collectible?.Code?.Path;
            if (path == null || !path.StartsWith("rucksack", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            float angle;
            try
            {
                angle = be.MeshAngle;
            }
            catch
            {
                return false;
            }

            angle = NormalizeAngleRadians(angle);
            if (MathF.Abs(angle) < 0.0001f) return false;

            meshAngle = angle;
            return true;
        }

        private static Cuboidf[] RotateAndMaybeOffsetBoxes(Cuboidf[] source, float angleRadians)
        {
            
            Cuboidf[] adjusted = new Cuboidf[source.Length];

            float angle = NormalizeAngleRadians(angleRadians);
            float sin = MathF.Sin(angle);
            float cos = MathF.Cos(angle);

            bool isEastWest = IsFacingEastWest(angle);
            float dx = 0f;
            float dz = 0f;

            if (isEastWest)
            {
                float signX = SignNonZero(sin);
                dx = signX * EastWestForwardOffset;
                dz = 0f;
            }

            for (int i = 0; i < source.Length; i++)
            {
                Cuboidf box = source[i];
                if (box == null)
                {
                    adjusted[i] = box;
                    continue;
                }

                Cuboidf clone = box.Clone();
                RotateAabbXZInPlace(clone, cos, sin);
                if (isEastWest && (dx != 0f || dz != 0f))
                {
                    TranslateAabbXZInPlace(clone, dx, dz);
                }

                adjusted[i] = clone;
            }

            return adjusted;
        }

        private static void RotateAabbXZInPlace(Cuboidf box, float cos, float sin)
        {
            float x1 = box.X1;
            float x2 = box.X2;
            float z1 = box.Z1;
            float z2 = box.Z2;

            RotatePointXZ(x1, z1, cos, sin, out float rx1, out float rz1);
            RotatePointXZ(x1, z2, cos, sin, out float rx2, out float rz2);
            RotatePointXZ(x2, z1, cos, sin, out float rx3, out float rz3);
            RotatePointXZ(x2, z2, cos, sin, out float rx4, out float rz4);

            float minX = MathF.Min(MathF.Min(rx1, rx2), MathF.Min(rx3, rx4));
            float maxX = MathF.Max(MathF.Max(rx1, rx2), MathF.Max(rx3, rx4));
            float minZ = MathF.Min(MathF.Min(rz1, rz2), MathF.Min(rz3, rz4));
            float maxZ = MathF.Max(MathF.Max(rz1, rz2), MathF.Max(rz3, rz4));

            box.X1 = minX;
            box.X2 = maxX;
            box.Z1 = minZ;
            box.Z2 = maxZ;
        }

        private static void TranslateAabbXZInPlace(Cuboidf box, float dx, float dz)
        {
            box.X1 += dx;
            box.X2 += dx;
            box.Z1 += dz;
            box.Z2 += dz;
        }

        private static void RotatePointXZ(float x, float z, float cos, float sin, out float rx, out float rz)
        {
            
            float dx = x - 0.5f;
            float dz = z - 0.5f;

            rx = (dx * cos) - (dz * sin) + 0.5f;
            rz = (dx * sin) + (dz * cos) + 0.5f;
        }

        private static bool IsFacingEastWest(float angle)
        {
            float twoPi = MathF.PI * 2f;
            float a = angle % twoPi;
            if (a < 0f) a += twoPi;
            float quarter = MathF.PI / 2f;
            int idx = (int)MathF.Round(a / quarter) & 3;

            return idx == 1 || idx == 3;
        }

        private static float SignNonZero(float v)
        {
            if (v > 0f) return 1f;
            if (v < 0f) return -1f;
            return 1f; 
        }

        private static float NormalizeAngleRadians(float angle)
        {
            
            float twoPi = MathF.PI * 2f;
            if (twoPi == 0f) return angle;

            angle %= twoPi;
            if (angle > MathF.PI) angle -= twoPi;
            if (angle < -MathF.PI) angle += twoPi;
            return angle;
        }
    }
}
