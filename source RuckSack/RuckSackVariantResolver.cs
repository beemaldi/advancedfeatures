
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace RuckSack
{
    internal static class RuckSackVariantResolver
    {
        internal static string? TryExtractBedrollTextureBase(ItemStack stack)
        {
            if (stack == null) return null;

            string? codePath = stack.Collectible?.Code?.Path;
            if (!string.IsNullOrEmpty(codePath))
            {
                if (codePath.Equals("bedroll", StringComparison.OrdinalIgnoreCase))
                {
                    return RuckSackTextureTokens.BedrollTextureDefaultToken;
                }

                const string prefix = "bedroll-";
                if (codePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string remainder = codePath.Substring(prefix.Length);
                    string[] parts = remainder.Split('-');
                    string color = parts.Length > 0 ? parts[0] : remainder;

                    color = RuckSackTextureTokens.NormalizeUpDownSuffix(color);

                    if (!string.IsNullOrEmpty(color))
                    {
                        return "block/cloth/linen/" + color;
                    }
                }
            }

            return null;
        }

        internal static string? TryExtractQuartzTextureBase(ItemStack stack)
        {
            if (stack == null) return null;
            string? codePath = stack.Collectible?.Code?.Path;
            if (!string.IsNullOrEmpty(codePath))
            {
                if (codePath.Equals("embraced-quartz", StringComparison.OrdinalIgnoreCase))
                {
                    return RuckSackTextureTokens.QuartzTextureDefaultToken;
                }

                const string prefix = "embraced-quartz-";
                if (codePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string token = codePath.Substring(prefix.Length);
                    token = RuckSackTextureTokens.NormalizeUpDownSuffix(token);
                    if (!string.IsNullOrEmpty(token))
                    {
                        return "item/resource/ungraded/" + token;
                    }
                }
            }
            if (stack.Item?.Textures != null && stack.Item.Textures.Count > 0)
            {
                
                if (stack.Item.Textures.TryGetValue("quartz", out CompositeTexture? quartz) && quartz?.Base != null)
                {
                    return RuckSackTextureTokens.NormalizeTextureBaseToken(quartz.Base.ToString());
                }

                foreach (CompositeTexture tex in stack.Item.Textures.Values)
                {
                    if (tex?.Base != null) return RuckSackTextureTokens.NormalizeTextureBaseToken(tex.Base.ToString());
                }
            }

            if (stack.Collectible?.Attributes == null) return null;

            JsonObject texObj = stack.Collectible.Attributes["textures"];
            if (texObj == null || !texObj.Exists) return null;
            foreach (JsonObject texEntry in texObj)
            {
                if (!texEntry.Exists) continue;

                string basePath = texEntry["base"].AsString(null);
                if (!string.IsNullOrEmpty(basePath))
                {
                    return RuckSackTextureTokens.NormalizeTextureBaseToken(basePath);
                }
            }

            return null;
        }

        internal static bool IsBedrollOrEmbracedQuartz(string codePath)
        {
            if (codePath == null) return false;

            if (codePath.StartsWith("bedroll", StringComparison.OrdinalIgnoreCase)) return true;
            if (codePath.Equals("embraced-quartz", StringComparison.OrdinalIgnoreCase)) return true;
            if (codePath.StartsWith("embraced-quartz-", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        internal static string? TryExtractBedrollColorToken(string codePath)
        {
            if (codePath == null) return null;
            const string prefix = "bedroll-";

            if (codePath.Equals("bedroll", StringComparison.OrdinalIgnoreCase))
            {
                return RuckSackTextureTokens.BedrollTextureDefaultToken;
            }

            if (codePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string token = codePath.Substring(prefix.Length);
                token = RuckSackTextureTokens.NormalizeUpDownSuffix(token);
                return token.Length > 0 ? token : RuckSackTextureTokens.BedrollTextureDefaultToken;
            }

            return null;
        }

        internal static string? TryExtractQuartzVariantToken(string codePath)
        {
            if (codePath == null) return null;
            const string baseCode = "embraced-quartz";
            const string prefix = "embraced-quartz-";

            if (codePath.Equals(baseCode, StringComparison.OrdinalIgnoreCase))
            {
                return RuckSackTextureTokens.QuartzTextureDefaultToken;
            }

            if (codePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string token = codePath.Substring(prefix.Length);
                token = RuckSackTextureTokens.NormalizeUpDownSuffix(token);
                return token.Length > 0 ? token : RuckSackTextureTokens.QuartzTextureDefaultToken;
            }

            return null;
        }
    }
}
