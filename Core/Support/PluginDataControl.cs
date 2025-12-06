using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosplayParty.PluginControl
{
    public class Keeper
    {
        public string DataName { get; private set; }
        public PluginData Data { get; protected set; }
        public bool IsLoaded { get { return Data != null; } }
        public int Version { get { return IsLoaded ? Data.version : -1; } }

        public Keeper(string name)
        {
            DataName = name;
        }

        public T Read<T>(string name)
        {
            if (!Data.data.TryGetValue(name, out var img) || img == null)
            {
                return default(T);
            }

            var src = MessagePackSerializer.Deserialize<T>((byte[])img);
            if (src == null)
            {
                Settings.Logger.LogWarning($"invalid props; {DataName}/{name}");
                return default(T);
            }

            return src;
        }

        public void Remove(string name)
        {
            if (Data == null) return;
            if (Data.data == null) return;
            if (Data.data.ContainsKey(name))Data.data.Remove(name);
        }

        public void Erase(string name)
        {
            if (Data == null) Data = new PluginData();
            Data.data[name] = null;
        }

        public void Write<T>(string name, T img)
        {
            if (Data == null) return;
            if (Data.data == null) return;
            Data.data[name]=MessagePackSerializer.Serialize(img);
        }

        public virtual void Save(){}
    }

    public class CharaData: Keeper
    {
        public ChaFileControl Target;

        public CharaData(ChaFileControl target, string name):
            base(name)
        {
            Target = target;
            Data = ExtendedSave.GetExtendedDataById(target, name);
        }

        public override void Save() {
            ExtendedSave.SetExtendedDataById(Target, DataName, Data);
        }
    }

    public class CoordData : Keeper
    {
        public ChaFileCoordinate Target;

        public CoordData(ChaFileCoordinate target, string name) :
            base(name)
        {
            Target = target;
            Data = ExtendedSave.GetExtendedDataById(target, name);
        }

        public override void Save()
        {
            ExtendedSave.SetExtendedDataById(Target, DataName, Data);
        }
    }
}
