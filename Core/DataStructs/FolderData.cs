using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CosplayParty
{
    [Serializable]
    [MessagePackObject]
    public class FolderData
    {
        [Key("_baseDir")]
        public string BaseDir { get; private set; }

        [Key("_subDir")]
        public string SubDir { get; private set; }

        [Key("_sub")]
        public List<FolderData> Subfolderdata { get; private set; }

        [Key("_cards")]
        public List<CardData> Cards { get; private set; }

        [Key("_specialType")]
        public SpecialCoordType SpecialType;

        [IgnoreMember]
        private string _foldername = "";

        [IgnoreMember]
        public string FullDir
        {
            get
            {
                if (SubDir == "") return BaseDir;
                return BaseDir + Path.DirectorySeparatorChar + SubDir;
            }
        }


        [SerializationConstructor]
        public FolderData(string _baseDir, string _subDir, List<CardData> _cards, List<FolderData> _sub, SpecialCoordType _specialType)
        {
            BaseDir = _baseDir;
            SubDir = _subDir;
            Subfolderdata = _sub;
            Cards = _cards;
            SpecialType = _specialType;
            _init();
            CleanUp();
            SetParent();
            //Settings.Logger.LogDebug($"FolderData: {_foldername} <= {BaseDir}/{SubDir}; {SpecialType}");
        }

        public FolderData(string path, FolderData parent = null)
        {
            Subfolderdata = new List<FolderData>();
            Cards = new List<CardData>();
            if (parent == null)
            {
                BaseDir = path;
                SubDir = "";
                SpecialType = SpecialCoordType.Create();
            }
            else
            {
                BaseDir = parent.BaseDir;
                SubDir = path;
                SpecialType = parent.SpecialType.Clone();
            }

            _init();
            SpecialType.Apply(_foldername);
            //Settings.Logger.LogDebug($"FolderData: {_foldername} <= {BaseDir}/{SubDir}; {SpecialType}");

            var sep = Path.DirectorySeparatorChar;
            if (Directory.Exists(FullDir))
            {
                FindCards();
                FindSubFolders();
            }
        }

        private void _init()
        {
            var di = new DirectoryInfo(FullDir);
            _foldername = (di == null) ? "" : di.Name;
        }

        public int GetCardCount()
        {
            var n = Cards.Count;
            for (var i = 0; i < Subfolderdata.Count; ++i)
            {
                n += Subfolderdata[i].GetCardCount();
            }
            return n;
        }

#if false
        private bool _isAvailableName(bool sub = false)
        {
            // 空の名前はサブフォルダ検索の機会を与える 
            if (_foldername.Length < 1) return true;

            // 明示的に設定された場合のみ先頭の _ を許可 
            if (sub && _foldername[0] == '_') return false;

            if (_foldername[0] == '!')
            {
                switch (_foldername)
                {
                    // KK/KKS専用フィルタ 
#if KK
                    case "!kk": return true;
#endif
#if KKS
                    case "!kks": return true;
#endif

                    // 状態別フィルタは OutfitDate.Filter() で適用 
                    case "!anger":
                    case "!lewd":
                    case "!teacher":
                    case "!short":
                    case "!not_short":
                    case "!tall":
                    case "!not_tall":
                    case "!flat":
                    case "!not_flat":
                    case "!busty":
                    case "!not_busty":
                        return true;

                    default: return false;
                }

            }

            return true;
        }

        public int GetAvailableCardCount(string attr,bool sub = false)
        {
            if (!_isAvailableName(sub)) return 0;

            var n = Cards.Count;
            for (var i = 0; i < Subfolderdata.Count; ++i)
            {
                n += Subfolderdata[i].GetAvailableCardCount(attr, true);
            }
            return n;
        }
#endif

        public void FindCards()
        {
            var files = Directory.GetFiles(FullDir, "*.png");
            //Settings.Logger.LogDebug($"FindCards: {files.Length} found in {FullDir}");
            var chafilecoordinate = new ChaFileCoordinate();
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (!Cards.Any(x => x.Filepath == name) && chafilecoordinate.LoadFile(file))
                {
#if false // Additional_Card_Info 廃止予定 
                    var ACI_Data = ExtendedSave.GetExtendedDataById(chafilecoordinate, "Additional_Card_Info");
                    if (ACI_Data == null)
                    {
                        Cards.Add(new CardData(name, this));
                        continue;
                    }

                    var data = new Additional_Card_Info.CoordinateInfo();

                    switch (ACI_Data.version)
                    {
                        case 0:
                            data = Additional_Card_Info.Migrator.CoordinateMigrateV0(ACI_Data);
                            break;
                        case 1:
                            if (ACI_Data.data.TryGetValue("CoordinateInfo", out var ByteData) && ByteData != null)
                            {
                                data = MessagePackSerializer.Deserialize<Additional_Card_Info.CoordinateInfo>((byte[])ByteData);
                            }
                            break;
                        default:
                            Settings.Logger.LogWarning("New version of Additional Card Info found, please update");
                            break;
                    }
                    Cards.Add(new CardData(name, this, data.RestrictionInfo));
#else
                    Cards.Add(new CardData(name, this));
#endif
                }
            }
            //            Settings.Logger.LogDebug($"{FolderPath} found {Cards.Count} cards");
        }

        public void FindSubFolders()
        {
            var sublist = DirectoryFinder.Grab_Folder_Directories(FullDir, false);
            foreach (var subfolder in sublist)
            {
                if (Subfolderdata.Any(X => X.FullDir == subfolder))
                {
                    continue;
                }
                Subfolderdata.Add(new FolderData(subfolder.Substring(BaseDir.Length + 1), this));
            }
        }

        public FolderData SelectSubFolder(string path)
        {
            var sep = Path.DirectorySeparatorChar;

            for (var i = 0; i < Subfolderdata.Count; ++i)
            {
                var f2 = Subfolderdata[i];
                var p2 = f2.FullDir;
                var l1 = path.Length;
                var l2 = p2.Length;
                //Settings.Logger.LogDebug($"FolderData.SelectSubFolder: {path} : {p2}");
                if (l1 < l2) continue;
                if (p2 == path) return f2;
                if (p2 + sep != path.Substring(0, l2) + sep) continue;
                return f2.SelectSubFolder(path);
            }

            return null;
        }

        public List<CardData> GetAllCards()
        {
            var list = new List<CardData>();

            list.AddRange(Cards);

            foreach (var item in Subfolderdata)
            {
                list.AddRange(item.GetAllCards());
            }

            return list;
        }

#if false
        public List<CardData> GetAvailableCards(string attr, bool sub = false)
        {
            var list = new List<CardData>();
            if (!_isAvailableName(sub)) return list;

            list.AddRange(Cards);

            foreach (var item in Subfolderdata)
            {
                list.AddRange(item.GetAvailableCards(attr, true));
            }

            return list;
        }
#endif

        public void Update()
        {
            Cards.Clear();
            FindCards();
            foreach (var item in Subfolderdata)
            {
                item.Update();
            }
            FindSubFolders();
        }

        public void CleanUp()
        {
            var foldercheck = Subfolderdata.Select(x => x.FullDir).ToArray();
            for (var i = foldercheck.Length - 1; i > -1; i--)
            {
                if (!Directory.Exists(foldercheck[i]))
                {
                    Subfolderdata.RemoveAt(i);
                }
            }
            var sep = Path.DirectorySeparatorChar;
            var cardscheck = Cards.Select(x => x.Filepath).ToArray();
            for (var i = cardscheck.Length - 1; i > -1; i--)
            {
                if (!File.Exists(FullDir + sep + cardscheck[i]))
                {
                    Cards.RemoveAt(i);
                }
            }
        }

        private void SetParent()
        {
            foreach (var item in Cards)
            {
                item.SetParent(this);
            }
        }

        public List<FolderData> GetAllFolders()
        {
            var result = new List<FolderData> { this };
            foreach (var item in Subfolderdata)
            {
                result.AddRange(item.GetAllFolders());
            }
            return result;
        }
        public List<FolderData> GetSubFolders()
        {
            var result = new List<FolderData> { this };
            foreach (var item in Subfolderdata)
            {
                result.Add(item);
            }
            return result;
        }
    }
}
