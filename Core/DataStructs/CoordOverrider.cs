using System;
using CosplayParty.Hair;
using CosplayParty.ME;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CosplayParty
{
    public class CoordOverrider : IDisposable
    {
        private ChaDefault ThisOutfitData;
        private int Index;

        public bool IsLoaded;
        public bool IsReady { get { return Selected == null || IsLoaded; } }

        public CardData Selected;

        public CoordOverrider(ChaDefault tod, int idx)
        {
            ThisOutfitData = tod;
            Index = idx;
        }

        public virtual void Dispose()
        {
            Unload();
            ThisOutfitData = null;
        }

        public virtual void Unload()
        {
            if (!IsLoaded) return;
            IsLoaded = false;
        }

        public void Load(string path)
        {
            if (String.IsNullOrEmpty(path)) Selected = null;
            else if (!path.EndsWith(".png")) Selected = null;
            else
            {
                var ThisCoordinate = ThisOutfitData.ChaControl.chaFile.coordinate[Index];
                IsLoaded = ThisCoordinate.LoadFile(path);//in case it fails
            }
        }
    }

    public class OverridingOuter : CoordOverrider
    {
        public OverridingOuter(ChaDefault tod, int idx) :
            base(tod, idx)
        {
        }

        public void Select()
        {
            Unload();

            if (Selected == null) return;
            Load(Selected.GetFullPath());
        }
    }

    public class OverridingInner : CoordOverrider
    {
        public OverridingInner(ChaDefault tod, int idx) :
            base(tod, idx)
        {
        }

        public void Select()
        {
            Unload();

        }
    }
}
