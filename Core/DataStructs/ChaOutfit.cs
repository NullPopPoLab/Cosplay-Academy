using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CosplayParty
{
    //! コーデ編集情報 
    /*! @note 編集前に Reset() を呼ぶ。
    */
    public class CoordinateProcessInfo : IDisposable
    {
        //! コーデ差し替え後に CoordinatePartsQueue から適用したアクセのインデクスが追記される 
        public readonly List<int> ACCKeepReturn = new List<int>();
        //! コーデ差し替え後に下着付属として追加したアクセのインデクスが追記される 
        public readonly List<int> UnderwearAccessoriesLocations = new List<int>();

        public bool[] UnderwearProcessed = new bool[9];
        public bool[] UnderClothingKeep = new bool[9];

        public void Dispose()
        {
            Reset();
        }

        public void Reset()
        {
            ACCKeepReturn.Clear();
            UnderwearAccessoriesLocations.Clear();
            UnderwearProcessed = new bool[9];
            UnderClothingKeep = new bool[9];
        }
    }

    public class ChaOutfit : IDisposable
    {
        public class ImportSources
        {
            public ChaFile Chafile;
            public ChaFileCoordinate[] Original_Coordinates;
            public Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>> CharaHair;
            public PluginData HairExtendedData;
            public PluginData MaterialEditorData;
            public ME_List FinalMaterials;
        }

        private ChaDefault ThisOutfitData;
        private int Index;

        public readonly CoordInfo Current = new CoordInfo();
        public readonly OverridingOuter Outer;
        public readonly OverridingInner Inner;

        // このあたりの構造、ロード前のコーデ適用なんかもあるのでロードと密連動させてはならない 
        // 用途に応じて適切なタイミングで扱う必要がある。 
        public readonly CoordinateProcessInfo ProcInfo;

        // FirstPass 処理で構築 
        // Clear() でも残す 
        public ChaFileCoordinate Original_Coordinate;
        public Dictionary<int, HairSupport.HairAccessoryInfo> HairInfo;

        public bool MakeUpKeep = false;

        public ChaOutfit(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;

            Outer = new OverridingOuter(tod, idx);
            Inner = new OverridingInner(tod, idx);
            ProcInfo = new CoordinateProcessInfo();
            HairInfo = new Dictionary<int, HairSupport.HairAccessoryInfo>();
        }

        public void Dispose()
        {
            Reset();
            Outer.Dispose();
            Inner.Dispose();
            Current.Dispose();
            ProcInfo.Dispose();
            ThisOutfitData = null;
            HairInfo = null;
        }

        public void Reset()
        {
            // firstpass 時点で内容消去必要あるものを処理
            Current.Reset();
            ProcInfo.Reset();
        }

        private ChaFileCoordinate _cloneCoordinate(ChaFileCoordinate OriginalCoordinate)
        {
            return new ChaFileCoordinate
            {
                clothes = OriginalCoordinate.clothes,
                makeup = OriginalCoordinate.makeup,
                enableMakeup = OriginalCoordinate.enableMakeup,
            }; ;
        }

        public void Import(ImportSources src)
        {
            var step=0;
            try
            {
                Original_Coordinate = _cloneCoordinate(src.Original_Coordinates[Index]);
                step = 1;
                if (src.CharaHair == null)
                {
                    src.CharaHair = new Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>>();
                }
                else if (src.CharaHair.TryGetValue(Index, out HairInfo) == false)
                {
                    HairInfo = new Dictionary<int, HairSupport.HairAccessoryInfo>();
                }
                step = 2;
                if (!src.FinalMaterials.Coordinates.TryGetValue(Index, out var mat))
                {
                    mat = new ME_Coordinate();
                }
                step = 3;
                Current.Import(src.Chafile.coordinate[Index], HairInfo, mat);
            }
            catch (Exception e)
            {
                Settings.Logger.LogError($"ChaOutfit.Import() error at step {step}; " + e);
            }
        }

        public void Override(ChaFileCoordinate outer, ChaFileCoordinate inner)
        {

            var co = new CoordOverrider(ThisOutfitData, Index);
            co.Collaborate(Current.Succession, outer, inner);
            co.Apply();
        }

        public void Collaborate()
        {
            // Queue accessories to keep
            //var UnderClothingKeep = new bool[9];

            //Load new outfit
            //ValidOutfits[outfitnum] = load_outfit;
            if (Outer.IsReady)
            {
                // コーデの読み替えが発生した 
                // アクセの組み換えを要する 
            }
            else
            {
                // 読み替えてないので、アクセの組み換えも無用 
                // インナーの差し替えはあるかもしれない 
            }

            ProcInfo.Reset();

#if false // 全面再検討 
            // 従前コーデの被服マテリアルは放棄 
            //outfit.Current.Material.SoftClear(PersonalClothingBools);
            Current.Material.SoftClear(new bool[Constants.ClothSlots]);

            // ロード済コーデ 
            var ThisCoordinate = Current.Coordinate;

            var Inputdata = ExtendedSave.GetExtendedDataById(ThisCoordinate, "com.deathweasel.bepinex.hairaccessorycustomizer");
            if (Inputdata != null)
            {
                if (Inputdata.version == 0)
                {
                    if (Inputdata.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                        Current.HairAccessories = MessagePackSerializer.Deserialize<Dictionary<int, HairSupport.HairAccessoryInfo>>((byte[])loadedHairAccessories);
                }
                else
                {
                    ClothingLoader.OutdatedMessage("hairaccessorycustomizer", true);
                }
            }

            var MaterialEditorData = ExtendedSave.GetExtendedDataById(ThisCoordinate, "com.deathweasel.bepinex.materialeditor");
            ThisOutfitData.FinalMaterials.LoadCoordinate(MaterialEditorData, ThisOutfitData, Index);
            var FinalMaterials = new ME_List(MaterialEditorData, ThisOutfitData);
            //var Import_ME_Data = new MaterialEditorProperties();
            if (!FinalMaterials.Coordinates.TryGetValue(Index, out Current.Material))
            {
                Current.Material = new ME_Coordinate();
            }

            var newacce = AccessoryInfo.Build(ThisCoordinate, Current.HairAccessories, Current.Material);
            var keptacce = Current.Succession.KeptAccessories;

#if false // 廃止予定 
            if (outfit.MakeUpKeep)
            {
                ThisCoordinate.enableMakeup = outfit.Original_Coordinate.enableMakeup;
                ThisCoordinate.makeup = outfit.Original_Coordinate.makeup;
            }
#endif

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

#if false // Additional_Card_Info 廃止予定 
            var ExpandedData = ExtendedSave.GetExtendedDataById(ThisCoordinate, "Additional_Card_Info");
            if (ExpandedData != null)
            {
                switch (ExpandedData.version)
                {
                    case 0:
                        if (ExpandedData.data.TryGetValue("CoordinateSaveBools", out var bytedata) && bytedata != null)
                        {
                            UnderClothingKeep = MessagePackSerializer.Deserialize<bool[]>((byte[])bytedata);
                        }
                        if (ExpandedData.data.TryGetValue("HairAcc", out bytedata) && bytedata != null)
                        {
                            HairToColor = MessagePackSerializer.Deserialize<List<int>>((byte[])bytedata);
                        }
                        if (ExpandedData.data.TryGetValue("ClothNot", out bytedata) && bytedata != null)
                        {
                            Underwearbools[outfitnum] = MessagePackSerializer.Deserialize<bool[]>((byte[])bytedata);
                        }
                        break;
                    case 1:

                        if (ExpandedData.data.TryGetValue("CoordinateInfo", out bytedata) && bytedata != null)
                        {
                            var coordinfo = MessagePackSerializer.Deserialize<Additional_Card_Info.CoordinateInfo>((byte[])bytedata);
                            UnderClothingKeep = coordinfo.CoordinateSaveBools;
                            HairToColor = coordinfo.HairAcc;
                            Underwearbools[outfitnum] = coordinfo.ClothNotData;
                        }
                        break;
                    default:
                        ClothingLoader.OutdatedMessage("Additional_Card_Info", false);
                        break;
                }
            }
            else
#endif

            //if (UnderClothingKeep == null) UnderClothingKeep = new bool[9];
            //if (HairToColor == null) HairToColor = new List<int>();
            //if (Underwearbools[outfitnum] == null) Underwearbools[outfitnum] = new bool[3];
#if false // Additional_Card_Info 廃止予定 
            for (var i = 0; i < 9; i++)
            {
                if (PersonalClothingBools[i])
                {
                    UnderClothingKeep[i] = true;
                }
            }
#endif
            //outfit.ProcInfo.UnderClothingKeep = UnderClothingKeep;


            var have_grasses = false;
            var have_pias = false;
            var have_mask = false;

            // ロードを適用するアクセのみ適用 
            var newparts = new List<ChaFileAccessory.PartsInfo>();
            for (var i = 0; i < newacce.Count; ++i)
            {
                var acce = newacce[i];
                var use = false;

                switch (acce.Type)
                {
                    case AccessoryType.HairStyle:
                    case AccessoryType.Hairfit:
                        // 常に適用しない 
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

                if (use) newparts.Add(acce.Parts);
                else
                {
                    // 空にする 
                    Current.HairAccessories.Remove(i);
                    acce.Parts.type = 120;
                    newparts.Add(acce.Parts);
                }
            }


            if (Inner.IsLoaded)
            {
#if false // 再検討; 下着可換 
                //var underwearbools = Underwearbools[outfitnum];
                var processed = outfit.Outer.UnderwearProcessed;
                Underwear_ME_Data.ChangeCoord(outfitnum);
                var Local_Underwear_ACC_Info = new List<ChaFileAccessory.PartsInfo>(Underwear_PartsInfos);
                var ObjectTypeList = new List<ObjectType>() { ObjectType.Accessory };
                for (var i = 0; i < Local_Underwear_ACC_Info.Count; i++)
                {
                    if (Local_Underwear_ACC_Info[i].type > 120)
                    {
                        var ACCdata = new HairSupport.HairAccessoryInfo
                        {
                            HairLength = -999
                        };
                        if (Settings.HairMatch.Value)
                        {
                            ACCdata.ColorMatch = true;
                        }
                        HairKeepQueue.Enqueue(false);
                        ACCKeepqueue.Enqueue(false);

                        MaterialEditorProperties editorProperties;
                        if (!Underwear_ME_Data.AccessoryProperties.TryGetValue(i, out editorProperties))
                        {
                            editorProperties = new MaterialEditorProperties();
                        }
                        ME_Queue.Enqueue(editorProperties);
                        PartsQueue.Enqueue(Local_Underwear_ACC_Info[i]);
                        HairQueue.Enqueue(ACCdata);
                    }
                }
                //var forceunder = Settings.ForceRandomUnderwear.Value;

                //When Top is not empty and bra is not kept
                var underclothesparts = Underwear.clothes.parts;
                var clothes_mainsubpart = ThisCoordinate.clothes.subPartsId[0];
                var clothespart = ThisCoordinate.clothes.parts;
                var CharacterClothingKeep_Coordinate = this.CharacterClothingKeep_Coordinate[outfitnum];

                if (/*!Constants.IgnoredTopIDs_Main.Contains(clothespart[0].id) && (!Constants.IgnoredTopIDs_A.TryGetValue(clothespart[0].id, out var list) || !list.Contains(clothes_mainsubpart)) &&*/ !CharacterClothingKeep_Coordinate[2])
                {
                    if (!UnderClothingKeep[2] /*&& !underwearbools[1] && !underwearbools[2]*/ && (clothespart[2].id != 0 /*|| forceunder*/))
                    {
                        processed[2] = true;
                        clothespart[2] = underclothesparts[2];
                        Additional_Clothing_Process(2, outfitnum, Underwear_ME_Data);
                    }

                    //if (underwearbools[0])
                    {
                        if (!UnderClothingKeep[3] /*&& !underwearbools[2]*/ && (clothespart[3].id != 0 /*|| forceunder*/))
                        {
                            processed[3] = true;
                            clothespart[3] = underclothesparts[3];
                            Additional_Clothing_Process(3, outfitnum, Underwear_ME_Data);
                        }
                    }
                }

                //When bot is not empty and underwear is not kept
                if (!Constants.IgnoredBotsIDs_Main.Contains(clothespart[1].id) && !underwearbools[0] && !CharacterClothingKeep_Coordinate[3])
                {
                    if (!UnderClothingKeep[3] && !underwearbools[2] && (clothespart[3].id != 0 || forceunder))
                    {
                        processed[3] = true;
                        clothespart[3] = underclothesparts[3];
                        Additional_Clothing_Process(3, outfitnum, Underwear_ME_Data);
                    }
                }

                if (outfitnum != 3)
                {
                    if (!CharacterClothingKeep_Coordinate[5] && (clothespart[5].id != 0 || forceunder))
                    {
                        if (!UnderClothingKeep[5])
                        {
                            processed[5] = true;
                            clothespart[5] = underclothesparts[5];
                            Additional_Clothing_Process(5, outfitnum, Underwear_ME_Data);
                        }
                        if (!UnderClothingKeep[6] && !CharacterClothingKeep_Coordinate[6])
                        {
                            processed[6] = true;
                            clothespart[6] = underclothesparts[6];
                            Additional_Clothing_Process(6, outfitnum, Underwear_ME_Data);
                        }
                    }

                    if (!UnderClothingKeep[6] && !CharacterClothingKeep_Coordinate[6] && (clothespart[6].id != 0 || forceunder))
                    {
                        processed[6] = true;
                        clothespart[6] = underclothesparts[6];
                        Additional_Clothing_Process(6, outfitnum, Underwear_ME_Data);
                    }
                }
#endif
            }


#if false // 髪型色適用想定 
            var haircolor = new Color[] { ChaControl.fileHair.parts[1].baseColor, ChaControl.fileHair.parts[1].startColor, ChaControl.fileHair.parts[1].endColor, ChaControl.fileHair.parts[1].outlineColor };
            if (Settings.HairMatch.Value && !MakerAPI.InsideMaker)
            {
                foreach (var item in HairToColor)
                {
                    if (item < parts.Count)
                        HairMatchProcess(outfitnum, item, haircolor, parts);
                }
            }
#endif



#if false
            var insert = 0;
            var ACCpostion = 0;
            var Empty = false;
            var print = true;
            //Don't Skip if inside Maker

            var UnderwearAccessoryStart = keptacce.Count;
            var aidx = 0;
#endif
#if false
            if (MakerAPI.InsideMaker)
            {
                //Normal
                for (var n = newparts.Count; aidx < keptacce.Count && ACCpostion < n; ACCpostion++)
                {
                    Empty = ThisCoordinate.accessory.parts[ACCpostion].type < 121;
                    if (Empty) //120 is empty/default
                    {
                        if (insert++ >= UnderwearAccessoryStart)
                        {
                            ProcInfo.UnderwearAccessoriesLocations.Add(ACCpostion);
                        }

                        var acce = keptacce[aidx++];

                        newparts[ACCpostion] = acce.Parts;
                        if (acce.Hair.HairLength > -998)
                        {
                            HairAccInfo[ACCpostion] = acce.Hair;
                        }
                        else
                        {
                            HairAccInfo.Remove(ACCpostion);
                        }

                        ProcInfo.ACCKeepReturn.Add(ACCpostion);
                        Current.Material.AddAccessory(Index, ACCpostion, acce.Material);
                    }
#if false // 髪型色適用想定 
                    if (Settings.HairMatch.Value && HairAccInfo.TryGetValue(ACCpostion, out var info))
                    {
                        info.ColorMatch = true;
                        HairMatchProcess(outfitnum, ACCpostion, haircolor, parts);
                    }
#endif
                }
            }
#endif

            // 受け継ぐアクセ 
            for(var i = 0; i < keptacce.Count; ++i)
            {
                var loc = newparts.Count;
                var acce = keptacce[i];
                var use = false;

                switch (acce.Type)
                {
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

                    default:
                        // 適用する
                        use = true;
                        break;
                }

                if (acce.Hair.HairLength > -998)
                {
                    var HairInfo = acce.Hair;
#if false // 髪型色適用想定 
                    if (Settings.HairMatch.Value)
                    {
                        HairInfo.ColorMatch = true;
                        HairMatchProcess(outfitnum, ACCpostion, haircolor, parts);
                    }
#endif
                    Current.HairAccessories[loc] = HairInfo;
                }
                else
                {
                    Current.HairAccessories.Remove(loc);
                }

                ProcInfo.ACCKeepReturn.Add(loc);
                Current.Material.AddAccessory(Index, loc, acce.Material);

                if (use) newparts.Add(acce.Parts);
            }

#if false
            //original accessories
            while (aidx < keptacce.Count)
            {
                if (print)
                {
                    Settings.Logger.LogDebug($"Ran out of space in new coordinate adding {keptacce.Count}");
                    print = false;
                }
                if (insert++ >= UnderwearAccessoryStart)
                {
                    ProcInfo.UnderwearAccessoriesLocations.Add(ACCpostion);
                }

                var acce = keptacce[aidx++];

                newparts.Add(acce.Parts);
                if (acce.Hair.HairLength > -998)
                {
                    var HairInfo = acce.Hair;
#if false // 髪型色適用想定 
                    if (Settings.HairMatch.Value)
                    {
                        HairInfo.ColorMatch = true;
                        HairMatchProcess(outfitnum, ACCpostion, haircolor, parts);
                    }
#endif
                    HairAccInfo[ACCpostion] = HairInfo;
                }
                else
                {
                    HairAccInfo.Remove(ACCpostion);
                }

                ProcInfo.ACCKeepReturn.Add(ACCpostion);
                Current.Material.AddAccessory(Index, ACCpostion, acce.Material);
                ACCpostion++;
            }
#endif

            ThisCoordinate.accessory.parts = newparts.ToArray();

            //outfit.Outer.HairAccessories = HairAccInfo;
#endif
        }
    }
}
