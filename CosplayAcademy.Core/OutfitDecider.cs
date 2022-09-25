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

                    if (Settings.EnableSets.Value)
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

                        var isset = false;

                        outfitData[sets].Insert(selectedfolder.GetAllCards(), isset);
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
            }
            else
            {
                OutfitData.Anger = false;
            }
            for (var i = 0; i < Constants.InputStrings.Length; i++)
            {
                Generalized_Assignment(false, i, i);
            }

            SpecialProcess();
            if (person != null)
            {
                Settings.Logger.LogDebug(name + " is processed.");
            }
        }

        private static void Generalized_Assignment(bool uniform_type, int Path_Num, int Data_Num)
        {
            var status = ThisOutfitData.ChaControl.fileParam;
            switch (Settings.H_EXP_Choice.Value)
            {
                case Hexp.RandConstant:
                    ThisOutfitData.alloutfitpaths[Path_Num] = outfitData[Data_Num].Random(uniform_type, false, status.personality, status.attribute, ThisOutfitData.ChaControl.GetBustCategory(), ThisOutfitData.ChaControl.GetHeightCategory());
                    break;
                case Hexp.Maximize:
                    ThisOutfitData.alloutfitpaths[Path_Num] = outfitData[Data_Num].Random(uniform_type, false, status.personality, status.attribute, ThisOutfitData.ChaControl.GetBustCategory(), ThisOutfitData.ChaControl.GetHeightCategory());
                    break;
                default:
                    ThisOutfitData.alloutfitpaths[Path_Num] = outfitData[Data_Num].RandomSet(uniform_type, false, status.personality, status.attribute, ThisOutfitData.ChaControl.GetBustCategory(), ThisOutfitData.ChaControl.GetHeightCategory());
                    break;
            }
        }
    }
}


