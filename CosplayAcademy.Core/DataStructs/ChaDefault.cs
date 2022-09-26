﻿using Cosplay_Academy.Hair;
using Cosplay_Academy.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cosplay_Academy
{
    public class ChaDefault
    {
        internal ChaControl ChaControl;
        internal ChaFile Chafile;

        internal bool firstpass = true;
        internal bool processed = false;

        internal Dictionary<int, List<ChaFileAccessory.PartsInfo>> CoordinatePartsQueue = new Dictionary<int, List<ChaFileAccessory.PartsInfo>>();
        internal Dictionary<int, CardData> alloutfitpaths = new Dictionary<int, CardData>();
        internal readonly Dictionary<int, string> outfitpaths = new Dictionary<int, string>();

        internal int Outfit_Size => ChaControl.chaFile.coordinate.Length;

        internal ChaFileParameter Parameter;

        internal SaveData.Heroine heroine;

#if KK
        internal string KoiOutfitpath;
        internal string ClubOutfitPath;
        internal bool ChangeKoiToClub;
        internal bool ChangeClubToKoi;
#endif
        internal bool Changestate = false;
        internal bool SkipFirstPriority = false;
        internal ME_Support ME = new ME_Support();

        internal ClothingLoader ClothingLoader;
        internal Dictionary<int, ChaFileCoordinate> Original_Coordinates = new Dictionary<int, ChaFileCoordinate>();
        internal Dictionary<string, PluginData> ExtendedCharacterData = new Dictionary<string, PluginData>();
        internal Dictionary<int, List<bool>> HairKeepQueue = new Dictionary<int, List<bool>>();
        internal Dictionary<int, List<bool>> ACCKeepQueue = new Dictionary<int, List<bool>>();

        #region hair accessories
        public Dictionary<int, List<HairSupport.HairAccessoryInfo>> HairAccQueue = new Dictionary<int, List<HairSupport.HairAccessoryInfo>>();
        #endregion

        #region Material Editor Save
        public Dictionary<int, List<MaterialEditorProperties>> Original_Accessory_Data = new Dictionary<int, List<MaterialEditorProperties>>();
        #endregion

        #region Material Editor Return
        public ME_List Finished;
        #endregion

        internal Dictionary<int, List<int>> HairKeepReturn = new Dictionary<int, List<int>>();
        internal Dictionary<int, List<int>> ACCKeepReturn = new Dictionary<int, List<int>>();

        public ChaDefault(ChaControl chaControl)
        {
            ChaControl = chaControl;
            ClothingLoader = new ClothingLoader(this);
            Finished = new ME_List(Outfit_Size);
        }

        public void Clear_Firstpass()
        {
            for (int i = 0, n = Outfit_Size; i < n; i++)
            {
                if (!HairKeepQueue.ContainsKey(i))
                {
                    HairKeepQueue[i] = new List<bool>();
                    ACCKeepQueue[i] = new List<bool>();
                    HairKeepReturn[i] = new List<int>();
                    ACCKeepReturn[i] = new List<int>();
                    Original_Accessory_Data[i] = new List<MaterialEditorProperties>();
                    HairAccQueue[i] = new List<HairSupport.HairAccessoryInfo>();
                    CoordinatePartsQueue[i] = new List<ChaFileAccessory.PartsInfo>();
                    continue;
                }

                HairKeepQueue[i].Clear();
                ACCKeepQueue[i].Clear();
                HairKeepReturn[i].Clear();
                ACCKeepReturn[i].Clear();
                Original_Accessory_Data[i].Clear();
                HairAccQueue[i].Clear();
                CoordinatePartsQueue[i].Clear();
            }
            for (int i = Outfit_Size, n = HairKeepQueue.Keys.Count; i < n; i++)
            {
                HairKeepQueue.Remove(i);
                ACCKeepQueue.Remove(i);
                HairKeepReturn.Remove(i);
                ACCKeepReturn.Remove(i);
                Original_Accessory_Data.Remove(i);
                HairAccQueue.Remove(i);
                CoordinatePartsQueue.Remove(i);
            }
            ME.TextureDictionary.Clear();
            Finished.SoftClear();
        }

        public void FillOutfitpaths()
        {
            for (var i = 0; i < Constants.GameCoordinateSize; i++)
            {
                var card = alloutfitpaths[i];
                if (card == null) continue;
                outfitpaths[i] = card.GetFullPath();

                Settings.Logger.LogDebug($"{(ChaFileDefine.CoordinateType)i} assigning " + outfitpaths[i]);
            }

            var simpledirectory = ClothingLoader.CardInfo.SimpleFolderDirectory;
            var simplenull = simpledirectory.IsNullOrEmpty();
            var advanced = ClothingLoader.CardInfo.AdvancedDirectory;
            if (advanced || !simplenull)
            {
                var sep = Path.DirectorySeparatorChar;
                List<FolderStruct> SimpleStruct = null;
                FolderStruct ADVStruct = null;
                var defaultpath = Settings.CoordinatePath.Value;
                var adv = ClothingLoader.CardInfo.AdvancedFolderDirectory;

                if (!simplenull)
                {
                    var simplepath = defaultpath + sep + simpledirectory;
                    if (DataStruct.FullStructures.Any(x => x.Key.EndsWith(simpledirectory)))
                    {
                        SimpleStruct = DataStruct.FullStructures.First(x => x.Key.EndsWith(simpledirectory)).Value;
                    }
                    else if (Directory.Exists(simplepath))
                    {
                        SimpleStruct = DataStruct.LoadFullStructure(simplepath);
                    }
                }

                for (var i = 0; i < Constants.GameCoordinateSize; i++)
                {
                    if (SimpleStruct != null)
                    {
                            var cards = SimpleStruct[i].GetAllCards();
                            if (cards.Count > 0)
                            {
                                outfitpaths[i] = cards[UnityEngine.Random.RandomRangeInt(0, cards.Count)].GetFullPath();
                                Settings.Logger.LogDebug($"{(ChaFileDefine.CoordinateType)i} assigning " + outfitpaths[i]);
                            }
                    }

                    if (advanced)
                    {
                        if (adv.TryGetValue(Constants.SpecificCategories[i], out var advdirectory) && !advdirectory.IsNullOrEmpty())
                        {
                            var advpath = defaultpath + sep + advdirectory;

                            if (!DataStruct.IndividualStructures.TryGetValue(advdirectory, out ADVStruct))
                            {
                                if (Directory.Exists(advpath))
                                {
                                    ADVStruct = DataStruct.LoadSingleStructure(advpath);
                                }
                            }

                            if (ADVStruct != null)
                            {
                                var cards = ADVStruct.GetAllCards();
                                if (cards.Count > 0)
                                {
                                    outfitpaths[i] = cards[UnityEngine.Random.RandomRangeInt(0, cards.Count)].GetFullPath();
                                    Settings.Logger.LogDebug($"{(ChaFileDefine.CoordinateType)i} assigning " + outfitpaths[i]);
                                }
                            }
                        }
                        ADVStruct = null;
                    }
                }
            }
        }

        private void SpecialCondition(int coordinate, Dictionary<int, string> outfitpath, int datanum)
        {
#if false // KK ; おそらく廃止
            if (coordinate == 4)
            {
                if (heroine == null ? Settings.KoiClub.Value : heroine.isStaff && Settings.KeepOldBehavior.Value)
                {
                    if (UnityEngine.Random.Range(1, 101) <= Settings.KoiChance.Value)
                    {
                        outfitpath[coordinate] = KoiOutfitpath;
                    }
                }

                outfitpath[coordinate] = alloutfitpaths[datanum].GetFullPath();
            }
#endif
        }
    }
}
