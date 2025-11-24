using ActionGame;
using ActionGame.Chara;
using ActionGame.Communication;
using Extensions;
using HarmonyLib;
using Illusion.Extensions;
using Manager;
using System;
using System.Reflection;

namespace CosplayParty
{
    internal static class Hooks
    {
        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

#if KK
        //private static void ShowTypeInfo(Type t)
        //{
        //    Settings.Logger.LogDebug($"Name: {t.Name}");
        //    Settings.Logger.LogDebug($"Full Name: {t.FullName}");
        //    Settings.Logger.LogDebug($"ToString:  {t}");
        //    Settings.Logger.LogDebug($"Assembly Qualified Name: {t.AssemblyQualifiedName}");
        //    Settings.Logger.LogDebug("");
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaitPoint), nameof(WaitPoint.SetWait))]
        internal static void ChangeOutfitAtWaitPoint(WaitPoint __instance)
        {
            try
            {
                var Chara = (Base)Traverse.Create(__instance).Property("chara").GetValue();
#if false // extra outfits test 
                if (Chara != null){
                    var didx = Chara.heroine.isDresses.Check(false);
                    if (didx < 0) didx = 0;
                    var ofc = Chara.chaCtrl.chaFile.coordinate.Length;
                    Chara.heroine.coordinates[didx] = ofc - 1;
                }
#endif
                if (Chara == null || Chara.chaCtrl == null || Chara.heroine == null || !Settings.StoryModeChange.Value || Chara.heroine.isTeacher)
                {
                    return;
                }
                var ThisOutfitData = CharaEvent.ChaDefaults.Find(x => x.Parameter.Compare(Chara.chaCtrl.fileParam));
                if (ThisOutfitData == null || !ThisOutfitData.IsProcessed)
                {
                    return;
                }
                var heroine = Chara.heroine;
                //ChaFileParameter ChaPara = Chara.chaCtrl.fileParam;
                //var ThisOutfitData = CharaEvent.ChaDefaults.Find(x => ChaPara.personality == x.Personality && x.FullName == ChaPara.fullname && x.BirthDay == ChaPara.strBirthDay);
                //if (ThisOutfitData == null || !ThisOutfitData.IsProcessed)
                //{
                //    return;
                //}
                ThisOutfitData.Heroine = heroine;

#if false // for KoiChance 
                if (__instance.MapNo == 46)
                {
                    if (ThisOutfitData.ChangeKoiToClub)
                    {
#if false
                        ThisOutfitData.outfitpaths[4] = ThisOutfitData.ClubOutfitPath;
                        ThisOutfitData.Outfits[4].Outer.Load(ThisOutfitData.outfitpaths[4]);
                        ThisOutfitData.ClothingLoader.GeneralizedLoad(4);
                        ThisOutfitData.ChangeKoiToClub = false;
                        ThisOutfitData.ClothingLoader.Run_Repacks(ThisOutfitData.ChaControl);
                        ThisOutfitData.ClothingLoader.Reload_RePacks(ThisOutfitData.ChaControl, true);
#endif
                        Settings.Logger.LogDebug($"do ChangeKoiToClub: {Chara.name} map:{__instance.MapNo}");
                        Chara.chaCtrl.ChangeCoordinateTypeAndReload(ChaFileDefine.CoordinateType.Club);
                        ThisOutfitData.ChangeKoiToClub = false;
                    }
                }
                else if (ThisOutfitData.ChangeClubToKoi && __instance.MapNo == 22)
                {
#if false
                    ThisOutfitData.ClubOutfitPath = ThisOutfitData.outfitpaths[4];
                    ThisOutfitData.outfitpaths[4] = ThisOutfitData.KoiOutfitpath;
#endif
                    var num = heroine.isDresses.Check(false);
                    if (num == -1)
                    {
                        num = 0;
                    }
#if false
                    heroine.coordinates[num] = 4;
                    ThisOutfitData.Outfits[4].Outer.Load(ThisOutfitData.outfitpaths[4]);
                    ThisOutfitData.ClothingLoader.GeneralizedLoad(4);
                    ThisOutfitData.ChangeClubToKoi = false;
                    ThisOutfitData.ClothingLoader.Run_Repacks(ThisOutfitData.ChaControl);
                    ThisOutfitData.ClothingLoader.Reload_RePacks(ThisOutfitData.ChaControl, true);
#endif
                    Settings.Logger.LogDebug($"do ChangeClubToKoi: {Chara.name} map:{__instance.MapNo}");
                    Chara.chaCtrl.ChangeCoordinateTypeAndReload(ChaFileDefine.CoordinateType.Club);
                    //Chara.chaCtrl.SetAccessoryStateAll(true);
                    ThisOutfitData.ChangeClubToKoi = false;
                }
                else if (ThisOutfitData.ChangeKoiToClub && __instance.MapNo != 22)
                {
                    var remainThreshold = (heroine.lewdness / (4 - (int)heroine.HExperience));
                    if (UnityEngine.Random.Range(0, 101) >= remainThreshold)
                    {
#if false
                        ThisOutfitData.outfitpaths[4] = ThisOutfitData.ClubOutfitPath;
                        var num = heroine.isDresses.Check(false);
                        if (num == -1)
                        {
                            num = 0;
                        }
                        heroine.coordinates[num] = 4;
                        ThisOutfitData.Outfits[4].Outer.Load(ThisOutfitData.outfitpaths[4]);
                        ThisOutfitData.ClothingLoader.GeneralizedLoad(4);
                        ThisOutfitData.ClothingLoader.Run_Repacks(ThisOutfitData.ChaControl);
                        ThisOutfitData.ClothingLoader.Reload_RePacks(ThisOutfitData.ChaControl, true);
#endif
                        Settings.Logger.LogDebug($"do ChangeKoiToClub: {Chara.name} map:{__instance.MapNo}");
                        ThisOutfitData.ChaControl.ChangeCoordinateTypeAndReload(ChaFileDefine.CoordinateType.Club);
                        //ThisOutfitData.ChaControl.SetAccessoryStateAll(true);
                    }
                    ThisOutfitData.ChangeKoiToClub = false;
                }
                //ExpandedOutfit.Logger.LogDebug($"SetWait2 success: {Chara.chaCtrl.fileParam.fullname} is waiting at {Chara.mapNo}");
#endif
            }
            catch (Exception ex)
            {
                Settings.Logger.LogError("ChangeOutfitAtWaitPoint fail - " + ex);
            }
        }

        //[HarmonyPatch]
        //static class FirstActionPatch
        //{
        //    public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("ActionGame.ActionControl+DesireInfo, Assembly-CSharp"), "FirstAction",new Type[] { typeof(SaveData.Heroine),AccessTools.TypeByName("ActionGame.ActionControl+DesireInfo, Assembly-CSharp")});//Assembly Name because it hates me now that I didn't want to use it
        //    static void Prefix(int _mapNo, NPC _npc, ActionControl __instance, SaveData.Heroine _heroine)
        //    {
        //        if (_mapNo == 22)
        //        {

        //        }
        //    }
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NPC), nameof(NPC.ReStart))]
        internal static void NPCRestart(NPC __instance)
        {
            try
            {
                var ThisOutfitData = CharaEvent.ChaDefaults.Find(x => x.Parameter.Compare(__instance.chaCtrl.fileParam));
#if false // おそらく廃止 
                if (ThisOutfitData == null || ThisOutfitData.processed || __instance.heroine.isTeacher || !Settings.StoryModeChange.Value)
                {
                    if (Settings.StoryModeChange.Value && Settings.ChangeToClubatKoi.Value && __instance.mapNo == 22)
                    {
                        __instance.chaCtrl.ChangeCoordinateTypeAndReload(ChaFileDefine.CoordinateType.Club);
                        __instance.heroine.coordinates[0] = 4;
                    }
                    return;
                }
                ThisOutfitData.ChangeKoiToClub = false;
                ThisOutfitData.ChangeClubToKoi = false;
                if (__instance.mapNo == 22 && UnityEngine.Random.Range(1, 101) <= Settings.KoiChance.Value)
                {
                    ThisOutfitData.ClubOutfitPath = ThisOutfitData.outfitpaths[4];
                    ThisOutfitData.outfitpaths[4] = ThisOutfitData.KoiOutfitpath;
                    ThisOutfitData.ClothingLoader.GeneralizedLoad(4, ThisOutfitData.outfitpaths[4].EndsWith(".png"));
                    __instance.heroine.coordinates[0] = 4;
                    ThisOutfitData.SkipFirstPriority = ThisOutfitData.ChangeKoiToClub = true;
                    ThisOutfitData.ClothingLoader.Reload_RePacks(__instance.chaCtrl, true);
                    __instance.chaCtrl.ChangeCoordinateTypeAndReload(ChaFileDefine.CoordinateType.Club);
                    //__instance.chaCtrl.SetAccessoryStateAll(true);
                    //ExpandedOutfit.Logger.LogError(__instance.chaCtrl.fileParam.fullname + " Action NO: " + __instance.AI.actionNo + " " + ThisOutfitData.heroine.clubActivities + " " + ThisOutfitData.heroine.coordinates.Length);
                }
#endif
            }
            catch (Exception ex)
            {

                Settings.Logger.LogError("ReStart fail - " + ex);
            }
            //change NPC's who start at club room to a koi outfit
        }

#if false // おそらく廃止 
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.SetState))]
        internal static void LoadSethook(HSceneProc __instance)
        {
            if (__instance.flags.isFreeH)
                CharaEvent.FreeHHeroines = __instance.flags.lstHeroine;
        }
#endif
#endif

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateType", new Type[]
        {
            typeof(bool)
        })]
        private static bool ChangeCoordinateTypePrefix(ChaControl __instance, bool changeBackCoordinateType)
        {
            Settings.Logger.LogDebug($"{__instance?.chaFile?.parameter.fullname}.ChangeCoordinateTypePrefix({changeBackCoordinateType})");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateType", new Type[]
        {
            typeof(ChaFileDefine.CoordinateType),
            typeof(bool)
        })]
        private static bool ChangeCoordinateTypePrefix(ChaControl __instance, ref ChaFileDefine.CoordinateType type, bool changeBackCoordinateType)
        {
            Settings.Logger.LogDebug($"{__instance?.chaFile?.parameter.fullname}.ChangeCoordinateTypePrefix({type},{changeBackCoordinateType})");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateTypeAndReload", new Type[]
        {
            typeof(bool)
        })]
        private static bool ChangeCoordinateTypeAndReloadPrefix(ChaControl __instance, bool changeBackCoordinateType)
        {
            Settings.Logger.LogDebug($"{__instance?.chaFile?.parameter.fullname}.ChangeCoordinateTypeAndReloadPrefix({changeBackCoordinateType})");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateTypeAndReload", new Type[]
        {
            typeof(ChaFileDefine.CoordinateType),
            typeof(bool)
        })]
        private static bool ChangeCoordinateTypeAndReloadPrefix(ChaControl __instance, ref ChaFileDefine.CoordinateType type, bool changeBackCoordinateType)
        {
            Settings.Logger.LogDebug($"{__instance?.chaFile?.parameter.fullname}.ChangeCoordinateTypeAndReloadPrefix({type},{changeBackCoordinateType})");
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NPC), "SynchroCoordinate")]
        private static void SynchroCoordinatePostfix(NPC __instance, bool isRemove)
        {
            Settings.Logger.LogDebug($"{__instance?.charaData?.Name}.SynchroCoordinatePostfix({isRemove})");
        }

#if false
        [HarmonyPatch]
        static class SetNextOutfitAtMove
        {
            public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("ActionGame.ActionControl+DesireInfo, Assembly-CSharp"), "SetWaitPoint");//Assembly Name because it hates me now that I didn't want to use it
            internal static void Postfix(WaitPoint wp, NPC _npc)
            {
                try
                {
#if false // extra outfits test 
                    if (_npc != null)
                    {
                        var didx = _npc.heroine.isDresses.Check(false);
                        if (didx < 0) didx = 0;
                        var ofc = _npc.chaCtrl.chaFile.coordinate.Length;
                        _npc.heroine.coordinates[didx] = ofc-1;
                    }
#endif

#if false // for KoiChance 
                    if (wp == null || _npc == null || !Settings.StoryModeChange.Value)
                    {
                        return;
                    }
                    var actScene = Singleton<Game>.Instance.actScene;
                    if (actScene != null && actScene.actCtrl != null)
                    {
                        //ChaFileParameter ChaPara = _npc.chaCtrl.fileParam;
                        //var ThisOutfitData = CharaEvent.ChaDefaults.Find(x => ChaPara.personality == x.Personality && x.FullName == ChaPara.fullname && x.BirthDay == ChaPara.strBirthDay);
                        //if (ThisOutfitData == null)
                        //{
                        //    return;
                        //}
                        var ThisOutfitData = CharaEvent.ChaDefaults.Find(x => x.Parameter.Compare(_npc.chaCtrl.fileParam));
                        if (ThisOutfitData == null) return;

                        if (wp.MapNo == 22 && _npc.mapNo != 22) //characters who walk to clubroom should be expected to change to koioutfit maybe.
                        {
                            if (ThisOutfitData.Changestate)
                            {
                                Settings.Logger.LogDebug($"do Changestate: {_npc.name} map:{_npc.mapNo}=>{wp.MapNo}");
                                ThisOutfitData.Changestate = false;
                                return;
                            }
                            //var tempcoord = ThisOutfitData.heroine.coordinates.ToList();
                            //var tempdress = ThisOutfitData.heroine.isDresses.ToList();
                            //tempcoord.Add(4);
                            //tempdress.Add(false);
                            //ThisOutfitData.heroine.coordinates = tempcoord.ToArray();
                            //ThisOutfitData.heroine.isDresses = tempdress.ToArray();
                            //actCtrl.SetDesire(0, ThisOutfitData.heroine, 100);
                            //ExpandedOutfit.Logger.LogWarning($"{_npc.chaCtrl.fileParam.fullname} is heading to club room...probably");
                            //if (UnityEngine.Random.Range(1, 101) <= Settings.KoiChance.Value)
                            {
                                Settings.Logger.LogDebug($"set ChangeClubToKoi: {_npc.name} map:{_npc.mapNo}=>{wp.MapNo}");
                                ThisOutfitData.ChangeClubToKoi = true;
                            }
                        }
                        else if (_npc.mapNo == 22 && wp.MapNo == 46)
                        {
                            Settings.Logger.LogDebug($"set ChangeKoiToClub: {_npc.name} map:{_npc.mapNo}=>{wp.MapNo}");
                            ThisOutfitData.ChangeKoiToClub = true;
                        }
                        else if (_npc.mapNo == 22 && wp.MapNo != 22)
                        {
                            Settings.Logger.LogDebug($"set ChangeKoiToClub: {_npc.name} map:{_npc.mapNo}=>{wp.MapNo}");
                            ThisOutfitData.ChangeKoiToClub = true;
                            //var tempcoord = ThisOutfitData.heroine.coordinates.ToList();
                            //var tempdress = ThisOutfitData.heroine.isDresses.ToList();
                            //tempcoord.Add(4);
                            //tempdress.Add(false);
                            //ThisOutfitData.heroine.coordinates = tempcoord.ToArray();
                            //ThisOutfitData.heroine.isDresses = tempdress.ToArray();
                            //actCtrl.SetDesire(0, ThisOutfitData.heroine, 100);
                            //ThisOutfitData.ChangeKoiToClub = true;

                        }
                }
#endif
                }
                catch (Exception ex)
                {
                    Settings.Logger.LogError("SetWaitPoint fail - " + ex);
                }
            }
        }
#endif
    }
}
