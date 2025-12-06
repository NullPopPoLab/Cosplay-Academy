using System;
using System.Collections.Generic;

namespace CosplayParty
{
    public class CharaInfo : IDisposable
    {
        public ChaControl Control;
        public ChaFileControl Source;
        public KCOX.CharaProps Overlay;
        public ME.CharaProps Material;
        public Hair.CharaProps Hair;

        public List<CoordInfo> Coord = new List<CoordInfo>();

        public CharaInfo() { }
        public CharaInfo(ChaFileControl src, ChaControl ctrl, string caption="") {

            Import(src,ctrl);
        }

        public void Dispose()
        {
            for (var i = 0; i < Coord.Count; ++i)
            {
                Coord[i].Dispose();
            }
        }

        public void Reset()
        {
            // firstpass 時点で内容消去必要あるものを処理
        }

        public void Import(ChaFileControl src, ChaControl ctrl)
        {
            Source = src;
            Control = ctrl;
            Overlay = new KCOX.CharaProps(src);
            Material = new ME.CharaProps(src);
            Hair = new Hair.CharaProps(src);

            for (var i = 0; i < src.coordinate.Length; ++i)
            {
                Coord.Add(new CoordInfo(this, i));
            }
        }
    }
}
