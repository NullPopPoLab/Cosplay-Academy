using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0162

namespace CosplayParty.Hair
{
    using AccessorySource = HairSupport.HairAccessoryInfo;
    using CoordSource = Dictionary<int, HairSupport.HairAccessoryInfo>;
    using CharaSource = Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>>;

    public class Common
    {
        public const string PluginName = "HairAccessoryCustomizer";
        public const string ExtendedDataName = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public const string CharaDataName = "HairAccessories";
        public const string CoordDataName = "CoordinateHairAccessories";
#if KK
        public const string ControllerName = "KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KK_HairAccessoryCustomizer";
#elif KKS
        public const string ControllerName = "KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KKS_HairAccessoryCustomizer";
#endif

    }

    public class BaseProps
    {
        public readonly string Caption;

        public BaseProps(string caption)
        {
            Caption = caption;
        }

        public static bool IsSupportedVersion(int ver)
        {
            return ver == 0;
        }
    }

    public class AccessoryProps : BaseProps
    {
        public AccessorySource Source;

        public AccessoryProps(string caption)
            : base(caption)
        {
            Source = new AccessorySource();
        }

        public AccessoryProps(AccessorySource src, string caption)
            : base(caption)
        {
            Source = src;
        }
    }

    public class CoordProps : BaseProps
    {
        public PluginControl.CoordData Keeper { get; private set; }
        public Dictionary<int, AccessoryProps> Accessory = new Dictionary<int, AccessoryProps>();

        //! for chara internal coord 
        public CoordProps(ChaFileControl chaFile, int idx)
            : base(chaFile.parameter.fullname + "-" + idx)
        {
            Settings.Logger.LogDebug($"{Common.PluginName}.CoordProps({Caption})");

            Keeper = new PluginControl.CoordData(chaFile.coordinate[idx], Common.ExtendedDataName);
        }

        //! from coord card 
        public CoordProps(ChaFileCoordinate coordFile, string caption = "")
            : base((caption != "") ? caption : (coordFile == null) ? "(empty)" : coordFile.coordinateName)
        {
            if (coordFile == null)
            {
                Settings.Logger.LogWarning($"{Common.PluginName}.CoordProps(empty)");
                return;
            }

            Keeper = new PluginControl.CoordData(coordFile, Common.ExtendedDataName);
            if (!Keeper.IsLoaded)
            {
                if (Settings.Dump_Hair_Load) Settings.Logger.LogDebug($"has no {Common.PluginName} props");
                return;
            }
            if (!IsSupportedVersion(Keeper.Version))
            {
                ClothingLoader.OutdatedMessage($"{Common.PluginName} PluginData", true);
                return;
            }

            var src = Keeper.Read<CoordSource>(Common.CoordDataName);
            if (src == null) return;

            Load(src);
        }

        internal void Load(CoordSource src)
        {
            Accessory.Clear();

            foreach (var t in src)
            {
                Accessory[t.Key] = new AccessoryProps(t.Value, Caption + "-" + t.Key);
            }
        }

        //! get by an accessory 
        public AccessoryProps GetAccessoryProps(int idx, bool force)
        {
            Accessory.TryGetValue(idx, out var ret);
            if (force && ret == null)
            {
                Accessory[idx] = ret = new AccessoryProps(Caption + "-" + idx);
            }
            return ret;
        }

        //! remove accessory properties 
        public void RemoveAccessoryProps(int idx)
        {
            var prop = GetAccessoryProps(idx, false);
            if (prop == null) return;
            if (Settings.Dump_Hair_Merge) Settings.Logger.LogDebug($"RemoveAccessoryProps({idx})");
            Accessory.Remove(idx);
        }

        //! replace accessory properties 
        public void SetAccessoryProps(int? coord, int idx, AccessoryProps src)
        {
            RemoveAccessoryProps(idx);

            if (src == null) return;
            if (Settings.Dump_Hair_Merge) Settings.Logger.LogDebug($"SetAccessoryProps({coord},{idx})");
            var prop = GetAccessoryProps(idx, true);
            prop.Source = src.Source;
        }

        public Dictionary<int, AccessorySource> Pack()
        {
            var dst = new Dictionary<int, AccessorySource>();
            foreach (var t in Accessory)
            {
                dst[t.Key] = t.Value.Source;
            }
            return dst;
        }

        public void Save()
        {
            var img = Pack();
            if (img.Count < 1) Keeper.Remove(Common.CoordDataName);
            else Keeper.Write(Common.CoordDataName, img);
            Keeper.Save();
        }
    }

    public class CharaProps : BaseProps
    {
        public PluginControl.CharaData Keeper { get; private set; }
        public List<CoordProps> Coord;

        //! from chara card 
        public CharaProps(ChaFileControl chaFile, string caption = "")
            : base((caption != null) ? caption : chaFile.parameter.fullname)
        {
            Settings.Logger.LogDebug($"{Common.PluginName}.CharaProps({Caption})");

            Coord = new List<CoordProps>();
            for (var i = 0; i < chaFile.coordinate.Length; ++i)
            {
                Coord.Add(new CoordProps(chaFile, i));
            }

            Keeper = new PluginControl.CharaData(chaFile, Common.ExtendedDataName);
            if (!Keeper.IsLoaded)
            {
                if (Settings.Dump_Hair_Load) Settings.Logger.LogDebug($"has no {Common.PluginName} props");
                return;
            }
            if (!IsSupportedVersion(Keeper.Version))
            {
                ClothingLoader.OutdatedMessage($"{Common.PluginName} PluginData", true);
                return;
            }

            var src = Keeper.Read<CharaSource>(Common.CharaDataName);
            if (src == null) return;

            Load(src);
        }

        internal void Load(CharaSource src)
        {
            for (var i = 0; i < Coord.Count; ++i)
            {
                if (!src.TryGetValue(i, out var sub) || sub == null) continue;

                Coord[i].Load(sub);
            }
        }

        public CharaSource Pack()
        {
            var dst = new CharaSource();
            for (var i = 0; i < Coord.Count; ++i)
            {
                var sub = Coord[i].Pack();
                if (sub.Count > 0) dst[i] = sub;
            }
            return dst;
        }

        public void Save()
        {
            var img = Pack();
            if (img.Count < 1) Keeper.Remove(Common.CharaDataName);
            else Keeper.Write(Common.CharaDataName, img);
            Keeper.Save();
        }
    }

    #region Stuff Hair Accessories needs
    public class HairSupport
    {
        [Serializable]
        [MessagePackObject]
        public class HairAccessoryInfo
        {
            [Key("HairGloss")]
            public bool HairGloss;
            [Key("ColorMatch")]
            public bool ColorMatch;
            [Key("OutlineColor")]
            public Color OutlineColor;
            [Key("AccessoryColor")]
            public Color AccessoryColor;
            [Key("HairLength")]
            public float HairLength;
        }
        #endregion
    }
}
