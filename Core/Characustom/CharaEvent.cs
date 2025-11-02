using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using MessagePack;
using System.Collections.Generic;
using UniRx;
#if TRACE
using System.Diagnostics;
#endif
using System.Linq;


namespace CosplayParty
{
    public class CharaEvent : CharaCustomFunctionController
    {
        public static List<ChaDefault> ChaDefaults = new List<ChaDefault>();

        internal ChaDefault ThisOutfitData;
        private ClothingLoader ClothingLoader => ThisOutfitData.ClothingLoader;

        public static List<SaveData.Heroine> FreeHHeroines { get; internal set; } = new List<SaveData.Heroine>();

        internal static int Firstpass = 0;
#if TRACE
        private static readonly Stopwatch Time = new Stopwatch();
        private static readonly List<long> Average = new List<long>();
#endif
        internal static bool inH = false; //hopefully code that will work if additional heroines are loaded in H actively. such as in Kplug, Not tested.

        internal static void MakerAPI_MakerExiting()
        {
            Firstpass = 0;
#if false
            if (!MakerAPI.IsInsideClassMaker())
            {
                ChaDefaults.Clear();
                OutfitDecider.ResetDecider();
            }
#endif
        }

        public static void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if false // たぶん廃止 
            var owner = Settings.Instance;
            e.AddSidebarControl(new SidebarToggle("Enable Cosplay Party", Settings.Makerview.Value, owner)).ValueChanged.Subscribe(value => Settings.Makerview.Value = value);
            e.AddSidebarControl(new SidebarToggle("CP: Rand outfits", Settings.ChangeOutfit.Value, owner)).ValueChanged.Subscribe(value => Settings.ChangeOutfit.Value = value);
            e.AddSidebarControl(new SidebarToggle("CP: Rand Underwear", Settings.RandomizeUnderwear.Value, owner)).ValueChanged.Subscribe(value => Settings.RandomizeUnderwear.Value = value);
            e.AddSidebarControl(new SidebarToggle("CP: Reset Sets", Settings.ResetMaker.Value, owner)).ValueChanged.Subscribe(value => Settings.ResetMaker.Value = value);
            e.AddSidebarControl(new SidebarToggle("CP: Only Underwear", Settings.RandomizeUnderwearOnly.Value, owner)).ValueChanged.Subscribe(value => Settings.RandomizeUnderwearOnly.Value = value);
#endif
        }

        protected override void OnReload(GameMode currentGameMode, bool MaintainState) //from KKAPI.Chara when characters enter reload state
        {
            if (ChaControl.sex == 0)
            {
                return;
            }
            if (currentGameMode == GameMode.Studio)
            {
                return;
            }
            var IsMaker = currentGameMode == GameMode.Maker;
            // エディット中は作用させない 
            if (IsMaker) return;

            Settings.Logger.LogDebug($"OnReload({currentGameMode})");

#if DEBUG
            Settings.Logger.LogDebug($"Processing {ChaControl.chaFile.parameter.fullname} {Firstpass}");
#endif
#if TRACE
            var Start = Time.ElapsedMilliseconds;
            if (ThisOutfitData == null || !ThisOutfitData.processed || currentGameMode == GameMode.Maker)
            {
                Time.Start();
            }
#endif
            if (/*IsMaker || !IsMaker &&*/ (ThisOutfitData == null || ThisOutfitData != null && !ThisOutfitData.processed))
            {
                Process(currentGameMode);

                ThisOutfitData.ClothingLoader.Reload_RePacks(ChaControl, inH);
            }
            else if (ThisOutfitData != null && ThisOutfitData.processed
#if !KKS
                && GameAPI.InsideHScene
#endif
                )
            {
                ThisOutfitData.Chafile = ChaFileControl;
                ThisOutfitData.ClothingLoader.Run_Repacks(ChaControl);

                ThisOutfitData.ClothingLoader.Reload_RePacks(ChaControl, inH);
            }

            if (/*IsMaker && Firstpass++ == 0 ||*/ inH)
            {
                ChaControl.ChangeCoordinateTypeAndReload();
            }
#if TRACE
            if (Time.IsRunning)
            {
                Time.Stop();
                var temp = Time.ElapsedMilliseconds - Start;
                Average.Add(temp);
                Settings.Logger.LogDebug($"Total elapsed time {Time.ElapsedMilliseconds}ms\nRun {Average.Count}: {temp}ms\nAverage: {Average.Average()}ms");
            }
#endif
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            //unused mandatory function 
        }

        private void ThisOutfitDataProcess()
        {
            if (ThisOutfitData != null && MakerAPI.InsideMaker)
            {
                return;
            }

            var heroine = ChaControl.GetHeroine() ?? FreeHHeroines.Find(x => x.chaCtrl == ChaControl);
#if DEBUG
            if (heroine == null)
                Settings.Logger.LogError($"Heroine Not found for {ChaControl.fileParam.fullname}");
#endif

            ThisOutfitData = ChaDefaults.Find(x => x.Heroine == heroine);

            if (ThisOutfitData == null)
            {
                ThisOutfitData = new ChaDefault(ChaControl)
                {
                    Parameter = ChaControl.fileParam,
                    Chafile = ChaFileControl,
                    ChaControl = ChaControl,
                    Heroine = heroine
                };
#if DEBUG
                //Settings.Logger.LogDebug($"Heroine null? {heroine == null}\nInH? {inH}");
#endif
                ChaDefaults.Add(ThisOutfitData);
                return;
            }

            ThisOutfitData.ChaControl = ChaControl;
            ThisOutfitData.Chafile = ChaFileControl;
        }

        public void Process(GameMode currentGameMode)
        {
            try
            {

                // disabled them 
                switch (currentGameMode)
                {
                    case GameMode.Studio: return;

                    case GameMode.Maker:
                        /*if (!Settings.Makerview.Value)*/
                        return;
                    //break;

                    case GameMode.MainGame:
                        if (!Settings.EnableSetting.Value) return;
                        break;
                }

                Settings.Logger.LogDebug($"Process({currentGameMode})");

                // この時点で ThisOutfitData 未生成のケースがあり、ここで生成される 
                ThisOutfitDataProcess();

#if KK
                if (ThisOutfitData.Heroine != null && ThisOutfitData.Heroine.isTeacher && !Settings.TeacherDress.Value)
                {
                    Settings.Logger.LogDebug("Teacher is excluded by TeacherDress setting");
                    return;
                }
#endif

#if false
            if (GameMode.Maker == currentGameMode)
            {
                ThisOutfitData.firstpass = true;
                ThisOutfitData.Chafile = MakerAPI.LastLoadedChaFile;

                if (Settings.ResetMaker.Value)
                {
                    OutfitDecider.ResetDecider();
                }
            }
#endif

                if (ThisOutfitData.firstpass) //Save all accessories to avoid duplicating head accessories each load and be reuseable
                {
                    Settings.Logger.LogDebug("Do firstpass: " + ThisOutfitData.Chafile.GetFancyCharacterName());

                    ThisOutfitData.Clear_Firstpass();
                    ThisOutfitData.Reset_Firstpass();

                    #region ACI Data
#if false // Additional_Card_Info 廃止予定 
                var ACI_data = new Additional_Card_Info.DataStruct();

                for (int i = 0, n = ThisOutfitData.Outfit_Size; i < n; i++)
                {
                    ACI_data.Createoutfit(i);
                }

                var Cosplay_Academy_Ready = false;
                var Required_Support = ExtendedSave.GetExtendedDataById(ThisOutfitData.Chafile, "Additional_Card_Info");
                if (Required_Support != null)
                {
                    switch (Required_Support.version)
                    {
                        case 0:
                            Additional_Card_Info.Migrator.MigrateV0(Required_Support, ref ACI_data);
                            break;
                        case 1:
                            if (Required_Support.data.TryGetValue("CardInfo", out var ByteData) && ByteData != null)
                            {
                                ACI_data.CardInfo = MessagePackSerializer.Deserialize<Additional_Card_Info.Cardinfo>((byte[])ByteData);
                            }
                            if (Required_Support.data.TryGetValue("CoordinateInfo", out ByteData) && ByteData != null)
                            {
                                ACI_data.CoordinateInfo = MessagePackSerializer.Deserialize<Dictionary<int, Additional_Card_Info.CoordinateInfo>>((byte[])ByteData);
                            }
                            break;
                        default:
                            Settings.Logger.LogWarning("New version of Additional Card Info found, please update");
                            break;
                    }
                }
                var CardInfo = ACI_data.CardInfo;
                var CoordinateInfo = ACI_data.CoordinateInfo;

                ClothingLoader.CardInfo = CardInfo;
                Cosplay_Academy_Ready = CardInfo.CosplayReady;
                ClothingLoader.MakeUpKeep = CoordinateInfo.ToDictionary(x => x.Key, x => x.Value.MakeUpKeep);
                ClothingLoader.CharacterClothingKeep_Coordinate = CoordinateInfo.ToDictionary(x => x.Key, x => x.Value.CoordinateSaveBools);
#endif
                    #endregion
                    ThisOutfitData.firstpass = false;
                }

                if (ChaControl.sex == 1)//run the following if female
                {
                    if (currentGameMode == GameMode.MainGame && !ThisOutfitData.processed /*|| Settings.ChangeOutfit.Value && GameMode.Maker == currentGameMode*/)
                    {
                        Settings.Logger.LogDebug("Processing: " + ThisOutfitData.Chafile.GetFancyCharacterName());
                        OutfitDecider.Decision(ChaControl.fileParam.fullname, ThisOutfitData);//Generate outfits
                        ThisOutfitData.processed = true;
                    }
                    var HoldOutfit = ChaControl.fileStatus.coordinateType; //requried for Cutscene characters to wear correct outfit such as sakura's first cutscene
                    ThisOutfitData.ClothingLoader.FullLoad(ChaControl, ChaFileControl);
                    ChaControl.fileStatus.coordinateType = HoldOutfit;
                    var temp = (ChaInfo)ChaControl;
                    var next = (ChaFileDefine.CoordinateType)temp.fileStatus.coordinateType;
#if true // コーデタイプ切り替え実験 
                    switch (next)
                    {
#if KK
                        case ChaFileDefine.CoordinateType.Gym:
#endif
                        case ChaFileDefine.CoordinateType.Swim:
                        case ChaFileDefine.CoordinateType.Pajamas:
                            break;

                        default:
                            next = GameEvent.NextCoordType;
                            break;
                    }
#endif
                    Settings.Logger.LogDebug("next Coord type: " + next);
                    ChaControl.ChangeCoordinateType(next, true); //forces cutscene characters to use outfits
                }
            }
            catch(Exception e)
            {
                Settings.Logger.LogError("CharaEvent.Process() error " + e);

            }
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
        {
#if false
            if (!Settings.AccKeeper.Value)
            {
                return;
            }//if disabled don't run
#endif
            if (ThisOutfitData == null)
            {
                Settings.Logger.LogWarning("ThisOutfitData not ready");
                return;
            }

            ClothingLoader.CoordinateLoad(coordinate, ChaControl);
        }
    }
}
