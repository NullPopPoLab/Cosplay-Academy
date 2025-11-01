using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CosplayParty
{
    public class OutfitData
    {
        const string defaultstring = "Default";
        private readonly static CardData Defaultcard = new CardData(defaultstring);

        private bool Part_of_Set = false;
        public List<CardData> Outfits_Per_State = new List<CardData>();
        private CardData Match_Outfit_Paths = null;

        public OutfitData()
        {
            Match_Outfit_Paths = Defaultcard;
            Part_of_Set = false;
            Outfits_Per_State = new List<CardData>();
        }

        public void Clear()
        {
            Outfits_Per_State.Clear();
            Part_of_Set = false;
        }

        public List<CardData> Sum()//returns list that is the sum of all available lists.
        {
            var temp = new List<CardData>();
            temp.AddRange(Outfits_Per_State);
            return temp;
        }

        public void Replace(List<CardData> Data, bool IsSet)//Insert data according to Outfits_Per_State[3] state and confirm if it is a setitem.
        {
            // 廃止 
            //            Data.Add(Defaultcard);
            Outfits_Per_State = Data;
            Part_of_Set = IsSet;
        }

        public CardData Random(SpecialCoordFilter filter)
        {
            try
            {
                // Angry,Lewdランダム適用 
#if KK
            if (filter.Angry)
            {
                filter.Angry = UnityEngine.Random.Range(0, 101) <= Settings.SpecialOutfitRatio_Angry.Value;
                //Settings.Logger.LogDebug($"Random: for angry {filter.Angry} ({Settings.AngrySpecialOutfitRatio.Value}%)");
            }
            if (filter.Teacher)
            {
                filter.Teacher = UnityEngine.Random.Range(0, 101) <= Settings.SpecialOutfitRatio_Teacher.Value;
                //Settings.Logger.LogDebug($"Random: for teacher {filter.teacher} ({Settings.TeacherSpecialOutfitRatio.Value}%)");
            }
#endif
                if (filter.Lewd)
                {
                    filter.Lewd = UnityEngine.Random.Range(0, 101) <= Settings.SpecialOutfitRatio_Lewd.Value;
                    //Settings.Logger.LogDebug($"Random: for lewd {filter.Lewd} ({Settings.LewdSpecialOutfitRatio.Value}%)");
                }

                var applicable = Outfits_Per_State.Where(x => Filter(x, filter));
                if (filter.Angry && applicable.Count() < 1)
                {
                    //Settings.Logger.LogDebug("Angry coord not found; retry without it");
                    // 候補なければAngryを外して試す 
                    filter.Angry = false;
                    applicable = Outfits_Per_State.Where(x => Filter(x, filter));
                }
                if (filter.Lewd && applicable.Count() < 1)
                {
                    //Settings.Logger.LogDebug("Lewd coord not found; retry without it");
                    // 候補なければLewdを外して試す 
                    filter.Lewd = false;
                    applicable = Outfits_Per_State.Where(x => Filter(x, filter));
                }
                if (filter.Teacher && applicable.Count() < 1)
                {
                    //Settings.Logger.LogDebug("Teacher coord not found; retry without it");
                    // 候補なければTeacherを外して試す 
                    filter.Teacher = false;
                    applicable = Outfits_Per_State.Where(x => Filter(x, filter));
                }

                var ac = applicable.Count();
                if (ac < 1) return null;
                return applicable.ElementAt(UnityEngine.Random.Range(0, ac));
            }
            catch (Exception e)
            {
                Settings.Logger.LogDebug($"Random: " + e);
                return null;
            }
        }

        public List<CardData> Exportarray()
        {
            return Outfits_Per_State;
        }

        public void Coordinate()//set a random outfit to coordinate for non-set items when coordinated
        {
            Match_Outfit_Paths = Random(SpecialCoordFilter.Create());
        }

        public bool IsSet()
        {
            return Part_of_Set;
        }

        private bool Filter(CardData check, SpecialCoordFilter filter)
        {
            var dirname = (check.ParentFolder == null) ? "" : check.ParentFolder.SubDir;
            //Settings.Logger.LogDebug($"Filter: {dirname}/{check.Filepath}; {check.SpecialType}");

            // 常に不可 
            if (check.SpecialType.Never) return false;
            // 指定位置のサブフォルダ除外 
            if (check.SpecialType.Excluded > filter.Unexclude) return false;

            // 状態不一致不可 
            // (候補なければfalseに変更して再度試される) 
            if (filter.Angry != check.SpecialType.Angry) return false;
            if (filter.Lewd != check.SpecialType.Lewd) return false;
            if (filter.Teacher != check.SpecialType.Teacher) return false;

            // 身長制限 
            if (check.SpecialType.DenyByHeiget[filter.HeightGrade]) return false;
            // バスト制限 
            if (check.SpecialType.DenyByBust[filter.BustGrade]) return false;

            // 対象フォルダ外 
            if (filter.SubDir.Length > dirname.Length) return false;
            if (filter.SubDir != dirname.Substring(0, filter.SubDir.Length)) return false;

#if false
            if (unrestricted)
            {
                return check.RestrictedPersonality.Count == 0 && check.Restricted.AllFalse() && check.Breastsize_Restriction.All(x => !x) && check.Height_Restriction.All(x => !x);
            }

            if (check.RestrictedPersonality.TryGetValue(personality, out var intresult) && intresult < 0)
            {
                return false;
            }
#endif

            //Settings.Logger.LogDebug($"Filter: available {dirname}/{check.Filepath}");
            return true;
        }
    }
}
