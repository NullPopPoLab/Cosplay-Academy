using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

namespace CosplayParty
{
    public class ChaDefault
    {
        internal ChaControl ChaControl;
        internal ChaFile Chafile;
        internal ChaFileParameter Parameter;
        internal SaveData.Heroine Heroine;

        /*! @note 初期処理を要するときtrueにする
        */
        internal bool firstpass = true;

        /*! @note コーデの再選択を行うときtrueにする
        */
        internal bool processed = false;

        internal readonly ChaOutfit[] Outfits;

        internal int Outfit_Size => ChaControl.chaFile.coordinate.Length;

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
        internal Dictionary<string, PluginData> ExtendedCharacterData = new Dictionary<string, PluginData>();

#region Material Editor Return
        public ME_List FinalMaterials;
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
            FinalMaterials = new ME_List(Outfit_Size);

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
            FinalMaterials.SoftClear();
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

        public void Reset_Firstpass()
        {
            var HairExtendedData = ExtendedSave.GetExtendedDataById(Chafile, "com.deathweasel.bepinex.hairaccessorycustomizer");

            var CharaHair = new Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>>();
            if (HairExtendedData != null && HairExtendedData.data.TryGetValue("HairAccessories", out var AllHairAccessories) && AllHairAccessories != null)
                CharaHair = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>>>((byte[])AllHairAccessories);

            var MaterialEditorData = ExtendedSave.GetExtendedDataById(Chafile, "com.deathweasel.bepinex.materialeditor");
            FinalMaterials = new ME_List(MaterialEditorData, this);

            for (int outfitnum = 0, n = Outfit_Size; outfitnum < n; outfitnum++)
            {
                var outfit = Outfits[outfitnum];

                if (CharaHair.TryGetValue(outfitnum, out outfit.HairInfo) == false)
                {
                    outfit.HairInfo = new Dictionary<int, HairSupport.HairAccessoryInfo>();
                }

                /*! @todo コーデのアクセ毎に設定を追加
                */
                var atype = AccessoryType.Standard;

                outfit.Original_Coordinate = _cloneCoordinate(Chafile.coordinate[outfitnum]);
#if false // Additional_Card_Info 廃止予定 
                    var HairKeep = new List<int>();
                    var ACCKeep = new List<int>();
                    if (CoordinateInfo.ContainsKey(outfitnum))
                    {
                        HairKeep = CoordinateInfo[outfitnum].HairAcc;
                        ACCKeep = CoordinateInfo[outfitnum].AccKeep;
                    }
#endif

                //var acclist = new List<ChaFileAccessory.PartsInfo>();
                var acclist = Chafile.coordinate[outfitnum].accessory.parts.ToList();

                //var ME_ACC_Storage = outfit.Original_Accessory_Data;

                if (!FinalMaterials.Coordinates.TryGetValue(outfitnum, out var coord))
                {
                    coord = new ME_Coordinate();
                }

                // 強制的に保持するか 
                var xkeep = (Settings.ExtremeAccKeeper.Value
#if false // Additional_Card_Info 廃止予定 
                    && !Cosplay_Academy_Ready
#endif
                    );

                var ME_ACC_Data = coord.AccessoryProperties;
                for (var i = 0; i < acclist.Count; i++)
                {
                    {
                        if (atype == AccessoryType.Standard && !Settings.DestinationHeadAccs.Value && Constants.HeadAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.HairOrnament;
                        if (atype == AccessoryType.Standard && !Settings.DestinationForeheadAccs.Value && Constants.ForeheadAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.HairOrnament;
                        if (atype == AccessoryType.Standard && !Settings.DestinationHatAccs.Value && Constants.HatAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.HairOrnament;
                        if (atype == AccessoryType.Standard && !Settings.DestinationEarAccs.Value && Constants.EarAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.HairOrnament;
                        if (atype == AccessoryType.Standard && !Settings.DestinationEyeAccs.Value && Constants.EyeAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.HairOrnament;
                        if (atype == AccessoryType.Standard && !Settings.DestinationNoseAccs.Value && Constants.NoseAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.HairOrnament;
                        if (atype == AccessoryType.Standard && !Settings.DestinationMouthAccs.Value && Constants.MouthAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.HairOrnament;
                        if (atype == AccessoryType.Standard && !Settings.DestinationTailAccs.Value && Constants.TailAcceSet.Contains(acclist[i].parentKey)) atype = AccessoryType.Bodyfit;
                    }

                    // アクセを残すか 
                    var keep = xkeep || atype != AccessoryType.Standard;

                    //Settings.Logger.LogDebug($"Process: Acc {outfitnum}-{i} XK={xkeep} GI={geneinc} HK={hkeep} AK={akeep}");

                    //ExpandedOutfit.Logger.LogDebug($"ACC :{i}\tID: {data.nowAccessories[i].id}\tParent: {data.nowAccessories[i].parentKey}");
                    if (keep)
                    {
                        if (!outfit.HairInfo.TryGetValue(i, out var acchair))
                        {
                            acchair = new HairSupport.HairAccessoryInfo
                            {
                                HairLength = -999
                            };
                        }

                        if (!ME_ACC_Data.TryGetValue(i, out var accmat))
                        {
                            accmat = new MaterialEditorProperties();
                        }


                        //Settings.Logger.LogDebug($"Keep from 1stpass: Acc {outfitnum}-{i}; {Intermediate[i]}");

                        outfit.Succession.Keep(atype, acclist[i], acchair, accmat);
                    }
                }
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
