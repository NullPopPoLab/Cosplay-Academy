using System.Collections.Generic;

namespace CosplayParty
{
    public static partial class OutfitDecider
    {
        private static void Grabber(ref List<FolderData> temp2, int set, int exp)
        {
#if false // 再検討 
            if (set == 1 && Settings.GrabUniform.Value)
            {
                temp2.AddRange(DataStruct.DefaultFolder[set].FolderData[exp].GetAllFolders());
                return;
            }
            if (set == 4 && Settings.GrabSwimsuits.Value)
            {
                temp2.AddRange(DataStruct.DefaultFolder[set].FolderData[exp].GetAllFolders());
                return;
            }
#endif
        }

        private static void SpecialProcess()
        {
#if false // 再検討; 特殊場面用 
            ThisOutfitData.KoiOutfitpath = outfitData[9].RandomSet(Settings.MatchGeneric[9].Value, false, ThisOutfitData.ChaControl.fileParam.personality, ThisOutfitData.Chafile.parameter.attribute, ThisOutfitData.ChaControl.GetBustCategory(), ThisOutfitData.ChaControl.GetHeightCategory()).GetFullPath();

            if (!Settings.MatchGeneric[1].Value)
            {
                ThisOutfitData.alloutfitpaths[1] = ThisOutfitData.alloutfitpaths[0];
            }

            //If Characters can use casual outfits after school
            if (Settings.AfterSchoolCasual.Value)
            {
                if (UnityEngine.Random.Range(1, 101) <= Settings.AfterSchoolcasualchance.Value)
                {
                    ThisOutfitData.alloutfitpaths[1] = ThisOutfitData.alloutfitpaths[10];//assign casual outfit to afterschool
                }
            }
#endif
        }
    }
}


