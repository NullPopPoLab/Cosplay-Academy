using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0162

namespace CosplayParty.PluginDataEdittingTemplate
{
    using ClothSource = ClothSourceTemplate;
    using AccessorySource= AccessorySourceTemplate;
    using CoordSource = CoordSourceTemplate;
    using CharaSource = CharaSourceTemplate;

    public class Common
    {
        public const string PluginName = "{PluginName}";
        public const string ExtendedDataName = "{DataName}";
        public const string CharaDataName = "{CharaDataName}";
        public const string CoordDataName = "{CoordDataName}";
        public const string ControllerName = "{FuncName}, {PlugFile}";

        public const bool Dump_Load = true;
        public const bool Dump_Save = true;
        public const bool Dump_Merge = true;
    }

    public class ClothSourceTemplate { }
    public class AccessorySourceTemplate { }
    public class CoordSourceTemplate {
        public Dictionary<int, ClothSource> Cloth = new Dictionary<int, ClothSource>();
        public Dictionary<int, AccessorySource> Accessory = new Dictionary<int, AccessorySource>();
    }
    public class CharaSourceTemplate {
        public Dictionary<int, CoordSource> Coord = new Dictionary<int, CoordSource>();
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

    public class ClothProps : BaseProps
    {
        public ClothSource Source;

        public ClothProps(string caption)
            : base(caption)
        {
        }

        public ClothProps(ClothSource src,string caption)
            : base(caption)
        {
            Source = src;
        }
    }

    public class AccessoryProps : BaseProps
    {
        public AccessorySource Source;

        public AccessoryProps(string caption)
            : base(caption)
        {
        }

        public AccessoryProps(AccessorySource src, string caption)
            : base(caption)
        {
            Source = src;
        }
    }

    public class CoordProps : BaseProps
    {
        public ChaFileCoordinate Source { get; private set; }
        public Dictionary<int, ClothProps> Cloth = new Dictionary<int, ClothProps>();
        public Dictionary<int, AccessoryProps> Accessory = new Dictionary<int, AccessoryProps>();

        //! for chara internal coord 
        public CoordProps(ChaFileControl chaFile, int idx)
            : base(chaFile.parameter.fullname + "-" + idx)
        {
            Settings.Logger.LogDebug($"{Common.PluginName}.CoordProps({Caption})");
            Source = chaFile.coordinate[idx];
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

            var data = ExtendedSave.GetExtendedDataById(coordFile, Common.ExtendedDataName);
            if (data == null)
            {
                if (Common.Dump_Load) Settings.Logger.LogDebug($"has no {Common.PluginName} props");
                return;
            }
            if (!IsSupportedVersion(data.version))
            {
                ClothingLoader.OutdatedMessage($"{Common.PluginName} PluginData", true);
                return;
            }

            if (!data.data.TryGetValue(Common.CoordDataName, out var img) || img == null)
            {
                if (Common.Dump_Load) Settings.Logger.LogDebug($"has no {Common.CoordDataName} props");
                return;
            }

            var src = MessagePackSerializer.Deserialize<CoordSource>((byte[])img);
            if (src == null)
            {
                Settings.Logger.LogWarning($"invalid {Common.CoordDataName} props");
                return;
            }

            Load(src);
        }

        internal void Load(CoordSource src)
        {
            Cloth.Clear();
            Accessory.Clear();

            foreach (var t in src.Cloth)
            {
                Cloth[t.Key] = new ClothProps(t.Value, Caption + "-" + t.Key);
            }
            foreach (var t in src.Accessory)
            {
                Accessory[t.Key] = new AccessoryProps(t.Value, Caption + "-" + t.Key);
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

        //! remove cloth properties 
        public void RemoveClothProps(int idx)
        {
            var prop = GetClothProps(idx, false);
            if (prop == null) return;
            Cloth.Remove(idx);
        }

        //! remove accessory properties 
        public void RemoveAccessoryProps(int idx)
        {
            var prop = GetAccessoryProps(idx, false);
            if (prop == null) return;
            Accessory.Remove(idx);
        }

        //! replace cloth properties 
        public void SetClothProps(int? coord, int idx, ClothProps src)
        {
            RemoveClothProps(idx);

            if (src == null) return;
            if (Common.Dump_Merge) Settings.Logger.LogDebug($"SetClothProps({coord},{idx})");
            var prop = GetClothProps(idx, true);
            prop.Source = src.Source;
        }

        //! replace accessory properties 
        public void SetAccessoryProps(int? coord, int idx, AccessoryProps src)
        {
            RemoveAccessoryProps(idx);

            if (src == null) return;
            if (Common.Dump_Merge) Settings.Logger.LogDebug($"SetAccessoryProps({coord},{idx})");
            var prop = GetAccessoryProps(idx, true);
            prop.Source = src.Source;
        }

        public CoordSource Pack()
        {
            var dst = new CoordSource();
            foreach (var t in Cloth)
            {
                dst.Cloth[t.Key] = t.Value.Source;
            }
            foreach (var t in Accessory)
            {
                dst.Accessory[t.Key] = t.Value.Source;
            }
            return dst;
        }

        public void Save()
        {
            var img = Pack();
            if (img == null) img = null;

            var dst = new PluginData();
            dst.data.Add(Common.CoordDataName, MessagePackSerializer.Serialize(img));
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
            Settings.Logger.LogDebug($"{Common.PluginName}.CharaProps({Caption})");
            Source = chaFile;
            Coord = new List<CoordProps>();
            for (var i = 0; i < chaFile.coordinate.Length; ++i)
            {
                Coord.Add(new CoordProps(chaFile, i));
            }

            var data = ExtendedSave.GetExtendedDataById(chaFile, Common.ExtendedDataName);
            if (data == null)
            {
                if (Common.Dump_Load) Settings.Logger.LogDebug($"has no {Common.PluginName} props");
                return;
            }
            if (!IsSupportedVersion(data.version))
            {
                ClothingLoader.OutdatedMessage($"{Common.PluginName} PluginData", true);
                return;
            }

            if (!data.data.TryGetValue(Common.CharaDataName, out var img) || img == null)
            {
                if (Common.Dump_Load) Settings.Logger.LogDebug($"has no {Common.CharaDataName} props");
                return;
            }

            var src = MessagePackSerializer.Deserialize<CharaSource>((byte[])img);
            if (src == null)
            {
                Settings.Logger.LogWarning($"invalid {Common.CharaDataName} props");
                return;
            }

            Load(src);
        }

        internal void Load(CharaSource src)
        {
            for (var i = 0; i < Coord.Count; ++i)
            {
                if (!src.Coord.TryGetValue(i, out var sub) || sub == null) continue;

                Coord[i].Load(sub);
            }
        }

        public CharaSource Pack()
        {
            var dst = new CharaSource();
            for (var i = 0; i < Coord.Count; ++i)
            {
                var sub = Coord[i].Pack();
                if (sub !=null) dst.Coord[i] = sub;
            }
            return dst;
        }

        public void Save()
        {
            var img = Pack();
            if (img == null) img = null;

            var dst = new PluginData();
            dst.data.Add(Common.CharaDataName, MessagePackSerializer.Serialize(img));
            ExtendedSave.SetExtendedDataById(Source, Common.ExtendedDataName, dst);
        }
    }
}
