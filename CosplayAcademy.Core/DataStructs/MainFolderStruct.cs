using MessagePack;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Cosplay_Academy
{
    [Serializable]
    [MessagePackObject]
    public class FolderStruct
    {
        [Key("_hfol")]
        public List<FolderData> FolderData { get; private set; }

        public FolderStruct()
        {
            FolderData = new List<FolderData>();
        }

        [SerializationConstructor]
        public FolderStruct(List<FolderData> _hfol)
        {
            FolderData = _hfol;
        }

        public List<CardData> GetAllCards()
        {
            var result = new List<CardData>();
            for (var i = 0; i < FolderData.Count; ++i)
            {
                result.AddRange(FolderData[i].GetAllCards());
            }
            return result;
        }

        public List<FolderData> GetAllFolders()
        {
            var result = new List<FolderData>();
            for (var i = 0; i < FolderData.Count; ++i)
            {
                result.AddRange(FolderData[i].GetAllFolders());
            }
            return result;
        }

        public List<FolderData> GetSubFolders()
        {
            var result = new List<FolderData>();
            for (var i = 0; i < FolderData.Count; ++i)
            {
                result.Add(FolderData[i]);
            }
            return result;
        }

        public FolderData SelectSubFolder(string path)
        {
            Settings.Logger.LogDebug($"SelectSubFolder: {path}");

            var sep = Path.DirectorySeparatorChar;

            for (var i = 0; i < FolderData.Count; ++i)
            {
                var f2 = FolderData[i];
                var p2 = f2.FolderPath;
                Settings.Logger.LogDebug($"SelectSubFolder to: {p2}");
                var l1 = path.Length;
                var l2 = p2.Length;
                if (l1 < l2) continue;
                if (p2 == path) return f2;
                if (p2 + sep != path.Substring(0, l2) + sep) continue;
                return f2.SelectSubFolder(path);
            }

            return null;
        }

        public void Populate(string path)
        {
            var sep = Path.DirectorySeparatorChar;

            var subdirectories = DirectoryFinder.Grab_Folder_Directories(path, true);
            foreach (var directory in subdirectories)
            {
                if (FolderData.Any(x => x.FolderPath == directory))
                    continue;

                FolderData.Add(new FolderData(directory));
            }
        }

        public void Update()
        {
            foreach (var folder in FolderData)
            {
                folder.Update();
            }
        }

        public void CleanUp()
        {
            for (var j = FolderData.Count - 1; j > -1; j--)
            {
                var folder = FolderData[j];
                if (!Directory.Exists(folder.FolderPath))
                {
                    FolderData.RemoveAt(j);
                    continue;
                }
                folder.CleanUp();
                if (folder.Cards.Count == 0)
                {
                    FolderData.RemoveAt(j);
                }
            }
        }
    }
}
