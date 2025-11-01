using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CosplayParty
{
    public class ChaDefault
    {
        internal ChaControl ChaControl;
        internal ChaFile Chafile;

        /*! @note 初期処理を要するときtrueにする
        */
        internal bool firstpass = true;

        /*! @note コーデの再選択を行うときtrueにする
        */
        internal bool processed = false;

        internal readonly ChaOutfit[] Outfits;

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

#region Material Editor Return
        public ME_List Finished;
#endregion

        //! シリアライズ用 HairAccessoryInfo 群 
        public Dictionary<int, Dictionary<int, Hair.HairSupport.HairAccessoryInfo>> HairAccessoriesPack
        {
            get
            {
                var t = new Dictionary<int, Dictionary<int, Hair.HairSupport.HairAccessoryInfo>>();
                for (var i = 0; i < Outfits.Length; ++i) t[i] = Outfits[i].HairAccessories;
                return t;
            }
        }

        public ChaDefault(ChaControl chaControl)
        {
            ChaControl = chaControl;
            ClothingLoader = new ClothingLoader(this);
            Finished = new ME_List(Outfit_Size);

            Outfits = new ChaOutfit[Outfit_Size];
            for (int i = 0, n = Outfit_Size; i < n; i++)
            {
                Outfits[i] = new ChaOutfit(this, i);
            }
        }

        public void Clear_Firstpass()
        {
            for (int i = 0, n = Outfit_Size; i < n; i++)
            {
                Outfits[i].Clear();
            }
            ME.TextureDictionary.Clear();
            Finished.SoftClear();
        }

        public void FillOutfitpaths()
        {
#if false // Additional_Card_Info 廃止予定 
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
#if KK
                        var cards = SimpleStruct[i].GetAvailableCards("kk");
#elif KKS
                        var cards = SimpleStruct[i].GetAvailableCards("kks");
#else
                        var cards = SimpleStruct[i].GetAvailableCards("none");
#endif
                        if (cards.Count > 0)
                            {
                                outfitpaths[i] = cards[UnityEngine.Random.RandomRangeInt(0, cards.Count)].GetFullPath();
                                Settings.Logger.LogDebug($"{(ChaFileDefine.CoordinateType)i} assigning " + outfitpaths[i]);
                            }
                    }

                    if (advanced)
                    {
                        if (adv.TryGetValue(Settings.SpecificCategories[i].Value, out var advdirectory) && !advdirectory.IsNullOrEmpty())
                        {
                            var advpath = defaultpath + sep + advdirectory;

#if false // 廃止予定 
                            if (!DataStruct.IndividualStructures.TryGetValue(advdirectory, out ADVStruct))
                            {
                                if (Directory.Exists(advpath))
                                {
                                    ADVStruct = DataStruct.LoadSingleStructure(advpath);
                                }
                            }
#endif

                            if (ADVStruct != null)
                            {
#if KK
                                var cards = ADVStruct.GetAvailableCards("kk");
#elif KKS
                                var cards = ADVStruct.GetAvailableCards("kks");
#else
                                var cards = ADVStruct.GetAvailableCards("none");
#endif
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
#endif
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
