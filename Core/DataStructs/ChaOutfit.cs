using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System;
using System.Collections.Generic;

namespace CosplayParty
{
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
        public class ImportSources
        {
            public ChaFile Chafile;
            public ChaFileCoordinate[] Original_Coordinates;
            public Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>> CharaHair;
            public PluginData HairExtendedData;

            public ME_CharaProps ME;
        }

        private ChaDefault ThisOutfitData;
        public int Index { get; private set; }

        public readonly CoordInfo Current = new CoordInfo();
        public readonly CoordLoader Outer = new CoordLoader();
        public readonly CoordLoader Inner = new CoordLoader();

        // このあたりの構造、ロード前のコーデ適用なんかもあるのでロードと密連動させてはならない 
        // 用途に応じて適切なタイミングで扱う必要がある。 
        public readonly CoordinateProcessInfo ProcInfo;

        // FirstPass 処理で構築 
        // Clear() でも残す 
        public ChaFileCoordinate Original_Coordinate;
        public Dictionary<int, HairSupport.HairAccessoryInfo> HairInfo;

        public bool MakeUpKeep = false;

        public ChaOutfit(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;

            ProcInfo = new CoordinateProcessInfo();
            HairInfo = new Dictionary<int, HairSupport.HairAccessoryInfo>();
        }

        public void Dispose()
        {
            Reset();
            Outer.Dispose();
            Inner.Dispose();
            Current.Dispose();
            ProcInfo.Dispose();
            ThisOutfitData = null;
            HairInfo = null;
        }

        public void Reset()
        {
            // firstpass 時点で内容消去必要あるものを処理
            Current.Reset();
            ProcInfo.Reset();
        }

        private ChaFileCoordinate _cloneCoordinate(ChaFileCoordinate OriginalCoordinate)
        {
            return new ChaFileCoordinate
            {
                clothes = OriginalCoordinate.clothes,
                makeup = OriginalCoordinate.makeup,
                enableMakeup = OriginalCoordinate.enableMakeup,
            }; ;
        }

        public void Import(ImportSources src)
        {
            Original_Coordinate = _cloneCoordinate(src.Original_Coordinates[Index]);
            if (src.CharaHair == null)
            {
                src.CharaHair = new Dictionary<int, Dictionary<int, HairSupport.HairAccessoryInfo>>();
            }
            else if (src.CharaHair.TryGetValue(Index, out HairInfo) == false)
            {
                HairInfo = new Dictionary<int, HairSupport.HairAccessoryInfo>();
            }
            var mat = src.ME.Coord[Index];
            Current.Import(src.Chafile.coordinate[Index], HairInfo, mat);
        }

        public void Override4Coordinate(ChaFileCoordinate outer, ChaFileCoordinate inner)
        {
            var co = new CoordOverrider(ThisOutfitData, Index);
            co.Collaborate(Current.Succession, outer, inner);
            co.Apply4Coordinate(outer);
        }

        public void Override4Generalize(ChaFileCoordinate outer, ChaFileCoordinate inner)
        {
            var co = new CoordOverrider(ThisOutfitData, Index);
            co.Collaborate(Current.Succession, outer, inner);
            co.Apply4Generalize(outer);
        }
    }
}
