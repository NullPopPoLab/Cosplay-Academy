using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

#pragma warning disable 0162 // unreached code

namespace CosplayParty
{
    public class ChaDefault
    {
        internal ChaControl ChaControl;
        internal ChaFileControl ChaFile;
        internal ChaFileParameter Parameter;
        internal SaveData.Heroine Heroine;

        /*! @note 初期処理を要するときtrueにする
        */
        //internal bool firstpass = true;
        public int RefreshedRevision { get; private set; }
        public static int RefreshingRevision { get; private set; } = 1;
        internal static void NeedRefresh() {
            ++RefreshingRevision;
            Settings.Logger.LogDebug($"NeedRefresh rev={RefreshingRevision}");
        }
        public bool IsRefreshed { get { return RefreshedRevision == RefreshingRevision; } }
        internal void MarkRefreshed() { RefreshedRevision = RefreshingRevision; }

        /*! @note コーデの再選択を行うときfalseにする
        */
        //internal bool processed = false;
        public int ProcessedRevision { get; private set; }
        public int ProcessingRevision { get; private set; } = 1;
        internal void NeedProcess() {
            ++ProcessingRevision;
            Settings.Logger.LogDebug($"NeedProcess rev={ProcessingRevision} for {ChaFile.charaFileName}");
        }
        public bool IsProcessed { get { return ProcessedRevision == ProcessingRevision; } }
        internal void MarkProcessed() { ProcessedRevision = ProcessingRevision; }

        internal readonly ChaOutfit[] Outfits;

        internal int Outfit_Size => ChaControl.chaFile.coordinate.Length;

#if false // for KoiChance 
#if KK
        internal string KoiOutfitpath;
        internal string ClubOutfitPath;
        internal bool ChangeKoiToClub;
        internal bool ChangeClubToKoi;
#endif
        internal bool Changestate = false;
#endif

        internal ClothingLoader ClothingLoader;

        public CharaInfo Info;

        public ChaDefault(ChaControl chaControl,ChaFileControl chaFile, SaveData.Heroine heroine)
        {
            Settings.Logger.LogDebug($"ChaDefault({chaFile?.parameter.fullname})");

            ChaControl = chaControl;
            ChaFile = chaFile;
            Parameter = ChaControl.fileParam;
            Heroine = heroine;

            Info = new CharaInfo(chaFile,chaControl);

            ClothingLoader = new ClothingLoader(this);

            Outfits = new ChaOutfit[Outfit_Size];
            for (int i = 0, n = Outfit_Size; i < n; i++)
            {
                Outfits[i] = new ChaOutfit(Info,i);
            }
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
