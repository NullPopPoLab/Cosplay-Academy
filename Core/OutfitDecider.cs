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
#if KKS
        private static readonly Filter[] filterByPeriods = new Filter[Settings.SpecificCategoriesByPeriod.Length];
#endif

        private static ChaDefault ThisOutfitData;
        public static int SelectByPeriod = -1;

        static OutfitDecider()
        {
            for (var role = 0; role < roleSet.Length; ++role)
            {
                roleSet[role] = new Role();
            }
            for (var sets = 0; sets < filterBySets.Length; ++sets)
            {
                filterBySets[sets] = new Filter();
            }
#if KKS
            for (var period = 0; period < filterByPeriods.Length; ++period)
            {
                filterByPeriods[period] = new Filter();
            }
#endif
        }

        public static void ResetOutfits()
        {
            Settings.Logger.LogDebug("ResetOutfits()");


            for (var role = 0; role < roleSet.Length; ++role)
            {
                roleSet[role].Clear();
            }
            Get_Outfits();
            ResetDecider();
        }

        public static void ResetDecider()
        {
            Settings.Logger.LogDebug("ResetDecider()");

            foreach (var item in CharaEvent.ChaDefaults)
            {
                item.processed = false;
            }

            for (var role = 0; role < roleSet.Length; ++role)
            {
                roleSet[role].CoordSet.Coordinate();
            }
        }

        public static void Get_Outfits()
        {
            var f0 = DataStruct.FullStructures[Settings.CoordinatePath.Value];

            if (f0==null)
            {
                Settings.Logger.LogWarning($"{Settings.CoordinatePath.Value} not found");
                return;
            }

            // カード情報いちいちコーデチャネル別にスキャンし直さなくてもいいよね 
            // そんなわけでフィルタリングも後回し 
            for (var role = 0; role < roleSet.Length; ++role)
            {
                var f1 = f0[role];
                var rt = roleSet[role];
                rt.BaseDir = Settings.CoordinatePath.Value + Constants.CoordinateRoles[role];
                var plen = rt.BaseDir.Length;
                rt.BaseFolder = f1.SelectSubFolder(rt.BaseDir);
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

                Settings.Logger.LogDebug($"{cards.Count} cards found in {rt.BaseDir}");
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

#if KKS
            // 時間帯別設定 
            for (int period=0;period< filterByPeriods.Length; ++period)
            {
                var rt = roleSet[0];
                var ft = filterByPeriods[period];
                var folders = rt.FolderList;
                ft.Order = Settings.SpecificCategoriesByPeriod[period].Value;
                ft.Folder=(ft.Order=="")?null: rt.BaseFolder.SelectSubFolder(rt.BaseDir + sep + ft.Order);
            }
#endif
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
            var outfit = ThisOutfitData.Outfits[sets];

            if (ThisOutfitData.heroine == null)
            {
                // フリーH らしい 
                if (!Settings.EnableInFreeH.Value)
                {
                    outfit.Outer.Selected = null;
                    outfit.Inner.Selected = null;
                    return;
                }
            }

            var ft = filterBySets[sets];
            if (ft == null)
            {
                Settings.Logger.LogWarning($"Generalized_Assignment: FilterBySets[{sets}] is null");
                outfit.Outer.Selected = null;
                outfit.Inner.Selected = null;
                return;
            }
#if KKS
            if (sets==0 && SelectByPeriod >=0 && filterByPeriods[SelectByPeriod].Folder!=null)
            {
                // 時間帯別設定優先 
                ft = filterByPeriods[SelectByPeriod];
            }
#endif

            var status = ThisOutfitData.ChaControl.fileParam;
            var src0 = Settings.RandomizeOutfit.Value ? roleSet[0].CoordSet : null;
            if (src0 == null)
            {
                outfit.Outer.Selected = null;
                //Settings.Logger.LogWarning($"Generalized_Assignment: outfits CoordSet is null");
#if false
                return;
#endif
            }

            var src1 = Settings.RandomizeUnderwear.Value ? roleSet[1].CoordSet : null;
            // 下着除外 
            switch (sets)
            {
#if KK
                case 3: // 水着 
                    src1 = null;
                    break;
#elif KKS
                case 1: // 水着 
                case 3: // 風呂場 
                    src1 = null;
                    break;
#endif
            }
            if (src1 == null)
            {
                outfit.Inner.Selected = null;
                //Settings.Logger.LogWarning($"Generalized_Assignment: underwears CoordSet is null");
            }

            var filter = new SpecialCoordFilter();
            filter.SubDir = ft.Order;
            filter.Unexclude = (ft.Folder == null) ? 0 : ft.Folder.SpecialType.Excluded;
            filter.HeightGrade = ThisOutfitData.ChaControl.GetHeightCategory();
            filter.BustGrade = ThisOutfitData.ChaControl.GetBustCategory();
            if (ThisOutfitData.heroine != null)
            {
#if KK
                filter.Angry = ThisOutfitData.heroine.isAnger;
                filter.Teacher = ThisOutfitData.heroine.isTeacher;
#endif
                filter.Lewd = ThisOutfitData.heroine.HExperience == SaveData.Heroine.HExperienceKind.淫乱;
            }

            Settings.Logger.LogDebug($"Generalized_Assignment: sets:{sets} filter:{filter}");

            if (src0 != null)
            {
                outfit.Outer.Selected = src0.Random(filter);
                Settings.Logger.LogDebug($"Generalized_Assignment: outer={outfit.Outer.Selected?.GetFullPath()}");
            }
            if (src1 != null)
            {
                filter.SubDir = "";
                filter.Unexclude = 0;
                outfit.Inner.Selected = src1.Random(filter);
                Settings.Logger.LogDebug($"Generalized_Assignment: inner={outfit.Inner.Selected?.GetFullPath()}");
            }
        }
    }
}
