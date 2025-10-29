using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CosplayParty
{
    public static partial class OutfitDecider
    {
        //! フィルタリング情報 
        public class Filter
        {
            public string Order = "";
            public FolderData Folder;
        }

        //! コーデ役割別情報 
        public class Role
        {
            public string BaseDir = "";
            public OutfitData CoordSet = new OutfitData();
            public List<string> FolderList = new List<string>();
            public FolderData BaseFolder;

            public void Clear()
            {
                BaseDir = "";
                CoordSet.Clear();
                FolderList.Clear();
                BaseFolder = null;
            }
        }

        private readonly static char sep = Path.DirectorySeparatorChar;
        private static readonly Role[] roleSet = new Role[Constants.CoordinateRoles.Length];

        private static readonly Filter[] filterBySets = new Filter[Constants.GameCoordinateSize];

        private static ChaDefault ThisOutfitData;
        public static string SelectByPeriod = "";

        static OutfitDecider()
        {
            for (var role = 0; role < roleSet.Length; ++role)
            {
                roleSet[role] = new Role();
            }
            for (var seta = 0; seta < filterBySets.Length; ++seta)
            {
                filterBySets[seta] = new Filter();
            }
        }

        public static void ResetDecider()
        {
            for (var role = 0; role < roleSet.Length; ++role)
            {
                roleSet[role].Clear();
            }

            foreach (var item in CharaEvent.ChaDefaults)
            {
                item.processed = false;
            }
            Get_Outfits();

            for (var role = 0; role < roleSet.Length; ++role)
            {
                roleSet[role].CoordSet.Coordinate();
            }
        }

        public static void Get_Outfits()
        {

            if (DataStruct.DefaultFolder.Count < 1)
            {
                Settings.Logger.LogWarning($"{Settings.CoordinatePath.Value} not found");
                return;
            }

            // カード情報いちいちコーデチャネル別にスキャンし直さなくてもいいよね 
            // そんなわけでフィルタリングも後回し 
            for (var role = 0; role < roleSet.Length; ++role)
            {
                var f0 = DataStruct.DefaultFolder[role];
                var rt = roleSet[role];
                rt.BaseDir = Settings.CoordinatePath.Value + Constants.CoordinateRoles[role];
                var plen = rt.BaseDir.Length;
                rt.BaseFolder = f0.SelectSubFolder(rt.BaseDir);
                if (rt.BaseFolder == null)
                {
                    Settings.Logger.LogWarning($"{rt.BaseDir} not found");
                    continue;
                }

                var l1 = rt.BaseFolder.GetSubFolders();
                for (var i = 0; i < l1.Count; ++i)
                {
                    if (l1[i].FullDir.Length > plen)
                    {
                        var fn = l1[i].FullDir.Substring(plen + 1);
                        rt.FolderList.Add(fn);
                    }
                }

                var cards = rt.BaseFolder.GetAllCards();

                //Settings.Logger.LogDebug($"{cards.Count} cards found");
#if false // 廃止予定 
                cards.AddRange(Grabber(sets));
#endif
                rt.CoordSet.Replace(cards, false);
            }

            // コーデチャネル別にはフィルタリング情報だけもっとけばよさげ 
            // カード情報は↑で一括実行してもっとく 
            for (int sets = 0, setslen = Constants.GameCoordinateSize; sets < setslen; sets++)
            {
                var rt = roleSet[0];
                var ft = filterBySets[sets];

                var folders = rt.FolderList;
                ft.Order = Settings.SpecificCategories[sets].Value;
#if KKS
                if (sets == 0)
                {
                    // 私服は時間帯別選択優先 
                    if (SelectByPeriod != "")
                    {
                        ft.Order = SelectByPeriod;
                        Settings.Logger.LogDebug("code for set 0: " + ft.Order);
                    }
                }
#endif
                if (ft.Order == "" && Settings.RandomizeDresscode.Value)
                {
                    // 直下のフォルダをランダム選択 
                    // コーデは、そのフォルダ内から選択される 
                    ft.Order = folders[UnityEngine.Random.Range(0, folders.Count)];

                    Settings.Logger.LogDebug("code randomized: " + ft.Order);
                }

                var dir = rt.BaseDir;
                ft.Folder = rt.BaseFolder;
                if (ft.Order != "")
                {
                    dir += sep + ft.Order;
                    ft.Folder = ft.Folder.SelectSubFolder(dir);
                    if (ft.Folder == null)
                    {
                        Settings.Logger.LogDebug($"Selected folder for set {sets}: {dir}: -- not found --");
                        continue;
                    }
                }

                Settings.Logger.LogDebug($"for {sets}, select from: " + dir);
            }
        }
#if false // 廃止予定 
        private static List<CardData> Grabber(int sets)
        {
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

            return new List<CardData>();
        }
#endif

        public static void Decision(string name, ChaDefault cha)
        {
            ThisOutfitData = cha;
            var person = ThisOutfitData.heroine;
            if (person != null)
            {
                Settings.Logger.LogDebug($"Decision for {name}");
            }
            for (var i = 0; i < Constants.GameCoordinateSize; i++)
            {
                Generalized_Assignment(i);
            }

            SpecialProcess();
            if (person != null)
            {
                Settings.Logger.LogDebug(name + " is processed.");
            }
        }

        private static void Generalized_Assignment(int sets)
        {
            var status = ThisOutfitData.ChaControl.fileParam;
            var src = roleSet[0].CoordSet;
            if (src == null)
            {
                Settings.Logger.LogWarning($"Generalized_Assignment: CoordSet is null");
                ThisOutfitData.alloutfitpaths[sets] = null;
                return;
            }

            var ft = filterBySets[sets];

            var filter = new SpecialCoordFilter();
            filter.SubDir = ft.Order;
            filter.Unexclude = (ft.Folder == null) ? 0 : ft.Folder.SpecialType.Excluded;
#if KK
            filter.Angry = ThisOutfitData.heroine.isAnger;
            filter.Teacher = !ThisOutfitData.heroine.isStaff; // なんか思ってたんと逆らしい。 
#endif
            filter.Lewd = ThisOutfitData.heroine.lewdness >= 100;
            filter.HeightGrade = ThisOutfitData.ChaControl.GetHeightCategory();
            filter.BustGrade = ThisOutfitData.ChaControl.GetBustCategory();

            Settings.Logger.LogDebug($"Generalized_Assignment: sets:{sets} filter:{filter}");

            ThisOutfitData.alloutfitpaths[sets] = src.Random(filter);
#if false // 廃止予定 
            ThisOutfitData.alloutfitpaths[sets] = src.Random(uniform_type, false, status.personality, status.attribute, bust, height);
            switch (Settings.H_EXP_Choice.Value)
            {
                case Hexp.RandConstant:
                    ThisOutfitData.alloutfitpaths[sets] = src.Random(uniform_type, false, status.personality, status.attribute, bust, height);
                    break;
                case Hexp.Maximize:
                    ThisOutfitData.alloutfitpaths[sets] = src.Random(uniform_type, false, status.personality, status.attribute, bust, height);
                    break;
                default:
                    ThisOutfitData.alloutfitpaths[sets] = src.RandomSet(uniform_type, false, status.personality, status.attribute, bust, height);
                    break;
            }
#endif
        }
    }
}
