using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using KKAPI.Maker;

#pragma warning disable 0162 // unreached code

namespace CosplayParty
{
    public class ModifiedCloth
    {
        public ChaFileClothes.PartsInfo Parts;
        public ClothProps Material;
    }
    public class ModifiedAccessory
    {
        public ChaFileAccessory.PartsInfo Parts;
        public ME.AccessoryProps Material;
        public Hair.AccessoryProps Hair;
    }
    public class ModifiedCoord
    {
        public ModifiedCloth[] Clothes = new ModifiedCloth[Constants.ClothSlots];
        public Dictionary<int, ModifiedAccessory> Accessory = new Dictionary<int, ModifiedAccessory>();
    }

    public class CoordOverrider
    {
        public static bool Compact = false;
        public CharaInfo Target;
        public int Index;

        private ModifiedCoord _modify = new ModifiedCoord();

        public CoordOverrider(CharaInfo target, int idx)
        {
            Target = target;
            Index = idx;
        }

        public void Collaborate(CoordinateSuccession succession, CoordInfo outer, CoordInfo inner) {

            if (Settings.Dump_Coord) 
            {
                var outername = (outer == null) ? "none" : outer.Source.coordinateName;
                var innername = (inner == null) ? "none" : inner.Source.coordinateName;
                Settings.Logger.LogDebug($"Collaborate outer={outername} inner={innername}");
            }

            var keptacce = succession.KeptAccessories;

            // 髪型色にするアクセリスト 
            var HairToColor = new List<int>();
            if (Settings.HairMatch.Value && Settings.DestinationHeadAccs.Value && !MakerAPI.InsideMaker)
            {
                // 新コーデにあるアクセのうち、髪型扱いにするパーツのみ対象とする 
                for (var i = 0; i < outer.AcceList.Count; ++i)
                {
                    var acce = outer.AcceList[i];

                    if (acce.Type != AccessoryType.HairStyle) continue;
                    HairToColor.Add(i);
                }
            }

            // インナー差し替え 
            if (inner != null)
            {
                /*! @todo select slots by coord settings */
                var enablity = new bool[] { false, false, true/*bra*/, true/*shorts*/, false, true/*panst*/, false, false, false };

                for(var i=0;i< inner.Source.clothes.parts.Length; ++i)
                {
                    if (!enablity[i])
                    {
                        _modify.Clothes[i] = null;
                        continue;
                    }

                    var parts = inner.Source.clothes.parts[i];
                    if (Settings.Dump_Coord) Settings.Logger.LogDebug($"Inner Cloth {i} overridden; id={parts.id}");

                    var ci = new ModifiedCloth();
                    ci.Parts = parts;
                    ci.Material = inner.Material.GetClothProps(i, false);

                    _modify.Clothes[i] = ci;
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
            for (var i = 0; i < outer.AcceList.Count; ++i)
            {
                var acce = outer.AcceList[i];
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
                        // 空でなければ適用する
                        /*! @todo 下着付属アクセは除外
                        */
                        use = !acce.IsEmpty;
                        break;
                }

                if (use)
                {
                    if (Settings.Dump_Coord) Settings.Logger.LogDebug($"Outer Accessory {i + 1} => {aidx + 1} (as {acce.TypeName}) allowed; {acce.PartsID}");

                    modify.Parts = acce.Parts;
                }
                else
                {
                    if (Settings.Dump_Coord) Settings.Logger.LogDebug($"Outer Accessory {i + 1} (as {acce.TypeName}) denied; {acce.PartsID}");
                    if (Compact) continue;

                    // 空にする 
                    acce.Parts.type = 120;
                    acce.Parts.id = 0;
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

                if (use)
                {
                    if (Settings.Dump_Coord) Settings.Logger.LogDebug($"Kept Accessory {aidx + 1} (as {acce.TypeName}) allowed; {acce.PartsID}");

                    //ProcInfo.ACCKeepReturn.Add(aidx);

                    modify.Parts = acce.Parts;
                    modify.Hair = acce.Hair;
                    modify.Material = acce.Material;
                    _modify.Accessory[aidx++] = modify;
                }
                else
                {
                    if (Settings.Dump_Coord) Settings.Logger.LogDebug($"Kept Accessory {aidx + 1} (as {acce.TypeName}) denied; {acce.PartsID}");
                }
            }

            // 元アクセの残り部分を明示的に消しておく必要がある 
            for (; aidx < outer.AcceList.Count; ++aidx)
            {
                if (Settings.Dump_Coord) Settings.Logger.LogDebug($"Padding Accessory {aidx + 1} (as Empty)");

                var acce = outer.AcceList[aidx];
                acce.Parts.type = 120;
                acce.Parts.id = 0;
                var modify = new ModifiedAccessory();
                modify.Parts = acce.Parts;
                _modify.Accessory[aidx] = modify;
            }
        }

        public void Apply4Coordinate(CoordInfo coord)
        {
            // データ適用動作 
            //var HairData = new Dictionary<int, HairSupport.HairAccessoryInfo>();
            var TargetCloth = coord.Source.clothes.parts;
            var TargetAcce = coord.Source.accessory.parts.ToList();

            for (var i = 0; i < _modify.Clothes.Length; ++i)
            {
                var cloth = _modify.Clothes[i];
                if (cloth == null) continue;
                TargetCloth[i] = cloth.Parts;
                coord.Material.SetClothProps(Index, i, cloth.Material);
            }

            foreach (var acce in _modify.Accessory)
            {
                if (acce.Value.Parts != null)
                {
                    if (acce.Key < TargetAcce.Count) TargetAcce[acce.Key] = acce.Value.Parts;
                    else
                    {
                        while (acce.Key > TargetAcce.Count) TargetAcce.Add(null);
                        TargetAcce.Add(acce.Value.Parts);
                    }
                }
#if false
                HairData[acce.Key] = (acce.Value.Hair != null) ?
                    acce.Value.Hair :
                         new HairSupport.HairAccessoryInfo
                         {
                             HairLength = -999
                         };
#endif

                coord.Material.SetAccessoryProps(Index, acce.Key, acce.Value.Material);
                coord.Hair.SetAccessoryProps(Index, acce.Key, acce.Value.Hair);
            }

            Target.Control.nowCoordinate.accessory.parts = TargetAcce.ToArray();
            //MoreAccessoriesKOI.MoreAccessories.ArraySync(Target.ChaControl);

            coord.Material.Save(false);
            //Target.ME.Save(false);

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

//ControllerCoordReload_Loop(typeof(KK_Plugins.MaterialEditor.MaterialEditorCharaController), ChaControl, coordinate);

#if false
            if (Settings.HairMatch.Value)
            {
                var PlugData = new PluginData();

                PlugData.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(HairData));
                ExtendedSave.SetExtendedDataById(Target.Source, CosplayParty.Hair.Common.ExtendedDataName, PlugData);

                //ControllerCoordReload_Loop(Type.GetType("KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KK_HairAccessoryCustomizer", false), ChaControl, coordinate);
            }
#endif
        }

        public void Apply4Generalize(CoordInfo newouter)
        {
            var target = Target.Coord[Index];

            var UnderClothingKeep = new bool[9];
            var HairToColor = new List<int>();

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
                        OutdatedMessage("Additional_Card_Info", false);
                        break;
                }
            }
            else
            for (var i = 0; i < 9; i++)
            {
                if (PersonalClothingBools[i])
                {
                    UnderClothingKeep[i] = true;
                }
            }
#endif

            //if (outfit.Inner.IsLoaded)
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


#if false // たぶん無意味どころか変える必要ないところまで変わる 
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

            var aidx = 0;
            if (MakerAPI.InsideMaker)
            {
                //Normal
                for (var n = parts.Count; aidx < keptacce.Count && ACCpostion < n; ACCpostion++)
                {
                    Empty = ThisCoordinate.accessory.parts[ACCpostion].type < 121;
                    if (Empty) //120 is empty/default
                    {
                        if (insert++ >= UnderwearAccessoryStart)
                        {
                            outfit.ProcInfo.UnderwearAccessoriesLocations.Add(ACCpostion);
                        }

                        var acce = keptacce[aidx++];

                        parts[ACCpostion] = acce.Parts;
                        if (acce.Hair.HairLength > -998)
                        {
                            HairAccInfo[ACCpostion] = acce.Hair;
                        }
                        else
                        {
                            HairAccInfo.Remove(ACCpostion);
                        }

                        outfit.ProcInfo.ACCKeepReturn.Add(ACCpostion);
                        ME_coord.AddAccessory(Index, ACCpostion, acce.Material);
                    }
#if false // たぶん無意味どころか変える必要ないところまで変わる 
                    if (Settings.HairMatch.Value && HairAccInfo.TryGetValue(ACCpostion, out var info))
                    {
                        info.ColorMatch = true;
                        HairMatchProcess(outfitnum, ACCpostion, haircolor, parts);
                    }
#endif
                }
            }

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
                    outfit.ProcInfo.UnderwearAccessoriesLocations.Add(ACCpostion);
                }

                var acce = keptacce[aidx++];

                parts.Add(acce.Parts);
                if (acce.Hair == null)
                {
                    HairAccInfo.Remove(ACCpostion);
                }
                else if (acce.Hair.HairLength > -998)
                {
                    var HairInfo = acce.Hair;
#if false // たぶん無意味どころか変える必要ないところまで変わる 
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

                outfit.ProcInfo.ACCKeepReturn.Add(ACCpostion);
                ME_coord.AddAccessory(Index, ACCpostion, acce.Material);
                ACCpostion++;
            }
#endif
            //outfit.ProcInfo.UnderClothingKeep = UnderClothingKeep;

            var TargetAcce = newouter.Source.accessory.parts.ToList();

#if false
            //            var UnderwearAccessoryStart = keptacce.Count;
            if (outfit.MakeUpKeep)
            {
                newouter.Source.enableMakeup = outfit.Original_Coordinate.enableMakeup;
                newouter.Source.makeup = outfit.Original_Coordinate.makeup;
            }
#endif

            //var Inputdata = ExtendedSave.GetExtendedDataById(newouter, CosplayParty.Hair.Common.ExtendedDataName);
            //var HairAccInfo = outfit.Current.HairAccessories;
            //if (Inputdata != null)
            // {
            //    if (Inputdata.version == 0)
            //    {
            //        if (Inputdata.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
            //            HairAccInfo = MessagePackSerializer.Deserialize<Dictionary<int, HairSupport.HairAccessoryInfo>>((byte[])loadedHairAccessories);
            //    }
            //    else
            //    {
            //        ClothingLoader.OutdatedMessage("hairaccessorycustomizer", true);
            //    }
            //}
            //var MaterialEditorData = ExtendedSave.GetExtendedDataById(newouter, "com.deathweasel.bepinex.materialeditor");
            //Target.FinalMaterials.LoadCoordinate(MaterialEditorData, Target, Index);
            //var Import_ME_Data = new MaterialEditorProperties();

//            var HairData = new Dictionary<int, HairSupport.HairAccessoryInfo>();
            var TargetCloth = newouter.Source.clothes.parts;

            for (var i = 0; i < _modify.Clothes.Length; ++i)
            {
                var cloth = _modify.Clothes[i];
                if (cloth == null) continue;
                TargetCloth[i] = cloth.Parts;
                target.Material.SetClothProps(Index, i, cloth.Material);
            }

            foreach (var acce in _modify.Accessory)
            {
                if (acce.Value.Parts != null)
                {
                    if (acce.Key < TargetAcce.Count) TargetAcce[acce.Key] = acce.Value.Parts;
                    else
                    {
                        while (acce.Key > TargetAcce.Count) TargetAcce.Add(null);
                        TargetAcce.Add(acce.Value.Parts);
                    }
                }
#if false
                HairData[acce.Key] = (acce.Value.Hair != null) ?
                    acce.Value.Hair :
                         new HairSupport.HairAccessoryInfo
                         {
                             HairLength = -999
                         };
#endif

                target.Material.SetAccessoryProps(Index, acce.Key, acce.Value.Material);
                target.Hair.SetAccessoryProps(Index, acce.Key, acce.Value.Hair);
            }

            // nowCoordinate ではインナー差し替えが反映されない 
            //var TargetCoordinate = Target.ChaControl.nowCoordinate;
            var TargetCoordinate = Target.Control.chaFile.coordinate[Index];

            newouter.Source.clothes.parts = TargetCloth;
            newouter.Source.accessory.parts = TargetAcce.ToArray();
            target.Material.Save(true);
            target.Hair.Save();

            // MoreAccessories は追加の手順なしでも反映されてる 
            //MoreAccessoriesKOI.MoreAccessories.ArraySync(Target.ChaControl);

            TargetCoordinate.LoadBytes(newouter.Source.SaveBytes(), newouter.Source.loadVersion);
            //Target.ChaControl.AssignCoordinate((ChaFileDefine.CoordinateType)Index, newouter.Coordinate);

            //ClothingLoader.ControllerCoordReload_Loop(typeof(KK_Plugins.MaterialEditor.MaterialEditorCharaController), Target.ChaControl, TargetCoordinate);
            ClothingLoader.ControllerCoordReload_Loop(ME.Common.ControllerName, Target.Control, TargetCoordinate);
            ClothingLoader.ControllerCoordReload_Loop(Hair.Common.ControllerName, Target.Control, TargetCoordinate);

            //outfit.Outer.HairAccessories = HairAccInfo;

#if false
            if (Settings.HairMatch.Value)
            {
                var PlugData = new PluginData();

                PlugData.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(HairData));
                ExtendedSave.SetExtendedDataById(Target.ChaFile, Hair.Common.ExtendedDataName, PlugData);

                //ControllerCoordReload_Loop(Type.GetType("KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KK_HairAccessoryCustomizer", false), ChaControl, coordinate);
            }
#endif
        }
    }
}
