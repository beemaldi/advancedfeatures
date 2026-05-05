
using System;

namespace RuckSack
{
    internal static class RuckSackTextureTokens
    {
        internal const string BedrollTextureBasePrefix = "game:block/cloth/linen/";
        internal const string QuartzTextureBasePrefix = "game:item/resource/ungraded/";
        internal const string BedrollTextureDefaultToken = "block/cloth/linen/brown";
        internal const string QuartzTextureDefaultToken = "item/resource/ungraded/clearquartz";

        internal static string NormalizeTextureBaseToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return token;
            string s = token.Replace('\\', '/').Trim();

            int colonIdx = s.IndexOf(':');
            if (colonIdx >= 0 && colonIdx < s.Length - 1)
            {
                
                s = s.Substring(colonIdx + 1);
            }

            if (s.StartsWith("textures/", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring("textures/".Length);
            }

            if (s.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 4);
            }
            return s;
        }

        internal static string NormalizeUpDownSuffix(string token)
        {
            if (string.IsNullOrEmpty(token)) return token;
            if (token.StartsWith("up-", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(3);
            }
            else if (token.StartsWith("down-", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(5);
            }

            if (token.EndsWith("-up", StringComparison.OrdinalIgnoreCase))
            {
                return token.Substring(0, token.Length - 3);
            }

            if (token.EndsWith("-down", StringComparison.OrdinalIgnoreCase))
            {
                return token.Substring(0, token.Length - 5);
            }

            return token;
        }

        internal static string BuildBedrollTexBase(string bedrollColorToken)
        {
            return bedrollColorToken;
        }

        internal static string BuildQuartzTexBase(string quartzVariantToken)
        {
            return quartzVariantToken;
        }
    }
}
