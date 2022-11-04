﻿using ExtensibleSaveFormat;
using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cosplay_Academy
{
    static class DirectoryFinder
    {
        private static readonly char sep = Path.DirectorySeparatorChar;

        public static void CheckMissingFiles()
        {
            // 規定のフォルダ準備 
            string[] names = { "inner", "outer" };
            for (var i = 0; i < names.Length; ++i)
            {
                var path = Settings.CoordinatePath.Value + sep + names[i];
                if (!Directory.Exists(path))
                {
                    Settings.Logger.LogWarning("Folder not found, creating directory at " + path);
                    Directory.CreateDirectory(path);
                }
            }
        }

        public static void Organize()
        {
            var coordinatepath = Settings.CoordinatePath.Value;
            var folders = Grab_All_Directories(coordinatepath + $"{sep}Unorganized");
            foreach (var item in folders)
            {
                var files = Get_Outfits_From_Path(item, false);

                foreach (var Coordinate in files)
                {
                    var Organizer = new ChaFileCoordinate();
                    Organizer.LoadFile(Coordinate);
#if false // Additional_Card_Info 廃止予定 
                    var ACI_Data = ExtendedSave.GetExtendedDataById(Organizer, "Additional_Card_Info");

                    if (ACI_Data == null)
                    {
                        continue;
                    }

                    Additional_Card_Info.CoordinateInfo coordiante;

                    if (ACI_Data.version == 1)
                    {
                        if (ACI_Data.data.TryGetValue("CoordinateInfo", out var ByteData) && ByteData != null)
                        {
                            coordiante = MessagePackSerializer.Deserialize<Additional_Card_Info.CoordinateInfo>((byte[])ByteData);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (ACI_Data.version < 1)
                    {
                        coordiante = Additional_Card_Info.Migrator.CoordinateMigrateV0(ACI_Data);
                    }
                    else
                    {
                        Settings.Logger.LogWarning("New version Detected please update Cosplay Party");
                        continue;
                    }
                    var restriction = coordiante.RestrictionInfo;
                    var CoordinateSubType = restriction.CoordinateSubType;

                    if (CoordinateSubType != 0 && CoordinateSubType != 10)
                    {
                        continue;
                    }

                    var CoordinateType = restriction.CoordinateType;
                    var HstateType_Restriction = restriction.HstateType_Restriction;
                    var SetNames = coordiante.SetNames;
                    var SubSetNames = coordiante.SubSetNames;
                    string Result;
                    var SubPath = "" + sep;
                    if (SetNames.Length > 0)
                    {
                        SubPath += SetNames;
                    }
                    if (SubSetNames.Length > 0)
                    {
                        if (!SubPath.EndsWith($"{sep}"))
                        {
                            SubPath += sep;
                        }
                        SubPath += SubSetNames;
                    }
                    var FileName = "" + sep + Coordinate.Split(sep).Last();
                    if (CoordinateType > 0)
                    {
                        CoordinateType++;
                    }
                    Result = coordinatepath + sep + SubPath;
                    if (!Directory.Exists(Result))
                        Directory.CreateDirectory(Result);
                    Result += FileName;
                    File.Copy(Coordinate, Result, true);
                    File.Delete(Coordinate);
#endif
                }
            }
        }

        public static List<string> Grab_All_Directories(string OriginalPath)
        {
            var FoldersPath = new List<string>();
            var originalpathexists = Directory.Exists(OriginalPath);
            if (originalpathexists)
            {
                FoldersPath.Add(OriginalPath);
                FoldersPath.AddRange(Directory.GetDirectories(OriginalPath, "*", SearchOption.AllDirectories)); //grab child folders
            }
            for (var i = 0; i < FoldersPath.Count; i++)
            {
                if (Directory.GetFiles(FoldersPath[i], "*.png").Length == 0)
                {
                    FoldersPath.RemoveAt(i--);
                }
            }
            return FoldersPath;
        }

        public static List<string> Grab_Folder_Directories(string OriginalPath, bool self)
        {
            var originalpathexists = Directory.Exists(OriginalPath);
            var list = new List<string>();
            if (originalpathexists)
            {
                if (self)
                    list.Add(OriginalPath);
                list.AddRange(Directory.GetDirectories(OriginalPath));//grab direct child folders
            }
            return list;
        }

        public static List<string> Get_Set_Paths(string Narrow)
        {
            var Choosen = new List<string>();
            var coordinatepath = Settings.CoordinatePath.Value;
            if (Directory.Exists(coordinatepath))
            {
                return Choosen;
            }
            var folders = Directory.GetDirectories(coordinatepath, "*", SearchOption.AllDirectories).ToList(); //grab child folders
            foreach (var folder in folders)
            {
                if (folder.Contains(Narrow))
                { Choosen.Add(folder); }
            }
            return Choosen;
        }

        public static List<string> Get_Outfits_From_Path(string OriginalPath, bool RemoveSets = true)
        {
            var Choosen = new List<string>();
            var Paths = new List<string>();
            if (Directory.Exists(OriginalPath))
            {
                Paths.Add(OriginalPath);
                Paths.AddRange(Directory.GetDirectories(OriginalPath, "*", SearchOption.AllDirectories)); //grab child folders
            }
            //step through each folder and grab files
            foreach (var path in Paths)
            {
                var files = Directory.GetFiles(path, "*.png");
                Choosen.AddRange(files);
            }
            var choosenempty = Choosen.Count == 0;
#if false // 廃止予定 
            if ((choosenempty || Settings.EnableDefaults.Value) && !OriginalPath.Contains($"{sep}Unorganized"))
            {
                Choosen.Add("Default");
                if (choosenempty)
                    Settings.Logger.LogWarning("No files found in :" + OriginalPath);
            }
#endif
            Settings.Logger.LogDebug($"Files found in : {OriginalPath} + {Choosen.Count}");
            return Choosen;
        }
    }
}


