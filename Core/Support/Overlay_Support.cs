using MessagePack;

namespace CosplayParty.Support
{
    public static class KCOX_Version
    {
        public const int Low = 1;
        public const int High = 2;

#if KKS
        public const int Save = 2;
#else
        public const int Save = 1;
#endif

        public static bool IsAvailable(int ver)
        {
            if (ver < Low) return false;
            if (ver > High) return false;
            return true;
        }
    }

#region Stuff KCOX_RePack Needs


    [MessagePackObject]
    public class ClothesTexData
    {
        [Key(0)]
        public byte[] TextureBytes;

        [Key(1)]
        public bool Override;

#if KKS
        [Key(2)]
        public OverlayBlendingMode BlendingMode;
#endif
    }

    public enum OverlayBlendingMode
    {
        Default = 0,
        LinearAlpha = 1,
    }

#endregion
}
