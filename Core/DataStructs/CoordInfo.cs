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
        /*! 猫耳とか尻尾とか。 @n
            キャラカードに載っているものはコーデを差し替えても常に残す。 @n
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

        //! 髪型連動アクセ 
        /*! 髪色反映しない以外は髪型とセットで扱う。 @n
        */
        HairOrnament,

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
    }

    //! 服情報 
    public class ClothInfo
    {
    }

    //! アクセ情報 
    public class AccessoryInfo
    {
        public ChaFileAccessory.PartsInfo Parts;
        public AccessoryType Type;
        public HairSupport.HairAccessoryInfo Hair;
        public ME.MaterialEditorProperties Material;

        public AccessoryInfo(ChaFileAccessory.PartsInfo val)
        {
            Parts = val;
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
        public List<AccessoryInfo> AcceList = new List<AccessoryInfo>();

        public readonly CoordinateSuccession Succession = new CoordinateSuccession();

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

            // 強制的に保持するか 
            var xkeep = Settings.ExtremeAccKeeper.Value;

            var matprop = mat.AccessoryProperties;
            //var ME_ACC_Storage = outfit.Original_Accessory_Data;

            //var acclist = new List<ChaFileAccessory.PartsInfo>();
            var acclist = coordinate.accessory.parts.ToList();
            for (var i = 0; i < acclist.Count; ++i)
            {
                var ainfo = new AccessoryInfo(acclist[i]);

                if (!hair.TryGetValue(i, out ainfo.Hair))
                {
                    ainfo.Hair = new HairSupport.HairAccessoryInfo
                    {
                        HairLength = -999
                    };
                }

                if (!matprop.TryGetValue(i, out ainfo.Material))
                {
                    ainfo.Material = new MaterialEditorProperties();
                }

                AcceList.Add(ainfo);

                /*! @todo コーデのアクセ毎に設定を追加
                 */

                if(ainfo.Type== AccessoryType.Auto)
                {
                    ainfo.Type = AccessoryType.Standard;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationHeadAccs.Value && Constants.HeadAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.HairOrnament;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationForeheadAccs.Value && Constants.ForeheadAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.HairOrnament;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationHatAccs.Value && Constants.HatAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.HairOrnament;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationEarAccs.Value && Constants.EarAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.Pias;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationEyeAccs.Value && Constants.EyeAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.Glasses;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationNoseAccs.Value && Constants.NoseAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.Mask;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationMouthAccs.Value && Constants.MouthAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.Mask;
                    if (ainfo.Type == AccessoryType.Standard && !Settings.DestinationTailAccs.Value && Constants.TailAcceSet.Contains(acclist[i].parentKey)) ainfo.Type = AccessoryType.Bodyfit;
                }

                // アクセを残すか 
                var keep = xkeep || ainfo.Type != AccessoryType.Standard;

                //Settings.Logger.LogDebug($"Process: Acc {outfitnum}-{i} XK={xkeep} GI={geneinc} HK={hkeep} AK={akeep}");

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
