using MessagePack;
using System;
using System.Collections.Generic;

namespace Cosplay_Academy
{
    [Serializable]
    [MessagePackObject]
    public class FolderStruct
    {
        [Key("_hstruct")]
        public HFolderStruct FolderData { get; private set; }

        public FolderStruct()
        {
            FolderData = new HFolderStruct();
        }

        [SerializationConstructor]
        public FolderStruct(HFolderStruct _hstruct)
        {
            FolderData = _hstruct;
        }

        public List<CardData> GetAllCards()
        {
            var list = new List<CardData>();
            list.AddRange(FolderData.GetAllCards());
            return list;
        }

        public List<FolderData> GetAllFolders()
        {
            var list = new List<FolderData>();
            list.AddRange(FolderData.GetAllFolders());
            return list;
        }

        public void Populate(string folderpath)
        {
            FolderData.Populate(folderpath);
        }

        public void Update()
        {
            FolderData.Update();
        }

        public void CleanUp()
        {
            FolderData.CleanUp();
        }
    }
}
