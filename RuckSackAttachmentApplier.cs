
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace RuckSack
{
    internal static class RuckSackAttachmentApplier
    {
        internal static bool ApplyAttachmentToRucksackStack(ItemStack rucksackStack, int kind, string? variantToken)
        {
            return ApplyAttachmentToRucksackStack(rucksackStack, kind, variantToken, null);
        }

        internal static bool ApplyAttachmentToRucksackStack(ItemStack rucksackStack, int kind, string? variantToken, ItemStack? consumedOneStack)
        {
            if (rucksackStack == null) return false;

            rucksackStack.Attributes ??= new TreeAttribute();

            ITreeAttribute? typesTree = rucksackStack.Attributes.GetTreeAttribute("types");
            if (typesTree == null)
            {
                rucksackStack.Attributes["types"] = new TreeAttribute();
                typesTree = rucksackStack.Attributes.GetTreeAttribute("types");
                if (typesTree == null) return false;
            }
            typesTree.SetString("arl", "1");
            if (!typesTree.HasAttribute("bedroll")) typesTree.SetString("bedroll", "none");
            if (!typesTree.HasAttribute("quartz")) typesTree.SetString("quartz", "none");
            EnsureDefaultArlTextureTokens(typesTree);

            string bedrollState = typesTree.HasAttribute("bedroll") ? typesTree.GetString("bedroll") : "none";
            string quartzState = typesTree.HasAttribute("quartz") ? typesTree.GetString("quartz") : "none";

            bool bedrollAttached = bedrollState.Equals("attached", StringComparison.OrdinalIgnoreCase);
            bool quartzAttached = quartzState.Equals("attached", StringComparison.OrdinalIgnoreCase);

            if (kind == (int)RuckSackAttachKind.Bedroll)
            {
                
                if (bedrollAttached) return false;
                bool shouldConsume = true;

                typesTree.SetString("bedroll", "attached");

                if (!string.IsNullOrEmpty(variantToken))
                {
                    
                    string normalized = RuckSackTextureTokens.NormalizeTextureBaseToken(variantToken);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        typesTree.SetString("bedrolltex", normalized);
                    }
                    else
                    {
                        EnsureDefaultArlTextureTokens(typesTree);
                    }
                }
                else
                {
                    
                    EnsureDefaultArlTextureTokens(typesTree);
                }
                if (bedrollAttached && quartzAttached)
                {
                    typesTree.SetString("quartz", "none");
                }

                if (shouldConsume && consumedOneStack != null)
                {
                    StoreReturnStack(typesTree, kind, consumedOneStack);
                }

                return shouldConsume;
            }

            if (kind == (int)RuckSackAttachKind.Quartz)
            {
                
                if (quartzAttached) return false;
                bool shouldConsume = true;

                typesTree.SetString("quartz", "attached");

                if (!string.IsNullOrEmpty(variantToken))
                {
                    
                    string normalized = RuckSackTextureTokens.NormalizeTextureBaseToken(variantToken);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        typesTree.SetString("quartztex", normalized);
                    }
                    else
                    {
                        EnsureDefaultArlTextureTokens(typesTree);
                    }
                }
                else
                {
                    
                    EnsureDefaultArlTextureTokens(typesTree);
                }
                if (bedrollAttached && quartzAttached)
                {
                    typesTree.SetString("bedroll", "none");
                }

                if (shouldConsume && consumedOneStack != null)
                {
                    StoreReturnStack(typesTree, kind, consumedOneStack);
                }

                return shouldConsume;
            }

            return false;
        }

        internal static bool TryDetachAttachment(ItemStack rucksackStack, int kind, out string? returnStackBase64)
        {
            returnStackBase64 = null;

            if (rucksackStack == null) return false;

            rucksackStack.Attributes ??= new TreeAttribute();

            ITreeAttribute? typesTree = rucksackStack.Attributes.GetTreeAttribute("types");
            if (typesTree == null)
            {
                
                return false;
            }

            if (!typesTree.HasAttribute("bedroll")) typesTree.SetString("bedroll", "none");
            if (!typesTree.HasAttribute("quartz")) typesTree.SetString("quartz", "none");

            string bedrollState = typesTree.GetString("bedroll", "none");
            string quartzState = typesTree.GetString("quartz", "none");

            bool bedrollAttached = bedrollState.Equals("attached", StringComparison.OrdinalIgnoreCase);
            bool quartzAttached = quartzState.Equals("attached", StringComparison.OrdinalIgnoreCase);

            if (kind == (int)RuckSackAttachKind.Bedroll)
            {
                if (!bedrollAttached) return false;

                returnStackBase64 = typesTree.GetString("bedrollstack", null);
                typesTree.SetString("bedroll", "none");
                return true;
            }

            if (kind == (int)RuckSackAttachKind.Quartz)
            {
                if (!quartzAttached) return false;

                returnStackBase64 = typesTree.GetString("quartzstack", null);
                typesTree.SetString("quartz", "none");
                return true;
            }

            return false;
        }

        private static void StoreReturnStack(ITreeAttribute typesTree, int kind, ItemStack stack)
        {
            if (typesTree == null || stack == null) return;

            string key = kind == (int)RuckSackAttachKind.Bedroll
                ? "bedrollstack"
                : "quartzstack";

            try
            {
                byte[] bytes = stack.ToBytes();
                string b64 = Convert.ToBase64String(bytes);
                typesTree.SetString(key, b64);
            }
            catch
            {
                
            }
        }

        private static void EnsureDefaultArlTextureTokens(ITreeAttribute typesTree)
        {
            if (typesTree == null) return;

            static bool IsBad(string s)
            {
                if (string.IsNullOrEmpty(s)) return true;
                if (s.IndexOf('{') >= 0 || s.IndexOf('}') >= 0) return true; 
                return false;
            }

            string bedRaw = typesTree.GetString("bedrolltex", null);
            string bed = RuckSackTextureTokens.NormalizeTextureBaseToken(bedRaw);
            if (IsBad(bed)) bed = RuckSackTextureTokens.BedrollTextureDefaultToken;
            if (!string.Equals(bedRaw, bed, StringComparison.Ordinal)) typesTree.SetString("bedrolltex", bed);

            string qtzRaw = typesTree.GetString("quartztex", null);
            string qtz = RuckSackTextureTokens.NormalizeTextureBaseToken(qtzRaw);
            if (IsBad(qtz)) qtz = RuckSackTextureTokens.QuartzTextureDefaultToken;
            if (!string.Equals(qtzRaw, qtz, StringComparison.Ordinal)) typesTree.SetString("quartztex", qtz);
        }
    }
}
