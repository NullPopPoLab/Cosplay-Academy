using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CosplayParty
{
    public class OverrideOuter : IDisposable
    {
        public bool IsLoaded;
        public string Path = "";
        public bool IsReady { get { return Path == "" || IsLoaded; } }

        private ChaDefault ThisOutfitData;
        private int Index;

        public OverrideOuter(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;
        }

        public void Dispose()
        {
            Unload();
            ThisOutfitData = null;
        }

        public void Unload()
        {
            if (!IsLoaded) return;
            IsLoaded = false;
            Path = "";
        }

        public void Load(string path)
        {
            Unload();

            if (String.IsNullOrEmpty(path)) return;
            if (!path.EndsWith(".png")) return;

            Path = path;
            var ThisCoordinate = ThisOutfitData.ChaControl.chaFile.coordinate[Index];
            IsLoaded = ThisCoordinate.LoadFile(Path);//in case it fails
        }
    }

    public class OverrideInner : IDisposable
    {
        public bool IsLoaded;
        public string Path = "";
        public bool IsReady { get { return Path == "" || IsLoaded; } }

        private ChaDefault ThisOutfitData;
        private int Index;

        public OverrideInner(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;
        }

        public void Dispose()
        {
            Unload();
            ThisOutfitData = null;
        }

        public void Unload()
        {
            if (!IsLoaded) return;
            IsLoaded = false;
            Path = "";
        }

        public void Load(string path)
        {
            Unload();

        }
    }

    //! 承継対象のアクセ 
    public class SuccessingAccessory
    {
        public ChaFileAccessory.PartsInfo Part;
        public HairSupport.HairAccessoryInfo Hair;
        public MaterialEditorProperties Material;

        //! 髪型として残すか 
        public bool ForHair;
        //! 髪型以外として残すか 
        public bool ForAcce;

        public SuccessingAccessory(ChaFileAccessory.PartsInfo part, HairSupport.HairAccessoryInfo hair, MaterialEditorProperties mat, bool hf, bool af)
        {
            Part = part;
            Hair = hair;
            Material = mat;
            ForHair = hf;
            ForAcce = af;
        }
    }

    //! コーデ差し替えで受け継ぐもの
    /*! @note 初回ロードで構築し、ずっと残しておく必要がある。
    */
    public class CoordinateSuccession : IDisposable
    {
        //! 次のコーデに差し替え後も残しておくべきアクセ情報の保持
        //        public List<ChaFileAccessory.PartsInfo> CoordinatePartsQueue = new List<ChaFileAccessory.PartsInfo>();
        //! 次のコーデに差し替え後も残しておくべきアクセ情報の保持
        //        public List<HairSupport.HairAccessoryInfo> HairAccQueue = new List<HairSupport.HairAccessoryInfo>();
        //! CoordinatePartsQueue や HairAccQueue に登録するアクセそれぞれについて、髪型として残すかのフラグ
        //        public List<bool> HairKeepQueue = new List<bool>();
        //! CoordinatePartsQueue や HairAccQueue に登録するアクセそれぞれについて、髪型以外として残すかのフラグ
        //        public List<bool> ACCKeepQueue = new List<bool>();

        //! 承継対象のアクセ 
        public List<SuccessingAccessory> KeptAccessories = new List<SuccessingAccessory>();

        public void Dispose()
        {
            Reset();
        }

        public void Reset()
        {
            KeptAccessories.Clear();
        }

        public void Keep(ChaFileAccessory.PartsInfo part, HairSupport.HairAccessoryInfo hair, MaterialEditorProperties mat, bool hf, bool af)
        {
            KeptAccessories.Add(new SuccessingAccessory(part,hair,mat,hf,af));
        }
    }

    //! コーデ編集情報 
    /*! @note 編集前に Reset() を呼ぶ。
    */
    public class CoordinateProcessInfo : IDisposable
    {
        //! コーデ差し替え後に HairAccQueue から適用したアクセのインデクスが追記される 
        public readonly List<int> HairKeepReturn = new List<int>();
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
            HairKeepReturn.Clear();
            ACCKeepReturn.Clear();
            UnderwearAccessoriesLocations.Clear();
            UnderwearProcessed = new bool[9];
            UnderClothingKeep = new bool[9];
        }
    }

    public class ChaOutfit : IDisposable
    {
        private ChaDefault ThisOutfitData;
        private int Index;

        public readonly OverrideOuter Outer;
        public readonly OverrideInner Inner;

        // このあたりの構造、ロード前のコーデ適用なんかもあるのでロードと密連動させてはならない 
        // 用途に応じて適切なタイミングで扱う必要がある。 
        public readonly CoordinateSuccession Succession;
        public readonly CoordinateProcessInfo ProcInfo;

        /*! @note シリアライズ向けにアクセ情報がまとめて保持される。
        */
        public Dictionary<int, HairSupport.HairAccessoryInfo> HairAccessories = new Dictionary<int, HairSupport.HairAccessoryInfo>();


        public bool MakeUpKeep = false;

        public ChaOutfit(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;

            Outer = new OverrideOuter(tod, idx);
            Inner = new OverrideInner(tod, idx);
            Succession = new CoordinateSuccession();
            ProcInfo = new CoordinateProcessInfo();
        }

        public void Dispose()
        {
            Clear();
            Outer.Dispose();
            Inner.Dispose();
            Succession.Dispose();
            ProcInfo.Dispose();
            ThisOutfitData = null;
        }

        public void Clear()
        {
            //Outer.Unload();
            //Inner.Unload();
            Succession.Reset();
            ProcInfo.Reset();

            HairAccessories.Clear();
        }
    }

    public class ChaDefault
    {
        internal ChaControl ChaControl;
        internal ChaFile Chafile;

        internal bool firstpass = true;
        internal bool processed = false;

        internal readonly ChaOutfit[] Outfits;

        internal CardData[] outfitcards;
        internal CardData[] underwearcards;
        internal string[] outfitpaths;
        internal string[] underwearpaths;

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

            outfitcards=new CardData[Outfit_Size];
            underwearcards = new CardData[Outfit_Size];
            outfitpaths=new string[Outfit_Size];
            underwearpaths = new string[Outfit_Size];

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
#if false
                if (!HairKeepQueue.ContainsKey(i))
                {
                    HairKeepQueue[i] = new List<bool>();
                    ACCKeepQueue[i] = new List<bool>();
                    Original_Accessory_Data[i] = new List<MaterialEditorProperties>();
                    HairAccQueue[i] = new List<HairSupport.HairAccessoryInfo>();
                    CoordinatePartsQueue[i] = new List<ChaFileAccessory.PartsInfo>();
                    continue;
                }

                HairKeepQueue[i].Clear();
                ACCKeepQueue[i].Clear();
                Original_Accessory_Data[i].Clear();
                HairAccQueue[i].Clear();
                CoordinatePartsQueue[i].Clear();
            }
                for (int i = Outfit_Size, n = HairKeepQueue.Keys.Count; i < n; i++)
                {
                    HairKeepQueue.Remove(i);
                    ACCKeepQueue.Remove(i);
                    Original_Accessory_Data.Remove(i);
                    HairAccQueue.Remove(i);
                    CoordinatePartsQueue.Remove(i);
#endif
            }
            ME.TextureDictionary.Clear();
            Finished.SoftClear();
        }

        public void FillOutfitpaths()
        {
            for (var i = 0; i < Constants.GameCoordinateSize; i++)
            {
                var card = outfitcards[i];
                if (card != null)
                {
                    outfitpaths[i] = card.GetFullPath();
                    Settings.Logger.LogDebug($"{(ChaFileDefine.CoordinateType)i} outfit assigning " + outfitpaths[i]);
                }
                else
                {
                    outfitpaths[i] = "";
                }

                card = underwearcards[i];
                if (card != null)
                {
                    underwearpaths[i] = card.GetFullPath();
                    Settings.Logger.LogDebug($"{(ChaFileDefine.CoordinateType)i} underware assigning " + underwearpaths[i]);
                }
                else {
                    underwearpaths[i] = "";
                }
            }

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
