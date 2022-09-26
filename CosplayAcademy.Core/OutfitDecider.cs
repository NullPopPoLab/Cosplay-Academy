using System.Collections.Generic;
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
            outfitData = new OutfitData[Constants.GameCoordinateSize];
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
            var path = Settings.CoordinatePath.Value + Constants.CoordinateRoles[0];
            var plen = path.Length;

            var folders = new List<string>();
            var f1 = DataStruct.DefaultFolder[0];
            var l1 = f1.GetSubFolders();
            for (var i = 0; i < l1.Count; ++i)
            {
                var fn = l1[i].FolderPath.Substring(plen+1);
                switch(fn[0]){
                    case '_': case '!': break;

                    default:
                        folders.Add(fn);
                        break;
				}
            }

            for (int sets = 0, setslen = Constants.GameCoordinateSize; sets < setslen; sets++)
            {
                var order = Constants.SpecificCategories[sets];
                if(order==""){
                    // ランダムカテゴリから選択 
                    order = folders[UnityEngine.Random.Range(0, folders.Count)];
				}

                var f2 = f1.SelectSubFolder(path + sep + order);
                if (f2 == null)
                {
                    Settings.Logger.LogDebug($"Selected folder for set {sets}: {order}: -- not found --");
                    continue;
                }

                    if (outfitData[sets].IsSet())//Skip set items
                    {
                        continue;
                    }

                    if (Settings.EnableSets.Value)
                    {
                        var AllFolder = f2.GetAllFolders();

                        Grabber(ref AllFolder, sets, 0);

                        if (AllFolder.Count == 0)
                        {
                            outfitData[sets].Insert(new List<CardData>(), false);
                            continue;
                        }

                        var selectedfolder = AllFolder[UnityEngine.Random.Range(0, AllFolder.Count)];

                        Settings.Logger.LogDebug($"Selected folder for set {sets}: {order}: {selectedfolder.FolderPath}");

                        var isset = false;

                        outfitData[sets].Insert(selectedfolder.GetAllCards(), isset);
                        continue;
                    }
                    var cards = f2.GetAllCards();
                    cards.AddRange(Grabber(sets));
                    outfitData[sets].Insert(cards, false);
            }
        }
        private static List<CardData> Grabber(int sets)
        {
        #if false // 再検討 
#if KK
            if (Settings.GrabSwimsuits.Value && sets == 4)
            {
                return DataStruct.DefaultFolder[3].FolderData[0].GetAllCards();
            }
            if (Settings.GrabUniform.Value && sets == 1)
            {
                return DataStruct.DefaultFolder[0].FolderData[0].GetAllCards();
            }

#endif
#if KKS
            if (Settings.GrabSwimsuits.Value && sets == 1)
            {
                return DataStruct.DefaultFolder[8].FolderData[hstate].GetAllCards();
            }
#endif
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
            for (var i = 0; i < Constants.GameCoordinateSize; i++)
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
            var bust = ThisOutfitData.ChaControl.GetBustCategory();
            var height = ThisOutfitData.ChaControl.GetHeightCategory();
            var src = outfitData[Data_Num];
            if (src == null)
            {
                Settings.Logger.LogWarning($"Generalized_Assignment: uniform={uniform_type} pn={Path_Num} dn={Data_Num} is null");
                ThisOutfitData.alloutfitpaths[Path_Num] = null;
                return;
            }

            Settings.Logger.LogDebug($"Generalized_Assignment: uniform={uniform_type} pn={Path_Num} dn={Data_Num} bust={bust} height={height}");

            switch (Settings.H_EXP_Choice.Value)
            {
                case Hexp.RandConstant:
                    ThisOutfitData.alloutfitpaths[Path_Num] = src.Random(uniform_type, false, status.personality, status.attribute, bust, height);
                    break;
                case Hexp.Maximize:
                    ThisOutfitData.alloutfitpaths[Path_Num] = src.Random(uniform_type, false, status.personality, status.attribute, bust, height);
                    break;
                default:
                    ThisOutfitData.alloutfitpaths[Path_Num] = src.RandomSet(uniform_type, false, status.personality, status.attribute, bust, height);
                    break;
            }
        }
    }
}


