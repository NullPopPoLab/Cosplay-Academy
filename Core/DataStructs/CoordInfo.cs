using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CosplayParty
{
    //! アクセタイプ 
    public enum AccessoryType
    {
        //! 自動判別 
        /*! …というか装着箇所と設定で決める。 @n
        */
        Auto = 0,

        //! 標準アクセ 
        /*! 通常の扱い。 @n
            コーデを差し替えるとき外す。 @n
        */
        Standard,

        //! 体と一体化 
        /*! キャラカードに載っているものはコーデを差し替えても常に残す。 @n
            差し替え先のコーデについているものは Standard と同じ扱い。 @n
        */
        Bodyfit,

        //! 髪型 
        /*! コーデを差し替えても残す。 @n
            髪色反映対象にする。 @n
            差し替え先のコーデについているものは除去されるが、
            将来的に髪型として差し替え対象にするかもしれない。 @n
        */
        HairStyle,

        //! アホ毛 
        /*! HairStyle と同じ扱いだが、差し替え先のコーデに帽子があれば一時的に除去し、
            その後の差し替えで帽子がないとき戻す。 @n
        */
        Ahoge,

        //! 髪型連動アクセ 
        /*! 髪色反映しない以外は HairStyle と同じ扱い。 @n
        */
        Hairfit,

        //! 帽子 
        /*! 扱いは通常通りだが、差し替え先のコーデに含まれるときアホ毛は除去される。 @n
        */
        Hat,

        //! 眼鏡 
        /*! コーデを差し替えても残すが、差し替え先のコーデに眼鏡があれば一時的に付け替え、
            その後の差し替えで眼鏡がないとき戻す。 @n
        */
        Glasses,

        //! ピアス 
        /*! コーデを差し替えても残すが、差し替え先のコーデにピアスがあれば一時的に付け替え、
            その後の差し替えでピアスがないとき戻す。 @n
        */
        Pias,

        //! マスク 
        /*! コーデを差し替えても残すが、差し替え先のコーデにマスクがあれば一時的に付け替え、
            その後の差し替えでマスクがないとき戻す。 @n
        */
        Mask,

        //! 獣耳等 
        /*! コーデを差し替えても残すが、差し替え先のコーデに獣耳等があれば一時的に付け替え、
            その後の差し替えで獣耳等がないとき戻す。 @n
            また、差し替え先のコーデに含まれるときピアスは除去される。 @n
        */
        Ears,

        //! 尻尾 
        /*! コーデを差し替えても残すが、差し替え先のコーデに尻尾があれば一時的に付け替え、
            その後の差し替えで尻尾がないとき戻す。 @n
        */
        Tail,

        //! 翼 
        /*! コーデを差し替えても残すが、差し替え先のコーデに翼があれば一時的に付け替え、
            その後の差し替えで翼がないとき戻す。 @n
        */
        Wing,
    }

    //! 服情報 
    public class ClothInfo
    {
        public ChaFileClothes.PartsInfo Parts;

        public uint HiddenFlags {
            get
            {
                uint r = 0;
                uint f = 1;
                for (var i = 0; i < Parts.hideOpt.Length; ++i, f <<= 1)
                {
                    if (Parts.hideOpt[i]) r |= f;
                }
                return r;
            }
        }

        public ClothInfo(ChaFileClothes.PartsInfo val)
        {
            Parts = val;
        }

        public static List<ClothInfo> Build(ChaFileCoordinate coordinate)
        {
            var dst = new List<ClothInfo>();
            var src = coordinate.clothes.parts;
            for (var i = 0; i < src.Length; ++i)
            {
                var cinfo = new ClothInfo(src[i]);

                dst.Add(cinfo);
            }
            return dst;
        }
    }

    //! アクセ情報 
    public class AccessoryInfo
    {
        public ChaFileAccessory.PartsInfo Parts;
        public AccessoryType Type;
        public HairSupport.HairAccessoryInfo Hair;
        public MaterialEditorProperties Material;

        public bool IsEmpty { get { return Parts.type < 121; } }
        public string TypeName { get { return IsEmpty ? "Empty" : Type.ToString(); } }
        public string PartsID { get { return Parts.type + "-" + Parts.id; } }

        public AccessoryInfo(ChaFileAccessory.PartsInfo val)
        {
            Parts = val;
        }

        public static List<AccessoryInfo> Build(ChaFileCoordinate coordinate, Dictionary<int, HairSupport.HairAccessoryInfo> hair, ME_Coordinate mat)
        {
            var matprop = mat?.AccessoryProperties;
            //var ME_ACC_Storage = outfit.Original_Accessory_Data;

            var dst = new List<AccessoryInfo>();
            if (coordinate == null) return dst;

            //var src = new List<ChaFileAccessory.PartsInfo>();
            var src = coordinate.accessory.parts;
            for (var i = 0; i < src.Length; ++i)
            {
                var ainfo = new AccessoryInfo(src[i]);

                var ishair = (hair != null) && hair.TryGetValue(i, out ainfo.Hair);
#if false
                if (!ishair)
                {
                    ainfo.Hair = new HairSupport.HairAccessoryInfo
                    {
                        HairLength = -999
                    };
                }
#endif

                if (matprop == null || !matprop.TryGetValue(i, out ainfo.Material))
                {
                    ainfo.Material = new MaterialEditorProperties();
                }

                dst.Add(ainfo);

                /*! @todo コーデのアクセ毎に設定を追加
                    @todo AccState参照
                 */

                if (ainfo.Type == AccessoryType.Auto)
                {
                    ainfo.Type = AccessoryType.Standard;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationHeadAccs.Value && Constants.HeadAcceSet.Contains(src[i].parentKey)) ainfo.Type = ishair ? AccessoryType.HairStyle: AccessoryType.Hairfit;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationForeheadAccs.Value && Constants.ForeheadAcceSet.Contains(src[i].parentKey)) ainfo.Type = ishair ? AccessoryType.HairStyle : AccessoryType.Hairfit;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationHatAccs.Value && Constants.HatAcceSet.Contains(src[i].parentKey)) ainfo.Type = ishair ? AccessoryType.HairStyle : AccessoryType.Hairfit;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationEarAccs.Value && Constants.EarAcceSet.Contains(src[i].parentKey)) ainfo.Type = AccessoryType.Pias;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationEyeAccs.Value && Constants.EyeAcceSet.Contains(src[i].parentKey)) ainfo.Type = AccessoryType.Glasses;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationNoseAccs.Value && Constants.NoseAcceSet.Contains(src[i].parentKey)) ainfo.Type = AccessoryType.Mask;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationMouthAccs.Value && Constants.MouthAcceSet.Contains(src[i].parentKey)) ainfo.Type = AccessoryType.Mask;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationTailAccs.Value && Constants.TailAcceSet.Contains(src[i].parentKey)) ainfo.Type = AccessoryType.Tail;
                }
            }

            return dst;
        }
    }

    //! コーデ差し替えで受け継ぐもの
    /*! @note 初回ロードで構築し、ずっと残しておく必要がある。
    */
    public class CoordinateSuccession : IDisposable
    {
        //! 承継対象のアクセ 
        public List<AccessoryInfo> KeptAccessories = new List<AccessoryInfo>();

        public void Dispose()
        {
            Reset();
        }

        public void Reset()
        {
            KeptAccessories.Clear();
        }

        public void Keep(AccessoryInfo ainfo)
        {
            KeptAccessories.Add(ainfo);
        }
    }

    public class CoordInfo : IDisposable
    {
        public const bool Dump = false;

        public List<ClothInfo> ClothList = new List<ClothInfo>();
        public List<AccessoryInfo> AcceList = new List<AccessoryInfo>();

        public readonly CoordinateSuccession Succession = new CoordinateSuccession();

        public ChaFileCoordinate Coordinate;
        public Dictionary<int, HairSupport.HairAccessoryInfo> Hair;
        public ME_Coordinate Material;

        /*! @note シリアライズ向けにアクセ情報がまとめて保持される。
        */
        public Dictionary<int, HairSupport.HairAccessoryInfo> HairAccessories = new Dictionary<int, HairSupport.HairAccessoryInfo>();

        public void Dispose()
        {
            Succession.Dispose();
        }

        public void Reset()
        {
            // firstpass 時点で内容消去必要あるものを処理
            Succession.Reset();
            HairAccessories.Clear();
            AcceList.Clear();
        }

        public void Import(ChaFileCoordinate coordinate, Dictionary<int, HairSupport.HairAccessoryInfo> hair, ME_Coordinate mat)
        {
#if false // Additional_Card_Info 廃止予定 
                    var HairKeep = new List<int>();
                    var ACCKeep = new List<int>();
                    if (CoordinateInfo.ContainsKey(outfitnum))
                    {
                        HairKeep = CoordinateInfo[outfitnum].HairAcc;
                        ACCKeep = CoordinateInfo[outfitnum].AccKeep;
                    }
#endif

            Coordinate = coordinate;
            Hair = hair;
            Material = mat;

            ClothList = ClothInfo.Build(coordinate);
            AcceList = AccessoryInfo.Build(coordinate,hair,mat);

            for (var i = 0; i < ClothList.Count; ++i)
            {
                var cinfo = ClothList[i];

                if (Dump) Settings.Logger.LogDebug($"Import: Cloth {i} id={cinfo.Parts.id} hid={cinfo.HiddenFlags:X}");
            }

            // 強制的に保持するか 
            var xkeep = Settings.ExtremeAccKeeper.Value;

            for(var i = 0; i < AcceList.Count; ++i)
            {
                var ainfo = AcceList[i];

                // アクセを残すか 
                var keep = xkeep || ainfo.Type != AccessoryType.Standard;

                if (Dump) Settings.Logger.LogDebug($"Import: Acc {i+1} keep={keep} type={ainfo.TypeName} id={ainfo.PartsID}");

                //ExpandedOutfit.Logger.LogDebug($"ACC :{i}\tID: {data.nowAccessories[i].id}\tParent: {data.nowAccessories[i].parentKey}");
                if (keep)
                {
                    //Settings.Logger.LogDebug($"Keep from 1stpass: Acc {outfitnum}-{i}; {Intermediate[i]}");

                    Succession.Keep(ainfo);
                }
            }
        }
    }
}
