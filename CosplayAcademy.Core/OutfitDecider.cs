﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cosplay_Academy
{
    public static partial class OutfitDecider
    {
        private readonly static char sep = Path.DirectorySeparatorChar;
        private static readonly OutfitData[] outfitData;

        private static ChaDefault ThisOutfitData;
        private static int HExperience;
        private static int RandHExperience;

        static OutfitDecider()
        {
            outfitData = new OutfitData[Constants.InputStrings.Length];
            for (int i = 0, n = outfitData.Length; i < n; i++)
            {
                outfitData[i] = new OutfitData();
            }
        }

        public static void ResetDecider()
        {
            foreach (var data in outfitData)
            {
                data.Clear();
            }
            foreach (var item in CharaEvent.ChaDefaults)
            {
                item.processed = false;
            }
#if KK
            ChaDefault.LastClub = -1;
#endif
            OutfitData.Anger = false;
            Get_Outfits();
            foreach (var data in outfitData)
            {
                data.Coordinate();
            }
        }

        public static void Get_Outfits()
        {
            for (int sets = 0, setslen = Constants.InputStrings.Length; sets < setslen; sets++)
            {
                FolderData overridefolder = null;
                    var hstatefolder = DataStruct.DefaultFolder[sets].FolderData[0];

                    if (Settings.ListOverrideBool[sets].Value)
                    {
                        var overridepath = Settings.ListOverride[sets].Value;
                        var find = hstatefolder.GetAllFolders().Find(x => x.FolderPath == overridepath);
                        if (find == null)
                        {
                            if (overridefolder == null)
                            {
                                overridefolder = new FolderData(overridepath);
                            }
                            find = overridefolder;
                        }
                        var Overridecards = find.GetAllCards();
                        outfitData[sets].Insert(Overridecards, Overridecards.Count > 0);//assign "is" set and store data
                        continue;
                    }

                    if (outfitData[sets].IsSet(0))//Skip set items
                    {
                        continue;
                    }

                    if (Settings.EnableSets.Value && Settings.MatchGeneric[sets].Value)
                    {
                        var AllFolder = hstatefolder.GetAllFolders();

                        Grabber(ref AllFolder, sets, 0);

                        if (AllFolder.Count == 0)
                        {
                            outfitData[sets].Insert(new List<CardData>(), false);
                            continue;
                        }

                        var selectedfolder = AllFolder[UnityEngine.Random.Range(0, AllFolder.Count)];

                        //Settings.Logger.LogWarning($"Selected folder for {Constants.InputStrings[sets]}/{Constants.InputStrings2[hstate]}: {selectedfolder.FolderPath}");

                        var isset = selectedfolder.FolderPath.Contains($"{sep}Sets{sep}");

                        outfitData[sets].Insert(selectedfolder.GetAllCards(), isset);

                        if (!Settings.IndividualSets.Value && isset)
                        {
                            Setsfunction(selectedfolder);
                        }
                        continue;
                    }
                    var cards = hstatefolder.GetAllCards();
                    cards.AddRange(Grabber(sets, 0));
                    outfitData[sets].Insert(cards, false);
                overridefolder = null;
            }
        }
        private static List<CardData> Grabber(int sets, int hstate)
        {
#if KK
            if (Settings.GrabSwimsuits.Value && sets == 4)
            {
                return DataStruct.DefaultFolder[3].FolderData[hstate].GetAllCards();
            }
            if (Settings.GrabUniform.Value && sets == 1)
            {
                return DataStruct.DefaultFolder[0].FolderData[hstate].GetAllCards();
            }

#endif
#if KKS
            if (Settings.GrabSwimsuits.Value && sets == 1)
            {
                return DataStruct.DefaultFolder[8].FolderData[hstate].GetAllCards();
            }
#endif

            return new List<CardData>();
        }

        public static void Decision(string name, ChaDefault cha)
        {
            ThisOutfitData = cha;
            var person = ThisOutfitData.heroine;
            if (person != null)
            {
#if KK
                OutfitData.Anger = person.isAnger;
#endif
                HExperience = (int)person.HExperience;
            }
            else
            {
                OutfitData.Anger = false;
                HExperience = 0;
            }
            RandHExperience = UnityEngine.Random.Range(0, HExperience + 1);
            for (var i = 0; i < Constants.InputStrings.Length; i++)
            {
                Generalized_Assignment(Settings.MatchGeneric[i].Value, i, i);
            }

            SpecialProcess();
            if (person != null)
            {
                Settings.Logger.LogDebug(name + " is processed.");
            }
        }

        private static void Setsfunction(FolderData folderData)
        {
            var sep = Path.DirectorySeparatorChar;
            var split = sep + folderData.FolderPath.Split(new string[] { sep + "Sets" + sep }, System.StringSplitOptions.RemoveEmptyEntries).Last();
            for (int sets = 0, n = outfitData.Length; sets < n; sets++)
            {
                for (var hexp = 0; hexp < 4; hexp++)
                {
                    if (Settings.FullSet.Value && outfitData[sets].IsSet(hexp) || Settings.ListOverrideBool[sets].Value)
                    {
                        continue;
                    }
                    var find = DataStruct.DefaultFolder[sets].FolderData[hexp].GetAllFolders().Find(x => x.FolderPath.EndsWith(split));
                    if (find == null)
                    {
                        continue;
                    }
                    var temp = find.GetAllCards();
                    outfitData[sets].Insert(find.GetAllCards(), true);
                }
            }
        }

        private static void Generalized_Assignment(bool uniform_type, int Path_Num, int Data_Num)
        {
            var status = ThisOutfitData.ChaControl.fileParam;
            switch (Settings.H_EXP_Choice.Value)
            {
                case Hexp.RandConstant:
                    ThisOutfitData.Hvalue = RandHExperience;
                    ThisOutfitData.alloutfitpaths[Path_Num] = outfitData[Data_Num].Random(uniform_type, false, status.personality, status.attribute, ThisOutfitData.ChaControl.GetBustCategory(), ThisOutfitData.ChaControl.GetHeightCategory());
                    break;
                case Hexp.Maximize:
                    ThisOutfitData.Hvalue = HExperience;
                    ThisOutfitData.alloutfitpaths[Path_Num] = outfitData[Data_Num].Random(uniform_type, false, status.personality, status.attribute, ThisOutfitData.ChaControl.GetBustCategory(), ThisOutfitData.ChaControl.GetHeightCategory());
                    break;
                default:
                    ThisOutfitData.Hvalue = UnityEngine.Random.RandomRangeInt(0, HExperience + 1);
                    ThisOutfitData.alloutfitpaths[Path_Num] = outfitData[Data_Num].RandomSet(ThisOutfitData.Hvalue, uniform_type, false, status.personality, status.attribute, ThisOutfitData.ChaControl.GetBustCategory(), ThisOutfitData.ChaControl.GetHeightCategory());
                    break;
            }
        }
    }
}


