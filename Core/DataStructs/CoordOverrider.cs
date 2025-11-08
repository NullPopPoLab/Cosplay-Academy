using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using KKAPI.Maker;

namespace CosplayParty
{
    public class OverridingBase : IDisposable
    {
        private ChaDefault ThisOutfitData;
        private int Index;

        public bool IsLoaded;
        public bool IsReady { get { return Selected == null || IsLoaded; } }

        public CardData Selected;

        public OverridingBase(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;
        }

        public virtual void Dispose()
        {
            Unload();
            ThisOutfitData = null;
        }

        public virtual void Unload()
        {
            if (!IsLoaded) return;
            IsLoaded = false;
        }

        public void Load(string path)
        {
            if (String.IsNullOrEmpty(path)) Selected = null;
            else if (!path.EndsWith(".png")) Selected = null;
            else
            {
                var ThisCoordinate = ThisOutfitData.ChaControl.chaFile.coordinate[Index];
                IsLoaded = ThisCoordinate.LoadFile(path);//in case it fails
            }
        }
    }

    public class ModifiedAccessory
    {
        public ChaFileAccessory.PartsInfo Parts;
        public MaterialEditorProperties Material;
        public HairSupport.HairAccessoryInfo Hair;
    }
    public class ModifiedCoord
    {
        public Dictionary<int, ModifiedAccessory> Accessory = new Dictionary<int, ModifiedAccessory>();
    }

    public class OverridingOuter : OverridingBase
    {
        public OverridingOuter(ChaDefault tod, int idx) :
            base(tod, idx)
        {
        }

        public void Select()
        {
            Unload();

            if (Selected == null) return;
            Load(Selected.GetFullPath());
        }
    }

    public class OverridingInner : OverridingBase
    {
        public OverridingInner(ChaDefault tod, int idx) :
            base(tod, idx)
        {
        }

        public void Select()
        {
            Unload();

        }
	}

    public class CoordOverrider
    {
        public ChaDefault Target;
        public int Index;

        private ModifiedCoord _modify = new ModifiedCoord();

        public readonly CoordinateProcessInfo ProcInfo = new CoordinateProcessInfo();

        public CoordOverrider(ChaDefault target, int idx)
        {
            Target = target;
            Index = idx;
        }

        public void Collaborate(CoordinateSuccession succession, ChaFileCoordinate outer, ChaFileCoordinate inner) {

            // 現在のコーデ側 
            var OriginalData = Target.ChaControl.nowCoordinate.accessory.parts.ToList();

            // ロードしたコーデ側 
            var MaterialEditorData = ExtendedSave.GetExtendedDataById(outer, "com.deathweasel.bepinex.materialeditor");
            var Coordinate_ME_Data = new ME_Coordinate(MaterialEditorData, Target, Index);
            var HairData = new Dictionary<int, HairSupport.HairAccessoryInfo>();
            var Inputdata = ExtendedSave.GetExtendedDataById(outer, "com.deathweasel.bepinex.hairaccessorycustomizer");
            if (Inputdata != null)
                if (Inputdata.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                    HairData = MessagePackSerializer.Deserialize<Dictionary<int, HairSupport.HairAccessoryInfo>>((byte[])loadedHairAccessories);

            var newacce = AccessoryInfo.Build(outer, HairData, Coordinate_ME_Data);
            var keptacce = succession.KeptAccessories;

            // 髪型色にするアクセリスト 
            var HairToColor = new List<int>();
            if (Settings.HairMatch.Value && Settings.DestinationHeadAccs.Value && !MakerAPI.InsideMaker)
            {
                // 新コーデにあるアクセのうち、髪型扱いにするパーツのみ対象とする 
                for (var i = 0; i < newacce.Count; ++i)
                {
                    var acce = newacce[i];

                    if (acce.Type != AccessoryType.HairStyle) continue;
                    HairToColor.Add(i);
                }
            }

            var have_hat = false;
            var have_grasses = false;
            var have_pias = false;
            var have_mask = false;
            var have_ears = false;
            var have_tail = false;
            var have_wing = false;

            // ロードを適用するアクセのみ適用 
            var aidx = 0;
            for (var i = 0; i < newacce.Count; ++i)
            {
                var acce = newacce[i];
                var modify = new ModifiedAccessory();
                var use = false;

                switch (acce.Type)
                {
                    case AccessoryType.Hairfit:
                        // 今のところ常に適用しない 
                        break;

                    case AccessoryType.HairStyle:
                    case AccessoryType.Ahoge:
                        // 今のところ常に適用しない 
#if false // 髪型色適用想定 
                if (acce.Hair!=null)
                {
                    var HairInfo = acce.Hair;
                    if (Settings.HairMatch.Value)
                    {
                        HairInfo.ColorMatch = true;
                        HairMatchProcess(outfitnum, ACCpostion, haircolor, parts);
                    }
                    //Current.HairAccessories[loc] = HairInfo;
               }
#endif
                        break;

                    case AccessoryType.Hat:
                        // 適用する
                        // 元のアホ毛は除外 
                        use = true;
                        have_hat = true;
                        break;

                    case AccessoryType.Glasses:
                        // 適用する
                        // 元の眼鏡は除外 
                        use = true;
                        have_grasses = true;
                        break;

                    case AccessoryType.Pias:
                        // 適用する
                        // 元のピアスは除外 
                        use = true;
                        have_pias = true;
                        break;

                    case AccessoryType.Mask:
                        // 適用する
                        // 元のマスクは除外 
                        use = true;
                        have_mask = true;
                        break;

                    default:
                        // 適用する
                        /*! @todo 下着付属アクセは除外
                        */
                        use = true;
                        break;
                }

                var type = acce.IsEmpty ? "Empty" : acce.Type.ToString();
                if (use)
                {
                    Settings.Logger.LogDebug($"New Accessory {i + 1} (as {type}) allowed; " + acce.Parts.id);

                    modify.Parts = acce.Parts;
                }
                else
                {
                    Settings.Logger.LogDebug($"New Accessory {i + 1} (as {type}) denied; " + acce.Parts.id);

                    // 空にする 
                    acce.Parts.type = 120;
                    modify.Parts = acce.Parts;
                }

                modify.Hair = acce.Hair;
                modify.Material = acce.Material;
                _modify.Accessory[aidx++] = modify;
            }

            // 受け継ぐアクセ 
            for (var i = 0; i < keptacce.Count; ++i)
            {
                var acce = keptacce[i];
                var modify = new ModifiedAccessory();
                var use = false;

                switch (acce.Type)
                {
                    case AccessoryType.HairStyle:
                        // 適用する 
                        /*! @todo ShadowColor要修復 */
                        use = true;
                        break;

                    case AccessoryType.Ahoge:
                        // 帽子がないとき適用 
                        /*! @todo ShadowColor要修復 */
                        if (!have_hat) use = true;
                        break;

                    case AccessoryType.Glasses:
                        // 眼鏡がないとき適用 
                        if (!have_grasses) use = true;
                        break;

                    case AccessoryType.Pias:
                        // ピアスがないとき適用 
                        if (!have_pias) use = true;
                        break;

                    case AccessoryType.Mask:
                        // マスクがないとき適用 
                        if (!have_mask) use = true;
                        break;

                    case AccessoryType.Ears:
                        // マスクがないとき適用 
                        if (!have_ears) use = true;
                        break;

                    case AccessoryType.Tail:
                        // マスクがないとき適用 
                        if (!have_tail) use = true;
                        break;

                    case AccessoryType.Wing:
                        // マスクがないとき適用 
                        if (!have_wing) use = true;
                        break;

                    default:
                        // 適用する
                        use = true;
                        break;
                }

                var type = acce.IsEmpty ? "Empty" : acce.Type.ToString();
                if (use)
                {
                    Settings.Logger.LogDebug($"Kept Accessory {aidx + 1} (as {type}) allowed; " + acce.Parts.id);

                    ProcInfo.ACCKeepReturn.Add(aidx);

                    modify.Parts = acce.Parts;
                    modify.Hair = acce.Hair;
                    modify.Material = acce.Material;
                    _modify.Accessory[aidx++] = modify;
                }
                else
                {
                    Settings.Logger.LogDebug($"Kept Accessory {aidx + 1} (as {type}) denied; " + acce.Parts.id);
                }
            }
        }

        public void Apply()
        {
            // データ適用動作 
            var MaterialEditorData = ExtendedSave.GetExtendedDataById(Target.Chafile, "com.deathweasel.bepinex.materialeditor");
            var Coordinate_ME_Data = new ME_Coordinate(MaterialEditorData, Target, Index);
            var HairData = new Dictionary<int, HairSupport.HairAccessoryInfo>();
            var OriginalData = Target.ChaControl.nowCoordinate.accessory.parts.ToList();

            foreach (var acce in _modify.Accessory)
            {
                if (acce.Value.Parts != null)
                {
                    if (acce.Key < OriginalData.Count) OriginalData[acce.Key] = acce.Value.Parts;
                    else
                    {
                        while (acce.Key > OriginalData.Count) OriginalData.Add(null);
                        OriginalData.Add(acce.Value.Parts);
                    }
                }
                HairData[acce.Key] = (acce.Value.Hair != null) ?
                    acce.Value.Hair :
                         new HairSupport.HairAccessoryInfo
                         {
                             HairLength = -999
                         };

                if (acce.Value.Material!=null) Coordinate_ME_Data.AddAccessory(Index, acce.Key, acce.Value.Material);
            }

            Target.ChaControl.nowCoordinate.accessory.parts = OriginalData.ToArray();
            MoreAccessoriesKOI.MoreAccessories.ArraySync(Target.ChaControl);

#region Pack
            var SaveData = new PluginData();

            Coordinate_ME_Data.AllProperties(out var rendererProperties, out var materialFloatProperties, out var materialColorProperties, out var materialShaders, out var materialTextureProperties);

            var TextureDictionary = Target.ME.TextureDictionary.Where(pair => materialTextureProperties.Any(x => x.TexID == pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value.Data);
            if (TextureDictionary.Count > 0)
                SaveData.data.Add("TextureDictionary", MessagePackSerializer.Serialize(TextureDictionary));
            else
                SaveData.data.Add("TextureDictionary", null);

            if (rendererProperties.Count > 0)
                SaveData.data.Add("RendererPropertyList", MessagePackSerializer.Serialize(rendererProperties));
            else
                SaveData.data.Add("RendererPropertyList", null);

            if (materialFloatProperties.Count > 0)
                SaveData.data.Add("MaterialFloatPropertyList", MessagePackSerializer.Serialize(materialFloatProperties));
            else
                SaveData.data.Add("MaterialFloatPropertyList", null);

            if (materialColorProperties.Count > 0)
                SaveData.data.Add("MaterialColorPropertyList", MessagePackSerializer.Serialize(materialColorProperties));
            else
                SaveData.data.Add("MaterialColorPropertyList", null);

            if (materialTextureProperties.Count > 0)
                SaveData.data.Add("MaterialTexturePropertyList", MessagePackSerializer.Serialize(materialTextureProperties));
            else
                SaveData.data.Add("MaterialTexturePropertyList", null);

            if (materialShaders.Count > 0)
                SaveData.data.Add("MaterialShaderList", MessagePackSerializer.Serialize(materialShaders));
            else
                SaveData.data.Add("MaterialShaderList", null);

            ExtendedSave.SetExtendedDataById(Target.Chafile, "com.deathweasel.bepinex.materialeditor", SaveData);

#if false // Additional_Card_Info 廃止予定 
            if (InsideMaker && Constants.PluginResults["Additional_Card_Info"])
            {
                SaveData = new PluginData() { version = 1 };

                var NowCoordinateInfo = new Additional_Card_Info.CoordinateInfo();
                var NowRestrictionInfo = NowCoordinateInfo.RestrictionInfo;

                Inputdata = ExtendedSave.GetExtendedDataById(coordinate, "Additional_Card_Info");
                if (Inputdata != null)
                {
                    switch (Inputdata.version)
                    {
                        case 0:
                            {
                                NowCoordinateInfo = Additional_Card_Info.Migrator.CoordinateMigrateV0(Inputdata);
                                NowRestrictionInfo = NowCoordinateInfo.RestrictionInfo;
                            }
                            break;
                        case 1:
                            if (Inputdata.data.TryGetValue("CoordinateInfo", out var ByteData) && ByteData != null)
                            {
                                NowCoordinateInfo = MessagePackSerializer.Deserialize<Additional_Card_Info.CoordinateInfo>((byte[])ByteData);
                                NowRestrictionInfo = NowCoordinateInfo.RestrictionInfo;
                            }
                            if (Inputdata.data.TryGetValue("RestrictionInfo", out ByteData) && ByteData != null)
                            {
                                NowRestrictionInfo = MessagePackSerializer.Deserialize<Additional_Card_Info.RestrictionInfo>((byte[])ByteData);
                            }
                            break;
                        default:
                            Settings.Logger.LogWarning("New Version Detected Please Update");
                            return;
                    }
                }

                NowCoordinateInfo.AccKeep.AddRange(ACCKeepResult);
                NowCoordinateInfo.HairAcc.AddRange(HairKeepResult);

                SaveData.data.Add("CoordinateInfo", MessagePackSerializer.Serialize(NowCoordinateInfo));
                SaveData.data.Add("RestrictionInfo", MessagePackSerializer.Serialize(NowRestrictionInfo));

                ExtendedSave.SetExtendedDataById(coordinate, "Additional_Card_Info", SaveData);
                //ControllerCoordReload_Loop(Type.GetType("Additional_Card_Info.CharaEvent, Additional_Card_Info", false), ChaControl, coordinate);
            }
#endif

#endregion

            //ControllerCoordReload_Loop(typeof(KK_Plugins.MaterialEditor.MaterialEditorCharaController), ChaControl, coordinate);

            if (Settings.HairMatch.Value)
            {
                var PlugData = new PluginData();

                PlugData.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(HairData));
                ExtendedSave.SetExtendedDataById(Target.Chafile, "com.deathweasel.bepinex.hairaccessorycustomizer", PlugData);

                //ControllerCoordReload_Loop(Type.GetType("KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KK_HairAccessoryCustomizer", false), ChaControl, coordinate);
            }
        }
    }
}
