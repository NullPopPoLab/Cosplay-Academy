using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosplayParty.Hair
{
    public class Common
    {
#if KK
        public const string ControllerName = "KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KK_HairAccessoryCustomizer";
#elif KKS
        public const string ControllerName = "KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KKS_HairAccessoryCustomizer";
#endif
        public const string ExtendedDataName = "com.deathweasel.bepinex.hairaccessorycustomizer";
    }

    public class BaseProps
    {
        public readonly string Caption;

        public BaseProps(string caption)
        {
            Caption = caption;
        }
    }

    public class AccessoryProps : BaseProps
    {
        public HairSupport.HairAccessoryInfo Source;

        public AccessoryProps(string caption)
            : base(caption)
        {
            Source = new HairSupport.HairAccessoryInfo();
        }

        public AccessoryProps(HairSupport.HairAccessoryInfo src, string caption)
            : base(caption)
        {
            Source = src;
        }
    }

    public class CoordProps : BaseProps
    {
        public ChaFileCoordinate Source { get; private set; }
        public Dictionary<int, AccessoryProps> Accessory = new Dictionary<int, AccessoryProps>();

        //! for chara internal coord 
        public CoordProps(ChaFileControl chaFile, int idx)
            : base(chaFile.parameter.fullname + "-" + idx)
        {
            Settings.Logger.LogDebug($"Hair.CoordProps({Caption})");
            Source = chaFile.coordinate[idx];
        }

        //! from coord card 
        public CoordProps(ChaFileCoordinate coordFile, string caption = "")
            : base((caption != "") ? caption : (coordFile == null) ? "(empty)" : coordFile.coordinateName)
        {
            if (coordFile == null)
            {
                Settings.Logger.LogWarning($"Hair.CoordProps(empty)");
                return;
            }

            var data = ExtendedSave.GetExtendedDataById(coordFile, Common.ExtendedDataName);
            if (data == null)
            {
                if (Settings.Dump_Hair_Load) Settings.Logger.LogDebug($"has no HairAccessoryCustomizer props");
                return;
            }
            if (data.version != 0)
            {
                ClothingLoader.OutdatedMessage("HairAccessoryCustomizer PluginData", true);
                return;
            }

            if (!data.data.TryGetValue("CoordinateHairAccessories", out var img) || img == null)
            {
                if (Settings.Dump_Hair_Load) Settings.Logger.LogDebug("invalid HairAccessoryCustomizer props");
                return;
            }

            var src = MessagePackSerializer.Deserialize<Dictionary<int, HairSupport.HairAccessoryInfo>>((byte[])img);
            Load(src);
        }

        internal void Load(Dictionary<int, HairSupport.HairAccessoryInfo> src)
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

        public Dictionary<int, HairSupport.HairAccessoryInfo> Pack()
        {
            var dst = new Dictionary<int, HairSupport.HairAccessoryInfo>();
            foreach (var t in Accessory)
            {
                dst[t.Key] = t.Value.Source;
            }
            return dst;
        }

        public void Save()
        {
            var img = Pack();
            if (img.Count < 1) img = null;

            var dst = new PluginData();
            dst.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(img));
            ExtendedSave.SetExtendedDataById(Source, Common.ExtendedDataName, dst);
        }
    }

    public class CharaProps : BaseProps
    {
        public List<CoordProps> Coord;
        public ChaFileControl Source { get; private set; }

        //! from chara card 
        public CharaProps(ChaFileControl chaFile, string caption = "")
            : base((caption != null) ? caption : chaFile.parameter.fullname)
        {
            Settings.Logger.LogDebug($"Hair.CharaProps({Caption})");
            Source = chaFile;
            Coord = new List<CoordProps>();
            for (var i = 0; i < chaFile.coordinate.Length; ++i)
            {
                Coord.Add(new CoordProps(chaFile, i));
            }

            var data = ExtendedSave.GetExtendedDataById(chaFile, Common.ExtendedDataName);
            if (data == null)
            {
                if (Settings.Dump_Hair_Load) Settings.Logger.LogDebug("has no HairAccessoryCustomizer props");
                return;
            }
            if (data.version != 0)
            {
                ClothingLoader.OutdatedMessage("HairAccessoryCustomizer PluginData", true);
                return;
            }

            if (!data.data.TryGetValue("HairAccessories", out var img) || img == null)
            {
                if (Settings.Dump_Hair_Load) Settings.Logger.LogDebug("invalid HairAccessoryCustomizer props");
                return;
            }

            var src = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>>>((byte[])img);
            Load(src);
        }

        internal void Load(Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>> src)
        {
            for (var i = 0; i < Coord.Count; ++i)
            {
                if (!src.TryGetValue(i, out var sub) || sub == null) continue;

                Coord[i].Load(sub);
            }
        }

        public Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>> Pack()
        {
            var dst = new Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>>();
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
            if (img.Count < 1) img = null;

            var dst = new PluginData();
            dst.data.Add("HairAccessories", MessagePackSerializer.Serialize(img));
            ExtendedSave.SetExtendedDataById(Source, Common.ExtendedDataName, dst);
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
