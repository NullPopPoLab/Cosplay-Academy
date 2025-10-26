using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;

namespace CosplayParty
{
    public struct SpecialCoordFilter
    {
        public bool Angry;
        public bool Lewd;
        public bool Teacher;
        public int HeightGrade;
        public int BustGrade;

        public override string ToString()
        {
            return $"angry={Angry} lewd={Lewd} teacher={Teacher} height={HeightGrade} bust={BustGrade}";
        }
    }

    [Serializable]
    [MessagePackObject]
    public struct SpecialCoordType
    {
        [Key("_angry")]
        public bool Angry;
        [Key("_lewd")]
        public bool Lewd;
        [Key("_teacher")]
        public bool Teacher;
        [Key("_denyByHeight")]
        public bool[] DenyByHeiget;
        [Key("_denyByBust")]
        public bool[] DenyByBust;

        public static SpecialCoordType Create()
        {
            var t = new SpecialCoordType();
            t.Angry = false;
            t.Lewd = false;
            t.Teacher = false;
            t.DenyByHeiget = new bool[3];
            t.DenyByBust = new bool[3];
            return t;
        }

        public SpecialCoordType Clone()
        {
            var t = new SpecialCoordType();
            t.Angry = Angry;
            t.Lewd = Lewd;
            t.Teacher = Teacher;
            t.DenyByHeiget = new bool[3] { DenyByHeiget[0], DenyByHeiget[1], DenyByHeiget[2] };
            t.DenyByBust = new bool[3] { DenyByBust[0], DenyByBust[1], DenyByBust[2] };
            return t;
        }

        public void Apply(string name)
        {
            switch (name)
            {
                case "!angry":
                    Angry = true;
                    break;

                case "!lewd":
                    Lewd = true;
                    break;

                case "!teacher":
                    Teacher = true;
                    break;

                case "!short":
                    DenyByHeiget[1] = true;
                    DenyByHeiget[2] = true;
                    break;

                case "!not_short":
                    DenyByHeiget[0] = true;
                    break;

                case "!tall":
                    DenyByHeiget[0] = true;
                    DenyByHeiget[1] = true;
                    break;

                case "!not_tall":
                    DenyByHeiget[2] = true;
                    break;

                case "!flat":
                    DenyByBust[1] = true;
                    DenyByBust[2] = true;
                    break;

                case "!not_flat":
                    DenyByBust[0] = true;
                    break;

                case "!busty":
                    DenyByBust[0] = true;
                    DenyByBust[1] = true;
                    break;

                case "!not_busty":
                    DenyByBust[2] = true;
                    break;
            }
        }

        public override string ToString()
        {
            return $"angry={Angry} lewd={Lewd} height=[{DenyByHeiget[0]},{DenyByHeiget[1]},{DenyByHeiget[2]}] bust=[{DenyByBust[0]},{DenyByBust[1]},{DenyByBust[2]}]";
        }
    }
}
