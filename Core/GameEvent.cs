using ActionGame;
using KKAPI.MainGame;
using UnityEngine;

namespace CosplayParty
{
    public class GameEvent : GameCustomFunctionController
    {
        public static Cycle.Type CurrentPeriod;
        public static ChaFileDefine.CoordinateType NextCoordType;

        protected override void OnPeriodChange(Cycle.Type period)
        {
            Settings.Logger.LogDebug($"OnPeriodChange({period})");

            CurrentPeriod = period;

            var changing = false;

            switch (period)
            {
#if KK
                case Cycle.Type.Morning:
                    NextCoordType = ChaFileDefine.CoordinateType.School01;
                    break;

                case Cycle.Type.LunchTime:
                    NextCoordType = ChaFileDefine.CoordinateType.School01;
                    break;

                case Cycle.Type.StaffTime:
                    NextCoordType = ChaFileDefine.CoordinateType.Club;
                    break;

                case Cycle.Type.AfterSchool:
                    NextCoordType = ChaFileDefine.CoordinateType.School02;
                    break;

                case Cycle.Type.GotoMyHouse:
                    NextCoordType = ChaFileDefine.CoordinateType.School02;
                    break;
#endif
#if KKS
                case Cycle.Type.Morning:
                    OutfitDecider.SelectByPeriod = 0;
                    break;
                case Cycle.Type.Daytime:
                    OutfitDecider.SelectByPeriod = 1;
                    break;
                case Cycle.Type.Evening:
                    OutfitDecider.SelectByPeriod = 2;
                    break;
                case Cycle.Type.Night:
                    OutfitDecider.SelectByPeriod = 3;
                    break;
                case Cycle.Type.MyHouse:
                    OutfitDecider.SelectByPeriod = 4;
                    break;
                default:
                    OutfitDecider.SelectByPeriod = -1;
                    break;
#endif
            }

            switch (Settings.UpdateFrequency.Value)
            {
                case OutfitUpdate.EveryPeriod:
#if KK
                    // 幕間会話シーンでの変更指定 
                    switch (period)
                    {
                        case Cycle.Type.GotoSchool:
                        case Cycle.Type.LunchTime:
                        case Cycle.Type.StaffTime:
                        case Cycle.Type.AfterSchool:
                        case Cycle.Type.GotoMyHouse:
                            changing = true;
                            break;
                    }
#else
                    changing = true;
#endif
                    break;

                case OutfitUpdate.Daily:
                    if (period == Cycle.Type.Morning) changing = true;
                    break;
            }

            if(changing)
            {
                Settings.Logger.LogDebug($"reset outfits CoordType={NextCoordType} SelectByPeriod={OutfitDecider.SelectByPeriod}");
                OutfitDecider.ResetDecider();
            }

#if false // for KoiChance 
            Settings.Logger.LogDebug($"set Changestate to All");
            foreach (var item in CharaEvent.ChaDefaults)
            {
                item.Changestate = true;
            }
#endif
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            Settings.Logger.LogDebug("OnDayChange");
            ChaDefault.NeedRefresh();
#if KK
            if ((Cycle.Week.Monday == day && Settings.UpdateFrequency.Value == OutfitUpdate.Weekly) || Cycle.Week.Holiday == day && Settings.SundayDate.Value)
            {
                OutfitDecider.ResetDecider();
            }
#endif
        }

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            CharaEvent.ChaDefaults.Clear();
            OutfitDecider.ResetDecider();
        }

        protected override void OnNewGame()
        {
            CharaEvent.ChaDefaults.Clear();
            OutfitDecider.ResetDecider();
        }
        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            Settings.Logger.LogDebug("OnStartH");

            if (Settings.EnableSetting.Value)
            {
                foreach (var Heroine in hFlag.lstHeroine)
                {
                    Heroine.chaCtrl.ChangeCoordinateTypeAndReload();
                }
            }
            CharaEvent.inH = true;

            base.OnStartH(proc, hFlag, vr);
        }

        protected override void OnEndH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            Settings.Logger.LogDebug("OnEndH");

            if (hFlag.isFreeH)
            {
                CharaEvent.ChaDefaults.Clear();
                OutfitDecider.ResetDecider();
            }
            CharaEvent.inH = false;

            base.OnEndH(proc, hFlag, vr);
        }
    }
}
