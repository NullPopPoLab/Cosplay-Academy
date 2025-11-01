using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using KKAPI.Maker;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if TRACE
using System.Diagnostics;
#endif

namespace CosplayParty
{
    public partial class ClothingLoader
    {
        private static readonly char sep = System.IO.Path.DirectorySeparatorChar;

        private readonly ChaDefault ThisOutfitData;
        private ChaControl ChaControl;
        private ChaFile ChaFile;
#if false // 再検討; 下着可換 
        private static readonly int underwearindex = Constants.InputStrings.ToList().IndexOf($"{sep}Underwear");
#endif
        private static bool InsideMaker = false;

        #region Underwear stuff
        public readonly ChaFileCoordinate Underwear = new ChaFileCoordinate();
        //private readonly Dictionary<int, bool[]> Underwearbools = new Dictionary<int, bool[]>(); //0: not bot; 1: notbra; 2: notshorts
        private List<ChaFileAccessory.PartsInfo> Underwear_PartsInfos = new List<ChaFileAccessory.PartsInfo>();
        private ME_Coordinate Underwear_ME_Data;
        #endregion

        #region ACI_Data
#if false // Additional_Card_Info 廃止予定 
        public Additional_Card_Info.Cardinfo CardInfo { get; internal set; }
        internal bool[] PersonalClothingBools => CardInfo.PersonalClothingBools;
        public bool Character_Cosplay_Ready => CardInfo.CosplayReady;
#endif

        internal Dictionary<int, bool[]> CharacterClothingKeep_Coordinate = new Dictionary<int, bool[]>();
        #endregion
#if TRACE
        #region StopWatches
        private static bool TimeProcess = true;
        private static readonly Stopwatch[] TimeWatch = new Stopwatch[4];
        private static List<long>[] Average;
        #endregion
#endif
        //private readonly Dictionary<int, bool> ValidOutfits = new Dictionary<int, bool>();
        //private readonly Dictionary<int, bool> ValidUnderwears = new Dictionary<int, bool>();

        public ClothingLoader(ChaDefault ThisOutfitData)
        {
            this.ThisOutfitData = ThisOutfitData;
#if TRACE
            if (TimeProcess)// do once
            {
                TimeProcess = false;
                Average = new List<long>[TimeWatch.Length];
                for (var i = 0; i < TimeWatch.Length; i++)
                {
                    TimeWatch[i] = new Stopwatch();
                    Average[i] = new List<long>();
                }
            }
#endif
        }

        public void FullLoad(ChaControl character, ChaFile file)
        {
            Settings.Logger.LogDebug($"FullLoad({character.name})");

#if TRACE
            var Start = TimeWatch[0].ElapsedMilliseconds;
            TimeWatch[0].Start();
#endif
            InsideMaker = MakerAPI.InsideMaker;
            ChaControl = character;
            ChaFile = file;

            ThisOutfitData.FillOutfitpaths();
            var holdoutfitstate = ChaControl.fileStatus.coordinateType;

            //Underwear.LoadFile(ThisOutfitData.allunderwearpaths[underwearindex].GetFullPath());
            //Settings.Logger.LogDebug($"loaded underwear " + ThisOutfitData.allunderwearpaths[underwearindex]);

            Underwear_ME_Data = new ME_Coordinate(ExtendedSave.GetExtendedDataById(Underwear, "com.deathweasel.bepinex.materialeditor"), ThisOutfitData, 0);
            Underwear_PartsInfos = new List<ChaFileAccessory.PartsInfo>(Underwear.accessory.parts);
            //Underwear_PartsInfos.AddRange(Support.MoreAccessories.Coordinate_Accessory_Extract(Underwear));

            for (var i = 0; i < Constants.GameCoordinateSize; i++)
            {
                var outfit = ThisOutfitData.Outfits[i];

                //if (!UnderwearAccessoriesLocations.ContainsKey(i)) UnderwearAccessoriesLocations[i] = new List<int>();

                //if (!MakeUpKeep.ContainsKey(i)) MakeUpKeep[i] = false;

                //if (!Underwearbools.ContainsKey(i)) Underwearbools[i] = new bool[3];

                //if (!UnderwearProcessed.ContainsKey(i)) UnderwearProcessed[i] = new bool[9];

                outfit.Outer.Select();
                outfit.Inner.Select();

                if (outfit.Outer.IsLoaded || outfit.Inner.IsLoaded)
                {
                    GeneralizedLoad(i);
                    if (ThisOutfitData.Outfits[i].Outer.IsLoaded)
                    {
                        Settings.Logger.LogDebug($"loaded {(ChaFileDefine.CoordinateType)i} " + outfit.Outer.Selected.GetFullPath());
                    }
                    else
                    {
                        Settings.Logger.LogDebug($"loaded {(ChaFileDefine.CoordinateType)i} Default with changed underwear");
                    }
                }
                else
                {
                    Settings.Logger.LogDebug($"No valid outfits found for {(ChaFileDefine.CoordinateType)i}");
                }
            }

            ChaControl.fileStatus.coordinateType = holdoutfitstate;
#if TRACE
            TimeWatch[0].Stop();
            var temp = TimeWatch[0].ElapsedMilliseconds - Start;
            Average[0].Add(temp);
            Settings.Logger.LogDebug($"\tFullLoad: Total elapsed time {TimeWatch[0].ElapsedMilliseconds}ms\n\tRun {Average[0].Count}: {temp}ms\n\tAverage: {Average[0].Average()}ms");
#endif

            // ここで CoordinateProcessInfo が参照される 
            Run_Repacks(character);
        }

        public void GeneralizedLoad(int outfitnum)
        {
#if TRACE
            var Start = TimeWatch[1].ElapsedMilliseconds;
            TimeWatch[1].Start();
#endif
            var outfit = ThisOutfitData.Outfits[outfitnum];

            if (!ThisOutfitData.Finished.Coordinates.TryGetValue(outfitnum, out var ME_coord))
            {
                ThisOutfitData.Finished.Coordinates[outfitnum] = ME_coord = new ME_Coordinate();
            }

            ChaControl.fileStatus.coordinateType = outfitnum;
            //UnderwearProcessed[outfitnum] = new bool[9];
            var ThisCoordinate = ChaControl.chaFile.coordinate[outfitnum];

            #region Queue accessories to keep

#if false
            // CoordinateSuccession から取り込んだ情報 
            var PartsQueue = new Queue<ChaFileAccessory.PartsInfo>();
            var HairQueue = new Queue<HairSupport.HairAccessoryInfo>();
            var HairKeepQueue = new Queue<bool>();
            var ACCKeepqueue = new Queue<bool>();
            var ME_Queue = new Queue<MaterialEditorProperties>(outfit.Original_Accessory_Data);
#endif

            var UnderClothingKeep = new bool[9];

            #endregion
            //Load new outfit

#if false // ロードは済んでいる 
            if (ThisOutfitData.Outfits[outfitnum] != null)
            {
                ThisOutfitData.Outfits[outfitnum].Load(ChaControl, ThisOutfitData, outfitnum);
            }
            if (ThisOutfitData.Underwears[outfitnum] != null)
            {
                ThisOutfitData.Underwears[outfitnum].Load(ChaControl, ThisOutfitData, outfitnum);
            }
#endif
            //ValidOutfits[outfitnum] = load_outfit;
            if (outfit.Outer.IsLoaded)
            {
#if false // Additional_Card_Info 廃止予定 
                ME_coord.SoftClear(PersonalClothingBools);
#else
                ME_coord.SoftClear(new bool[9]);
#endif
                //only requeue items if a new file is loaded as they are unloaded.
                //                PartsQueue = new Queue<ChaFileAccessory.PartsInfo>(outfit.Succession.CoordinatePartsQueue);
                //                HairQueue = new Queue<HairSupport.HairAccessoryInfo>(outfit.Succession.HairAccQueue);
                //                HairKeepQueue = new Queue<bool>(outfit.Succession.HairKeepQueue);
                //                ACCKeepqueue = new Queue<bool>(outfit.Succession.ACCKeepQueue);
                //ME_Queue = new Queue<MaterialEditorProperties>(outfit.Original_Accessory_Data);
            }

            var keptacce = outfit.Succession.KeptAccessories;

            outfit.ProcInfo.Reset();

            var UnderwearAccessoryStart = keptacce.Count;
#region MakeUp
            if (outfit.MakeUpKeep)
            {
                ThisCoordinate.enableMakeup = ThisOutfitData.Original_Coordinates[outfitnum].enableMakeup;
                ThisCoordinate.makeup = ThisOutfitData.Original_Coordinates[outfitnum].makeup;
            }
#endregion
            var HairToColor = new List<int>();
            #region Reassign Existing Accessories

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
#endif

            if (Settings.HairMatch.Value && !MakerAPI.InsideMaker && Settings.DestinationHeadAccs.Value)
            {
                // 頭に載っている髪パーツのみ対象とする 
                for (var i = 0; i < ThisCoordinate.accessory.parts.Length; ++i)
                {
                    var p = ThisCoordinate.accessory.parts[i];
                    if (!Constants.HeadAcceSet.Contains(p.parentKey) && !Constants.HatAcceSet.Contains(p.parentKey)) continue;
                    HairToColor.Add(i);
                }
            }
            if (UnderClothingKeep == null) UnderClothingKeep = new bool[9];
            if (HairToColor == null) HairToColor = new List<int>();
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
            outfit.ProcInfo.UnderClothingKeep = UnderClothingKeep;

            var Inputdata = ExtendedSave.GetExtendedDataById(ThisCoordinate, "com.deathweasel.bepinex.hairaccessorycustomizer");
            var HairAccInfo = outfit.HairAccessories;
            if (Inputdata != null)
            {
                if (Inputdata.version == 0)
                {
                    if (Inputdata.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                        HairAccInfo = MessagePackSerializer.Deserialize<Dictionary<int, HairSupport.HairAccessoryInfo>>((byte[])loadedHairAccessories);
                }
                else
                {
                    OutdatedMessage("hairaccessorycustomizer", true);
                }
            }
#region ME Acc Import
            var MaterialEditorData = ExtendedSave.GetExtendedDataById(ThisCoordinate, "com.deathweasel.bepinex.materialeditor");
            ThisOutfitData.Finished.LoadCoordinate(MaterialEditorData, ThisOutfitData, outfitnum);
            var Import_ME_Data = new MaterialEditorProperties();
            #endregion

            var parts = new List<ChaFileAccessory.PartsInfo>();
            // ロード対象アクセのみ選択 
            for (var i = 0; i < ThisCoordinate.accessory.parts.Length; ++i)
            {
                var p = ThisCoordinate.accessory.parts[i];
                if (!Settings.DestinationHeadAccs.Value && Constants.HeadAcceSet.Contains(p.parentKey)) continue;
                if (!Settings.DestinationForeheadAccs.Value && Constants.ForeheadAcceSet.Contains(p.parentKey)) continue;
                if (!Settings.DestinationHatAccs.Value && Constants.HatAcceSet.Contains(p.parentKey)) continue;
                if (!Settings.DestinationEarAccs.Value && Constants.EarAcceSet.Contains(p.parentKey)) continue;
                if (!Settings.DestinationEyeAccs.Value && Constants.EyeAcceSet.Contains(p.parentKey)) continue;
                if (!Settings.DestinationNoseAccs.Value && Constants.NoseAcceSet.Contains(p.parentKey)) continue;
                if (!Settings.DestinationMouthAccs.Value && Constants.MouthAcceSet.Contains(p.parentKey)) continue;
                if (!Settings.DestinationTailAccs.Value && Constants.TailAcceSet.Contains(p.parentKey)) continue;
                parts.Add(p);
            }

            if (outfit.Inner.IsLoaded)
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

            var insert = 0;
            var ACCpostion = 0;
            var Empty = false;
            var print = true;
            //Don't Skip if inside Maker

            var aidx = 0;
            if (MakerAPI.InsideMaker)
            {
                //Normal
                for (var n = parts.Count; aidx< keptacce.Count && ACCpostion < n; ACCpostion++)
                {
                    Empty = ThisCoordinate.accessory.parts[ACCpostion].type < 121;
                    if (Empty) //120 is empty/default
                    {
                        if (insert++ >= UnderwearAccessoryStart)
                        {
                            outfit.ProcInfo.UnderwearAccessoriesLocations.Add(ACCpostion);
                        }

                        var acce = keptacce[aidx++];

                        parts[ACCpostion] = acce.Part;
                        if (acce.Hair.HairLength > -998)
                        {
                            HairAccInfo[ACCpostion] = acce.Hair;
                        }
                        else
                        {
                            HairAccInfo.Remove(ACCpostion);
                        }

                        if (acce.ForHair)
                        {
                            outfit.ProcInfo.HairKeepReturn.Add(ACCpostion);
                        }
                        if (acce.ForAcce)
                        {
                            outfit.ProcInfo.ACCKeepReturn.Add(ACCpostion);
                        }

                        ME_coord.AddAccessory(outfitnum, ACCpostion, acce.Material);
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
            while (aidx< keptacce.Count)
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

                parts.Add(acce.Part);
                if (acce.Hair.HairLength > -998)
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

                ME_coord.AddAccessory(outfitnum, ACCpostion, acce.Material);

                if (acce.ForHair)
                {
                    outfit.ProcInfo.HairKeepReturn.Add(ACCpostion);
                }
                if (acce.ForAcce)
                {
                    outfit.ProcInfo.ACCKeepReturn.Add(ACCpostion);
                }

                ACCpostion++;
            }

            ThisCoordinate.accessory.parts = parts.ToArray();

            //outfit.Outer.HairAccessories = HairAccInfo;
            #endregion

#if TRACE
            TimeWatch[1].Stop();
            var temp = TimeWatch[1].ElapsedMilliseconds - Start;
            Average[1].Add(temp);
            Settings.Logger.LogDebug($"\t{(ChaFileDefine.CoordinateType)outfitnum} GeneralLoad: Total elapsed time {TimeWatch[1].ElapsedMilliseconds}ms\n\tRun {Average[1].Count}: {temp}ms\n\tAverage: {Average[1].Average()}ms");
#endif
        }

        //! コーデカードのロード 
        public void CoordinateLoad(ChaFileCoordinate coordinate, ChaControl chacontrol)
        {
            Settings.Logger.LogDebug($"CoordinateLoad({coordinate?.coordinateFileName},{chacontrol?.name})");

            ChaControl = chacontrol;
            ChaFile = ThisOutfitData.Chafile;
            InsideMaker = MakerAPI.InsideMaker;


#region Queue accessories to keep

            var outfitnum = chacontrol.fileStatus.coordinateType;
            var outfit = ThisOutfitData.Outfits[outfitnum];

            //            var PartsQueue = new Queue<ChaFileAccessory.PartsInfo>(outfit.Succession.CoordinatePartsQueue);
            //            var HairQueue = new Queue<HairSupport.HairAccessoryInfo>(outfit.Succession.HairAccQueue);
            //            var ACCKeepQueue = new Queue<bool>(outfit.Succession.ACCKeepQueue);
            //            var HairKeepQueue = new Queue<bool>(outfit.Succession.HairKeepQueue);
            //            var ME_Queue = new Queue<MaterialEditorProperties>(outfit.Original_Accessory_Data);
            var keptacce = outfit.Succession.KeptAccessories;

            var HairKeepResult = new List<int>();
            var ACCKeepResult = new List<int>();

            #region ME Acc Import
            var MaterialEditorData = ExtendedSave.GetExtendedDataById(coordinate, "com.deathweasel.bepinex.materialeditor");

            var Coordinate_ME_Data = new ME_Coordinate(MaterialEditorData, ThisOutfitData, outfitnum);
            #endregion

            #endregion

            //Apply pre-existing Accessories in any open slot or final slots.

            var OriginalData = chacontrol.nowCoordinate.accessory.parts.ToList();

            #region Reassign Existing Accessories

            var Inputdata = ExtendedSave.GetExtendedDataById(coordinate, "com.deathweasel.bepinex.hairaccessorycustomizer");
            var HairACCDictionary = new Dictionary<int, HairSupport.HairAccessoryInfo>();
            if (Inputdata != null)
                if (Inputdata.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null)
                    HairACCDictionary = MessagePackSerializer.Deserialize<Dictionary<int, HairSupport.HairAccessoryInfo>>((byte[])loadedHairAccessories);

            var aidx = 0;
            var ACCpostion = 0;
            bool Empty;
            for (var n = OriginalData.Count; aidx< keptacce.Count && ACCpostion < n; ACCpostion++)
            {
                Empty = OriginalData[ACCpostion].type == 120;
                if (Empty) //120 is empty/default
                {
                    var acce = keptacce[aidx++];

                    OriginalData[ACCpostion] = acce.Part;
                    if (acce.Hair.HairLength > -998)
                    {
                        HairACCDictionary[ACCpostion] = acce.Hair;
                    }
                    else
                    {
                        HairACCDictionary.Remove(ACCpostion);
                    }

                    Coordinate_ME_Data.AddAccessory(outfitnum, ACCpostion, acce.Material);

                    if (acce.ForHair)
                    {
                        HairKeepResult.Add(ACCpostion);
                    }
                    if (acce.ForAcce)
                    {
                        ACCKeepResult.Add(ACCpostion);
                    }
                }
                if (Settings.HairMatch.Value && HairACCDictionary.TryGetValue(ACCpostion, out var info))
                {
                    info.ColorMatch = true;
                }
            }

            var print = true;

            while (aidx< keptacce.Count)
            {
                if (print)
                {
                    Settings.Logger.LogDebug($"Ran out of space in new coordiante adding {keptacce.Count}");
                    print = false;
                }

                var acce = keptacce[aidx++];

                OriginalData.Add(acce.Part);
                if (acce.Hair.HairLength > -998)
                {
                    var HairInfo = acce.Hair;
                    if (Settings.HairMatch.Value)
                    {
                        HairInfo.ColorMatch = true;
                    }
                    HairACCDictionary[ACCpostion] = HairInfo;
                }
                else
                {
                }

                Coordinate_ME_Data.AddAccessory(outfitnum, ACCpostion, acce.Material);

                if (InsideMaker)
                {
                    if (acce.ForHair)
                    {
                        HairKeepResult.Add(ACCpostion);
                    }
                    if (acce.ForAcce)
                    {
                        ACCKeepResult.Add(ACCpostion);
                    }
                }
                ACCpostion++;
            }

            chacontrol.nowCoordinate.accessory.parts = OriginalData.ToArray();

            MoreAccessoriesKOI.MoreAccessories.ArraySync(chacontrol);
            #endregion

            #region Pack
            var SaveData = new PluginData();

            Coordinate_ME_Data.AllProperties(out var rendererProperties, out var materialFloatProperties, out var materialColorProperties, out var materialShaders, out var materialTextureProperties);

            var TextureDictionary = ThisOutfitData.ME.TextureDictionary.Where(pair => materialTextureProperties.Any(x => x.TexID == pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value.Data);
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

            ExtendedSave.SetExtendedDataById(coordinate, "com.deathweasel.bepinex.materialeditor", SaveData);

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
                var Plugdata = new PluginData();

                Plugdata.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(HairACCDictionary));
                ExtendedSave.SetExtendedDataById(coordinate, "com.deathweasel.bepinex.hairaccessorycustomizer", Plugdata);

                //ControllerCoordReload_Loop(Type.GetType("KK_Plugins.HairAccessoryCustomizer+HairAccessoryController, KK_HairAccessoryCustomizer", false), ChaControl, coordinate);
            }
        }

        private void Additional_Clothing_Process(int index, int outfitnum, ME_Coordinate ME_Data)
        {
            var finishcoords = ThisOutfitData.Finished.Coordinates;
            if (!finishcoords.TryGetValue(outfitnum, out var finishcoord))
            {
                finishcoords[outfitnum] = new ME_Coordinate();
            }

            if (ME_Data.ClothingProperties.TryGetValue(index, out var editorProperties))
            {
                finishcoord.ClothingProperties[index] = editorProperties;
                return;
            }

            finishcoord.ClothingProperties.Remove(index);
        }

        private void HairMatchProcess(int outfitnum, int ACCPosition, Color[] haircolor, List<ChaFileAccessory.PartsInfo> Parts)
        {
            Parts[ACCPosition].color = haircolor;
            if (!ThisOutfitData.Finished.Coordinates.TryGetValue(outfitnum, out var coord))
            {
                ThisOutfitData.Finished.Coordinates[outfitnum] = coord = new ME_Coordinate();
            }
            if (!coord.AccessoryProperties.TryGetValue(ACCPosition, out var editorProperties))
            {
                return;
            }
            var haircomponent = editorProperties.MaterialColorProperty;
            var hairpart = ChaControl.fileHair.parts[1];
            for (var i = 0; i < haircomponent.Count; i++)
            {
                if (haircomponent[i].Property == "Color")
                {
                    haircomponent[i].Value = hairpart.baseColor;
                    continue;
                }
                if (haircomponent[i].Property == "Color2")
                {
                    haircomponent[i].Value = hairpart.startColor;
                    continue;
                }
                if (haircomponent[i].Property == "Color3")
                {
                    haircomponent[i].Value = hairpart.endColor;
                    continue;
                }
                if (haircomponent[i].Property == "ShadowColor")
                {
                    haircomponent[i].Value = hairpart.outlineColor;
                    continue;
                }
            }
        }
    }
}