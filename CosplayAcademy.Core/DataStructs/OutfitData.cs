using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cosplay_Academy
{
    public class OutfitData
    {
        const string defaultstring = "Default";
        private readonly static CardData Defaultcard = new CardData(defaultstring);

        private bool Part_of_Set = false;
        public List<CardData> Outfits_Per_State = new List<CardData>();
        private CardData Match_Outfit_Paths = null;
        public static bool Anger = false;

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

        public void Insert(List<CardData> Data, bool IsSet)//Insert data according to Outfits_Per_State[3] state and confirm if it is a setitem.
        {
            Data.Add(Defaultcard);
            Outfits_Per_State = Data;
            Part_of_Set = IsSet;
        }

        public CardData Random(bool Match, bool unrestricted, int personality = 0, ChaFileParameter.Attribute trait = null, int breast = 0, int height = 0)//get any random outfit according to experience
        {
            if (Match)
            {
                return Match_Outfit_Paths;
            }
            IEnumerable<CardData> applicable;
            if (!Anger)
            {
                var Tries = 0;
                var EXP = 0;
                CardData Result=null;
                do
                {
                    applicable = Outfits_Per_State.Where(x => Filter(x, unrestricted, personality, trait, breast, height));
                    var ac = applicable.Count();
                    if (ac < 1) break;
                    var rand = UnityEngine.Random.Range(0, ac);
                    Result = applicable.ElementAt(rand);
                    var isdefault = Result.Filepath == defaultstring;
                    if (Settings.EnableDefaults.Value && isdefault || !isdefault)
                    {
                        break;
                    }
                    if (Tries++ >= 10)
                    {
                        EXP--;
                        Tries = 0;
                        while (EXP > -1 && Outfits_Per_State.Count == 1)
                        {
                            EXP--;
                        }
                    }
                } while (EXP > -1);
                return Result;
            }
            else{
                applicable = Outfits_Per_State.Where(x => Filter(x, unrestricted, personality, trait, breast, height));
                var ac = applicable.Count();
                if (ac < 1) return null;
                return applicable.ElementAt(UnityEngine.Random.Range(0, ac));
            }
        }

        public CardData RandomSet(bool Match, bool unrestricted, int personality = 0, ChaFileParameter.Attribute trait = null, int breast = 0, int height = 0)//if set exists add its items to pool along with any coordinated outfit and other choices
        {
            IEnumerable<CardData> applicable;

                        var EXP = 0;
                        var Tries = 0;
                        var Result = Defaultcard;
                        do
                        {
                            if (Part_of_Set || !Match)
                            {
                                applicable = Outfits_Per_State.Where(x => Filter(x, unrestricted, personality, trait, breast, height));
                                var rand = UnityEngine.Random.Range(0, applicable.Count());
                                Result = applicable.ElementAt(rand);
                            }
                            else
                                Result = Match_Outfit_Paths;

                            var isdefault = Result.Filepath == defaultstring;
                            if (Settings.EnableDefaults.Value && isdefault || !isdefault)
                            {
                                break;
                            }
                            if ((Tries++ >= 3 || Match))
                            {
                                EXP--;
                                Tries = 0;
                                while (EXP > -1 && Outfits_Per_State.Count < 2)
                                {
                                    EXP--;
                                }
                            }
                        } while (EXP > -1);
                        return Result;

#if false // 再検討 
            var temp = new List<CardData>();

                if (Part_of_Set || !Match)
                    temp.AddRange(Outfits_Per_State.Where(x => Filter(x, unrestricted, personality, trait, breast, height)));
                else
                    temp.Add(Match_Outfit_Paths);

            CardData LastResult;
            var tries = 0;
            do
            {
                LastResult = temp[UnityEngine.Random.Range(0, temp.Count)];
            } while (++tries < 3 && LastResult == Defaultcard && !Settings.EnableDefaults.Value);
            return LastResult;
#endif
        }

        public List<CardData> Exportarray()
        {
            return Outfits_Per_State;
        }

        public void Coordinate()//set a random outfit to coordinate for non-set items when coordinated
        {
                Match_Outfit_Paths = Random(false, true);
        }

        public bool IsSet()
        {
            return Part_of_Set;
        }

        private bool Filter(CardData check, bool unrestricted, int personality, ChaFileParameter.Attribute trait, int breast, int height)
        {
            if (!check.DefinedData)
            {
                return true;
            }

            if (unrestricted)
            {
                return check.RestrictedPersonality.Count == 0 && check.Restricted.AllFalse() && check.Breastsize_Restriction.All(x => !x) && check.Height_Restriction.All(x => !x);
            }

            if (check.RestrictedPersonality.TryGetValue(personality, out var intresult) && intresult < 0)
            {
                return false;
            }

            if (check.Restricted.AnyOverlap(trait))
            {
                return false;
            }

            if (check.Breastsize_Restriction[breast])
            {
                return false;
            }

            if (check.Height_Restriction[height])
            {
                return false;
            }

            return true;
        }
    }
}
