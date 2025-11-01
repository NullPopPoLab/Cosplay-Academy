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
        Auto=0,

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

        //! 下着連動アクセ 
        /*! 下着差し替え時に外す。 @n
        */
        InnerOrnament,
    }

    //! 承継対象のアクセ 
    public class SuccessingAccessory
    {
        public AccessoryType Type;
        public ChaFileAccessory.PartsInfo Part;
        public HairSupport.HairAccessoryInfo Hair;
        public MaterialEditorProperties Material;

        public SuccessingAccessory(AccessoryType type,ChaFileAccessory.PartsInfo part, HairSupport.HairAccessoryInfo hair, MaterialEditorProperties mat)
        {
            Type = type;
            Part = part;
            Hair = hair;
            Material = mat;
        }
    }

    //! コーデ差し替えで受け継ぐもの
    /*! @note 初回ロードで構築し、ずっと残しておく必要がある。
    */
    public class CoordinateSuccession : IDisposable
    {
        //! 承継対象のアクセ 
        public List<SuccessingAccessory> KeptAccessories = new List<SuccessingAccessory>();

        public void Dispose()
        {
            Reset();
        }

        public void Reset()
        {
            KeptAccessories.Clear();
        }

        public void Keep(AccessoryType type, ChaFileAccessory.PartsInfo part, HairSupport.HairAccessoryInfo hair, MaterialEditorProperties mat)
        {
            KeptAccessories.Add(new SuccessingAccessory(type,part,hair,mat));
        }
    }

    //! コーデ編集情報 
    /*! @note 編集前に Reset() を呼ぶ。
    */
    public class CoordinateProcessInfo : IDisposable
    {
        //! コーデ差し替え後に CoordinatePartsQueue から適用したアクセのインデクスが追記される 
        public readonly List<int> ACCKeepReturn = new List<int>();
        //! コーデ差し替え後に下着付属として追加したアクセのインデクスが追記される 
        public readonly List<int> UnderwearAccessoriesLocations = new List<int>();

        public bool[] UnderwearProcessed = new bool[9];
        public bool[] UnderClothingKeep = new bool[9];

        public void Dispose()
        {
            Reset();
        }

        public void Reset()
        {
            ACCKeepReturn.Clear();
            UnderwearAccessoriesLocations.Clear();
            UnderwearProcessed = new bool[9];
            UnderClothingKeep = new bool[9];
        }
    }

    public class ChaOutfit : IDisposable
    {
        private ChaDefault ThisOutfitData;
        private int Index;

        public readonly OverridingOuter Outer;
        public readonly OverridingInner Inner;

        // このあたりの構造、ロード前のコーデ適用なんかもあるのでロードと密連動させてはならない 
        // 用途に応じて適切なタイミングで扱う必要がある。 
        public readonly CoordinateSuccession Succession;
        public readonly CoordinateProcessInfo ProcInfo;

        /*! @note シリアライズ向けにアクセ情報がまとめて保持される。
        */
        public Dictionary<int, HairSupport.HairAccessoryInfo> HairAccessories = new Dictionary<int, HairSupport.HairAccessoryInfo>();


        public bool MakeUpKeep = false;

        public ChaOutfit(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;

            Outer = new OverridingOuter(tod, idx);
            Inner = new OverridingInner(tod, idx);
            Succession = new CoordinateSuccession();
            ProcInfo = new CoordinateProcessInfo();
        }

        public void Dispose()
        {
            Clear();
            Outer.Dispose();
            Inner.Dispose();
            Succession.Dispose();
            ProcInfo.Dispose();
            ThisOutfitData = null;
        }

        public void Clear()
        {
            //Outer.Unload();
            //Inner.Unload();
            Succession.Reset();
            ProcInfo.Reset();

            HairAccessories.Clear();
        }
    }
}
