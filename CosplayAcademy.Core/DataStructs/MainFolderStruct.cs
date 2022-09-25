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
        public HFolderStruct[] FolderData { get; private set; }

        public FolderStruct()
        {
            FolderData = new HFolderStruct[1];
            FolderData[0] = new HFolderStruct();
        }

        [SerializationConstructor]
        public FolderStruct(HFolderStruct[] _hstruct)
        {
            FolderData = _hstruct;
        }

        public List<CardData> GetAllCards()
        {
            var list = new List<CardData>();
            foreach (var hstates in FolderData)
            {
                list.AddRange(hstates.GetAllCards());
            }
            return list;
        }

        public List<FolderData> GetAllFolders()
        {
            var list = new List<FolderData>();
            foreach (var hstates in FolderData)
            {
                list.AddRange(hstates.GetAllFolders());
            }
            return list;
        }

        public void Populate(string folderpath)
        {
            FolderData[0].Populate(folderpath);
        }

        public void Update()
        {
            foreach (var item in FolderData)
            {
                item.Update();
            }
        }

        public void CleanUp()
        {
            foreach (var item in FolderData)
            {
                item.CleanUp();
            }
        }
    }
}
