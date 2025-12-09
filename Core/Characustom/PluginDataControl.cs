using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosplayParty.PluginControl
{
    public class PlugKeeper
    {
        public string PlugName { get; private set; }
        public PluginData RawData { get; protected set; }
        public bool IsLoaded { get { return RawData != null; } }
        public int Version { get { return IsLoaded ? RawData.version : -1; } }

        public PlugKeeper(string name)
        {
            PlugName = name;
        }

        public T Read<T>(string name)
        {
            if (!RawData.data.TryGetValue(name, out var img) || img == null)
            {
                return default(T);
            }

            var src = MessagePackSerializer.Deserialize<T>((byte[])img);
            if (src == null)
            {
                Settings.Logger.LogWarning($"invalid props; {PlugName}/{name}");
                return default(T);
            }

            return src;
        }

        public void Remove(string name)
        {
            if (RawData == null) return;
            if (RawData.data == null) return;
            if (RawData.data.ContainsKey(name))RawData.data.Remove(name);
        }

        public void Erase(string name)
        {
            if (RawData == null) RawData = new PluginData();
            RawData.data[name] = null;
        }

        public void Write<T>(string name, T img)
        {
            if (RawData == null) return;
            if (RawData.data == null) return;
            RawData.data[name]=MessagePackSerializer.Serialize(img);
        }

        public virtual void Save(){}
    }

    public class CharaPlugKeeper: PlugKeeper
    {
        public ChaFileControl Target;

        public CharaPlugKeeper(ChaFileControl target, string name):
            base(name)
        {
            Target = target;
            RawData = ExtendedSave.GetExtendedDataById(target, name);
        }

        public override void Save() {
            ExtendedSave.SetExtendedDataById(Target, PlugName, RawData);
        }
    }

    public class CoordPlugKeeper : PlugKeeper
    {
        public ChaFileCoordinate Target;

        public CoordPlugKeeper(ChaFileCoordinate target, string name) :
            base(name)
        {
            Target = target;
            RawData = ExtendedSave.GetExtendedDataById(target, name);
        }

        public override void Save()
        {
            ExtendedSave.SetExtendedDataById(Target, PlugName, RawData);
        }
    }
}
