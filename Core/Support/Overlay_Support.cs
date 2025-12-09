using System;
using System.Collections.Generic;
using ExtensibleSaveFormat;
using MessagePack;

namespace CosplayParty.KCOX
{
    using CoordOverlaySource = Dictionary<string, ClothesTexData>;
    using CoordTexSizeSource = Dictionary<string, int>;
    using CharaOverlaySource = Dictionary<int, Dictionary<string, ClothesTexData>>;
    using CharaTexSizeSource = Dictionary<int, Dictionary<string, int>>;

    public class Common
    {
        public const string PluginName = "OverlayMods";
        public const string ExtendedDataName = "KCOX";
        public const string OverlayDataName = "Overlays";
        public const string TexSizeDataName = "TextureSizeOverride";
#if KK
        public const string ControllerName = "KoiClothesOverlayX.KoiClothesOverlayController, KK_OverlayMods";
#elif KKS
        public const string ControllerName = "KoiClothesOverlayX.KoiClothesOverlayController, KKS_OverlayMods";
#endif

        public const int LowVersion = 1;
        public const int HighVersion = 2;
        public const int SaveVersion = 2;

        public bool Dump_Load { get { return Settings.Dump_KCOX_Load; } }
        public bool Dump_Save { get { return Settings.Dump_KCOX_Save; } }
        public bool Dump_Merge { get { return Settings.Dump_KCOX_Merge; } }

        public static bool IsSupportedVersion(int ver)
        {
            if (ver < LowVersion) return false;
            if (ver > HighVersion) return false;
            return true;
        }

        public static readonly string[] SlotNames =
        {
            "ct_clothesTop",
            "ct_clothesBot",
            "ct_bra",
            "ct_shorts",
            "ct_gloves",
            "ct_panst",
            "ct_socks",
            "ct_shoes_inner",
            "ct_shoes_outer",
        };

        public static readonly Dictionary<string, int> SlotLookup = new Dictionary<string, int>()
        {
            ["ct_clothesTop"] = 0,
            ["ct_top_parts_A"] = 0,
            ["ct_top_parts_B"] = 0,
            ["ct_clothesBot"] = 1,
            ["ct_bra"] = 2,
            ["ct_shorts"] = 3,
            ["ct_gloves"] = 4,
            ["ct_panst"] = 5,
            ["ct_socks"] = 6,
            ["ct_shoes_inner"] = 7,
            ["ct_shoes_outer"] = 8,
        };

        public static int Name2Slot(string name)
        {
            var ctp = name.IndexOf("ct_");
            if(ctp<0)return -1; // by coord 

            // specified cloth slot 
            if (SlotLookup.TryGetValue(name.Substring(ctp), out var slot)) return slot;

            // unknown name
            Settings.Logger.LogWarning($"Unknown overlay texture name: {name}");

            return -1;
        }
    }

    public class ClothSource
    {
        public CoordOverlaySource Overlay;
        public int? TexSize;
    }

    public class CoordSource
    {
        public CoordOverlaySource Overlay;
        public CoordTexSizeSource TexSize;

        public static CoordSource CreateEmpty()
        {
            var t = new CoordSource();
            t.Overlay = new CoordOverlaySource();
            t.TexSize = new CoordTexSizeSource();
            return t;
        }
    }

    public class CharaSource
    {
        public CharaOverlaySource Overlay;
        public CharaTexSizeSource TexSize;
    }

    public class BaseProps: Common
    {
        public readonly string Caption;

        public BaseProps(string caption)
        {
            Caption = caption;
        }
    }

    public class ClothProps : BaseProps
    {
        public CoordOverlaySource Overlay = new CoordOverlaySource();
        public int? TexSize;

        public ClothProps(string caption)
            : base(caption)
        {
        }

        public ClothProps(ClothSource src, string caption)
            : base(caption)
        {
            Load(src);
        }

        internal void Load(ClothSource src)
        {
            Overlay = src.Overlay;
            TexSize = src.TexSize;
        }
    }

    public class CoordProps : BaseProps
    {
        public PluginControl.CoordPlugKeeper Keeper { get; private set; }
        public Dictionary<int, ClothProps> Cloth = new Dictionary<int, ClothProps>();
        public CoordOverlaySource Overlay = new CoordOverlaySource();

        //! for chara internal coord 
        public CoordProps(ChaFileControl chaFile, int idx)
            : base(chaFile.parameter.fullname + "-" + idx)
        {
            Settings.Logger.LogDebug($"{PluginName}.CoordProps({Caption})");

            Keeper = new PluginControl.CoordPlugKeeper(chaFile.coordinate[idx], ExtendedDataName);
        }

        //! from coord card 
        public CoordProps(ChaFileCoordinate coordFile, string caption = "")
            : base((caption != "") ? caption : (coordFile == null) ? "(empty)" : coordFile.coordinateName)
        {
            if (coordFile == null)
            {
                Settings.Logger.LogWarning($"{PluginName}.CoordProps(empty)");
                return;
            }

            Keeper = new PluginControl.CoordPlugKeeper(coordFile, ExtendedDataName);
            if (!Keeper.IsLoaded)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"has no {PluginName} props");
                return;
            }
            if (!IsSupportedVersion(Keeper.Version))
            {
                ClothingLoader.OutdatedMessage($"{PluginName} PluginData", true);
                return;
            }

            var src = new CoordSource();
            src.Overlay = Keeper.Read<CoordOverlaySource>(OverlayDataName);
            src.TexSize = Keeper.Read<CoordTexSizeSource>(TexSizeDataName);
            Load(src);
        }

        internal void Load(CoordSource src)
        {
            Cloth.Clear();

            if (src.Overlay == null) { }
            else foreach (var t in src.Overlay)
                {
                    Settings.Logger.LogDebug($"KCOX Overlay for {Caption}: {t.Key}={t.Value}");

                    int slot = Name2Slot(t.Key);
                    if (slot < -1)
                    {
                        Overlay[t.Key] = t.Value;
                    }
                    else
                    {
                        var dst = GetClothProps(slot, true);
                        dst.Overlay[t.Key] = t.Value;
                    }
                }

            if (src.TexSize == null) { }
            else foreach (var t in src.TexSize)
                {
                    Settings.Logger.LogDebug($"KCOX TexSize for {Caption}: {t.Key}={t.Value}");

                    if (!SlotLookup.TryGetValue(t.Key, out var slot)) continue;

                    var dst = GetClothProps(slot, true);
                    dst.TexSize = t.Value;
                }
        }

        //! get by a cloth  
        public ClothProps GetClothProps(int idx, bool force)
        {
            Cloth.TryGetValue(idx, out var ret);
            if (force && ret == null)
            {
                Cloth[idx] = ret = new ClothProps(Caption + "-" + idx);
            }
            return ret;
        }

        //! remove cloth properties 
        public void RemoveClothProps(int idx)
        {
            var prop = GetClothProps(idx, false);
            if (prop == null) return;
            Cloth.Remove(idx);
        }

        //! replace cloth properties 
        public void SetClothProps(int? coord, int idx, ClothProps src)
        {
            RemoveClothProps(idx);

            if (src == null) return;
            if (Dump_Merge) Settings.Logger.LogDebug($"SetClothProps({coord},{idx})");
            var prop = GetClothProps(idx, true);
            prop.Overlay = src.Overlay;
            prop.TexSize = src.TexSize;
        }

        public CoordSource Pack()
        {
            var dst = CoordSource.CreateEmpty();

            foreach (var t in Overlay)
            {
                dst.Overlay[t.Key] = t.Value;
            }

            foreach (var t in Cloth)
            {
                if (t.Value.TexSize == null) continue;

                dst.TexSize[SlotNames[t.Key]] = t.Value.TexSize.Value;
            }

            return dst;
        }

        public void Save()
        {
            if (Keeper.Target == null)
            {
                Settings.Logger.LogWarning($"no target to save {PluginName} props for {Caption}");
                return;
            }

            Settings.Logger.LogDebug($"Save {PluginName} Props for {Caption}");
            var img = Pack();

            if (img.Overlay.Count > 0) Keeper.Write(OverlayDataName, img.Overlay);
            else Keeper.Remove(OverlayDataName);
            if(img.TexSize.Count>0)Keeper.Write(TexSizeDataName, img.TexSize);
            else Keeper.Remove(TexSizeDataName);
            Keeper.Save();
        }
    }

    public class CharaProps : BaseProps
    {
        public PluginControl.CharaPlugKeeper Keeper { get; private set; }
        public List<CoordProps> Coord;

        //! from chara card 
        public CharaProps(ChaFileControl chaFile, string caption = "")
            : base((caption != null) ? caption : chaFile.parameter.fullname)
        {
            Settings.Logger.LogDebug($"{PluginName}.CharaProps({Caption})");

            Coord = new List<CoordProps>();
            for (var i = 0; i < chaFile.coordinate.Length; ++i)
            {
                Coord.Add(new CoordProps(chaFile, i));
            }

            Keeper = new PluginControl.CharaPlugKeeper(chaFile, ExtendedDataName);
            if (!Keeper.IsLoaded)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"has no {PluginName} props");
                return;
            }
            if (!IsSupportedVersion(Keeper.Version))
            {
                ClothingLoader.OutdatedMessage($"{PluginName} PluginData", true);
                return;
            }

            var src = new CharaSource();
            src.Overlay = Keeper.Read<CharaOverlaySource>(OverlayDataName);
            src.TexSize = Keeper.Read<CharaTexSizeSource>(TexSizeDataName);
            Load(src);
        }

        internal void Load(CharaSource src)
        {
            for (var i = 0; i < Coord.Count; ++i)
            {
                var sub = new CoordSource();
                src.Overlay?.TryGetValue(i, out sub.Overlay);
                src.TexSize?.TryGetValue(i, out sub.TexSize);

                Coord[i].Load(sub);
            }
        }

        public CharaSource Pack()
        {
            var dst = new CharaSource();
            dst.Overlay = new CharaOverlaySource();
            dst.TexSize = new CharaTexSizeSource();

            for (var i = 0; i < Coord.Count; ++i)
            {
                var sub = Coord[i].Pack();
                if (sub.Overlay.Count > 0) dst.Overlay[i] = sub.Overlay;
                if (sub.TexSize.Count > 0) dst.TexSize[i] = sub.TexSize;
            }
            return dst;
        }

        public void Save()
        {
            if (Keeper.Target == null)
            {
                Settings.Logger.LogWarning($"no target to save {PluginName} props for {Caption}");
                return;
            }

            Settings.Logger.LogDebug($"Save {PluginName} Props for {Caption}");
            var img = Pack();

            if (img.Overlay.Count > 0) Keeper.Write(OverlayDataName, img.Overlay);
            else Keeper.Remove(OverlayDataName);
            if (img.TexSize.Count > 0) Keeper.Write(TexSizeDataName, img.TexSize);
            else Keeper.Remove(TexSizeDataName);
            Keeper.Save();
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

        [Key(2)]
        public OverlayBlendingMode BlendingMode;
    }

    public enum OverlayBlendingMode
    {
        Default = 0,
        LinearAlpha = 1,
    }

#endregion
}
