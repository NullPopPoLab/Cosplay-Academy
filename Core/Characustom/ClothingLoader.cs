using System;
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

#if false // 再検討; 下着可換 
        public readonly ChaFileCoordinate Underwear = new ChaFileCoordinate();
        //private readonly Dictionary<int, bool[]> Underwearbools = new Dictionary<int, bool[]>(); //0: not bot; 1: notbra; 2: notshorts
        private List<ChaFileAccessory.PartsInfo> Underwear_PartsInfos = new List<ChaFileAccessory.PartsInfo>();
        private ME_Coordinate Underwear_ME_Data;
#endif

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

#if false // 再検討; 下着可換 
            Underwear_ME_Data = new ME_Coordinate(ExtendedSave.GetExtendedDataById(Underwear, "com.deathweasel.bepinex.materialeditor"), ThisOutfitData, 0);
            Underwear_PartsInfos = new List<ChaFileAccessory.PartsInfo>(Underwear.accessory.parts);
            //Underwear_PartsInfos.AddRange(Support.MoreAccessories.Coordinate_Accessory_Extract(Underwear));
#endif

            for (var i = 0; i < Constants.GameCoordinateSize; i++)
            {
                var outfit = ThisOutfitData.Outfits[i];

                outfit.Outer.Load();
                outfit.Inner.Load();

                if (outfit.Outer.IsLoaded || outfit.Inner.IsLoaded)
                {
                    GeneralizedLoad(i);
                    if (outfit.Outer.IsLoaded)
                    {
                        Settings.Logger.LogDebug($"loaded {(ChaFileDefine.CoordinateType)i} " + outfit.Outer.Path);
                    }
                    else
                    {
                        Settings.Logger.LogDebug($"loaded {(ChaFileDefine.CoordinateType)i} Default with changed underwear");
                    }
                    if (outfit.Inner.IsLoaded)
                    {
                        Settings.Logger.LogDebug($"loaded {(ChaFileDefine.CoordinateType)i} " + outfit.Inner.Path);
                    }

                    // 寧ろ壊れる 
                    //ThisOutfitData.ClothingLoader.ControllerCoordReload_Loop("Common.ControllerName", ChaControl, outfit.Current.Coordinate);
                }
                else
                {
                    Settings.Logger.LogDebug($"No valid outfits found for {(ChaFileDefine.CoordinateType)i}");
                }
            }

            ChaControl.fileStatus.coordinateType = holdoutfitstate;
            //MoreAccessoriesKOI.MoreAccessories.ArraySync(ChaControl);

#if TRACE
            TimeWatch[0].Stop();
            var temp = TimeWatch[0].ElapsedMilliseconds - Start;
            Average[0].Add(temp);
            Settings.Logger.LogDebug($"\tFullLoad: Total elapsed time {TimeWatch[0].ElapsedMilliseconds}ms\n\tRun {Average[0].Count}: {temp}ms\n\tAverage: {Average[0].Average()}ms");
#endif

            try
            {
                //ThisOutfitData.ME.Save(false);

                // ここで CoordinateProcessInfo が参照される 
                //Run_Repacks(character);
            }
            catch (Exception e)
            {
                Settings.Logger.LogError("ClothingLoader.FullLoad() Repack error: " + e);
            }
        }

        public void GeneralizedLoad(int outfitnum)
        {
            ThisOutfitData.ChaControl.fileStatus.coordinateType = outfitnum;

            var outfit = ThisOutfitData.Outfits[outfitnum];
            var coord = outfit.Outer.Coordinate;
            var outcapt = "";
            if (coord == null)
            {
                outcapt = ThisOutfitData.ChaFile.parameter.fullname + "-" + outfitnum;
                var src = ThisOutfitData.ChaFile.coordinate[outfitnum];
                coord = new ChaFileCoordinate();
                coord.LoadBytes(src.SaveBytes(), src.loadVersion);

                // PluginData はコピーされないので別途対応 
                ExtendedSave.SetExtendedDataById(coord, ME.Common.ExtendedDataName, ThisOutfitData.Info.Coord[outfitnum].Material.Export(true));
            }

            var outer = new CoordInfo(coord, outcapt);
            var inner = (outfit.Inner.Coordinate==null)?null:new CoordInfo(outfit.Inner.Coordinate);
            outfit.Override4Generalize(outer, inner);
        }

        //! コーデカードのロード 
        public void CoordinateLoad(ChaFileCoordinate coordinate, ChaControl chacontrol, ChaFileControl chafile)
        {
            Settings.Logger.LogDebug($"CoordinateLoad({coordinate?.coordinateFileName},{chacontrol?.name})");

            if (chacontrol != ThisOutfitData.ChaControl)
            {
                Settings.Logger.LogWarning($"ChaControl mismatch({chacontrol.name},{ThisOutfitData.ChaControl.name})");
            }
            if (chafile != ThisOutfitData.ChaFile)
            {
                Settings.Logger.LogWarning($"ChaFile mismatch({chafile.charaFileName},{ThisOutfitData.ChaFile.charaFileName})");
            }

            ChaControl = chacontrol;
            ChaFile = chafile;

            InsideMaker = MakerAPI.InsideMaker;

            var outfitnum = chacontrol.fileStatus.coordinateType;
            var outfit = ThisOutfitData.Outfits[outfitnum];
            if (Settings.RandomizeUnderwear.Value)
            {
                OutfitDecider.SelectInner(outfitnum);
                outfit.Inner.Load();
                if (outfit.Inner.IsLoaded)
                {
                    Settings.Logger.LogDebug($"loaded {(ChaFileDefine.CoordinateType)outfitnum} " + outfit.Inner.Path);
                }
            }

            var outer = new CoordInfo(coordinate);
            var inner = (outfit.Inner.Coordinate==null)?null:new CoordInfo(outfit.Inner.Coordinate);
            outfit.Override4Coordinate(outer, inner);
        }

#if false
        private void Additional_Clothing_Process(int index, int outfitnum, CoordProps ME_Data)
        {
            var finishcoords = ThisOutfitData.FinalMaterials.Coordinates;
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
            if (!ThisOutfitData.FinalMaterials.Coordinates.TryGetValue(outfitnum, out var coord))
            {
                ThisOutfitData.FinalMaterials.Coordinates[outfitnum] = coord = new ME_Coordinate();
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
#endif
    }
}