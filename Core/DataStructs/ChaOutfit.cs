using System;

namespace CosplayParty
{
    public class CoordLoader
    {
        public string Path = "";
        public ChaFileCoordinate Coordinate;
        public bool IsSelected { get { return !String.IsNullOrEmpty(Path); } }
        public bool IsLoaded { get { return Coordinate != null; } }
        public bool IsReady { get { return IsLoaded || !IsSelected; } }

        public virtual void Dispose()
        {
            Unload();
        }

        public virtual void Unload()
        {
            if (!IsLoaded) return;
            Coordinate = null;
            Path = "";
        }

        public void Load()
        {
            Unload();

            if (String.IsNullOrEmpty(Path)) Coordinate = null;
            else if (!Path.EndsWith(".png")) Coordinate = null;
            else
            {
                Coordinate = new ChaFileCoordinate();
                if (!Coordinate.LoadFile(Path)) Coordinate = null;

                Settings.Logger.LogDebug($"CoordLoader {(IsLoaded ? "Ready" : "Failure")} for {Path}");
            }
        }

        public void Select(string path)
        {
            Unload();

            Path = path;
        }
    }

    public class ChaOutfit : IDisposable
    {
        public CharaInfo Info;
        public int Index { get; private set; }

        public readonly CoordLoader Outer = new CoordLoader();
        public readonly CoordLoader Inner = new CoordLoader();

        public bool MakeUpKeep = false;

        public ChaOutfit(CharaInfo info, int idx)
        {
            Info = info;
            Index = idx;
        }

        public void Dispose()
        {
            Reset();

            Outer.Dispose();
            Inner.Dispose();
        }

        public void Reset()
        {
        }

        public void Override4Coordinate(CoordInfo outer, CoordInfo inner)
        {
            var co = new CoordOverrider(Info, Index);
            co.Collaborate(Info.Coord[Index].Succession, outer, inner);
            co.Apply4Coordinate(outer);
        }

        public void Override4Generalize(CoordInfo outer, CoordInfo inner)
        {
            var co = new CoordOverrider(Info, Index);
            co.Collaborate(Info.Coord[Index].Succession, outer, inner);
            co.Apply4Generalize(outer);
        }
    }
}
