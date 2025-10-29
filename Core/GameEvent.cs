using ActionGame;
using KKAPI.MainGame;
using UnityEngine;

namespace CosplayParty
{
    public class GameEvent : GameCustomFunctionController
    {
        protected override void OnPeriodChange(Cycle.Type period)
        {
            var changing = false;

            switch (Settings.UpdateFrequency.Value)
            {
                case OutfitUpdate.EveryPeriod:
                    changing = true;
#if KK
                    // 幕間会話シーンでの変更指定 
                    switch (period)
                    {
                        case Cycle.Type.Morning:
                        case Cycle.Type.LunchTime:
                        case Cycle.Type.StaffTime:
                        case Cycle.Type.AfterSchool:
                        case Cycle.Type.GotoMyHouse:
                            changing = true;
                            break;

                        default:
                            changing = false;
                            break;
                    }
#endif
                    break;

                case OutfitUpdate.Daily:
                    if(period == Cycle.Type.Morning) changing = true;
                    break;
            }


            if(changing)
            {
                OutfitDecider.ResetDecider();

                // 時間帯別設定 
#if KKS
                switch (period)
                {
                    case Cycle.Type.Morning:
                        OutfitDecider.SelectByPeriod = Settings.SpecificCategoriesByPeriod[0].Value;
                        break;
                    case Cycle.Type.Daytime:
                        OutfitDecider.SelectByPeriod = Settings.SpecificCategoriesByPeriod[1].Value;
                        break;
                    case Cycle.Type.Evening:
                        OutfitDecider.SelectByPeriod = Settings.SpecificCategoriesByPeriod[2].Value;
                        break;
                    case Cycle.Type.Night:
                        OutfitDecider.SelectByPeriod = Settings.SpecificCategoriesByPeriod[3].Value;
                        break;
                    case Cycle.Type.MyHouse:
                        OutfitDecider.SelectByPeriod = Settings.SpecificCategoriesByPeriod[4].Value;
                        break;
                    default:
                        OutfitDecider.SelectByPeriod = "";
                        break;
                }
#endif
                Settings.Logger.LogDebug($"set for {period}: " + OutfitDecider.SelectByPeriod);
            }
            else{
                Settings.Logger.LogDebug($"skip at {period}");
            }

            foreach (var item in CharaEvent.ChaDefaults)
            {
                item.Changestate = true;
            }
        }

        protected override void OnDayChange(Cycle.Week day)
        {
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
