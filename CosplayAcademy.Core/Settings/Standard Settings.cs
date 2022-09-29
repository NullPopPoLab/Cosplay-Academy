﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
#if TRACE
using System.Diagnostics;
#endif

namespace Cosplay_Academy
{
    [BepInPlugin(GUID, "Cosplay Party", Version)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
    [BepInDependency(MoreAccessoriesKOI.MoreAccessories.GUID, MoreAccessoriesKOI.MoreAccessories.versionNum)]
    [BepInDependency(Sideloader.Sideloader.GUID)]
    public partial class Settings : BaseUnityPlugin
    {
        public const string GUID = "CosplayParty";
        public const string Version = "0.1";
        public static Settings Instance;
        internal static new ManualLogSource Logger { get; private set; }

        public static ConfigEntry<bool> UpdateCache { get; private set; }
        public static ConfigEntry<bool> UpdateFolders { get; private set; }

        public static ConfigEntry<bool> UseAlternativePath { get; private set; }
        public static ConfigEntry<string> CoordinatePath { get; private set; }
        public static ConfigEntry<bool> EnableSetting { get; private set; }
        public static ConfigEntry<bool> EnableSets { get; private set; }
        public static ConfigEntry<bool> IndividualSets { get; private set; }
        public static ConfigEntry<bool> EnableDefaults { get; private set; }
        public static ConfigEntry<bool> StoryModeChange { get; private set; }
        public static ConfigEntry<bool> KeepOldBehavior { get; private set; }

        public static ConfigEntry<bool> HairMatch { get; private set; }
        public static ConfigEntry<bool> DestinationHairstyle { get; private set; }

        public static ConfigEntry<bool> Makerview { get; private set; }
        public static ConfigEntry<bool> FullSet { get; private set; }
        public static ConfigEntry<bool> ResetMaker { get; set; }

        public static ConfigEntry<bool> ChangeOutfit { get; set; }

        public static ConfigEntry<Hexp> H_EXP_Choice { get; private set; }

        public static ConfigEntry<OutfitUpdate> UpdateFrequency { get; private set; }
        public static ConfigEntry<bool> SundayDate { get; private set; }

        public static ConfigEntry<bool> AccKeeper { get; private set; }
        public static ConfigEntry<bool> RandomizeUnderwear { get; private set; }
        public static ConfigEntry<bool> RandomizeUnderwearOnly { get; private set; }
        public static ConfigEntry<bool> ForceRandomUnderwear { get; private set; }
        public static ConfigEntry<bool> UnderwearStates { get; private set; }
        public static ConfigEntry<bool> ExtremeAccKeeper { get; private set; }

        private static ConfigEntry<string> Lastversion { get; set; }


#if TRACE
        private static Stopwatch Stopwatch = new Stopwatch();
#endif
        internal void StandardSettings()
        {
            Instance = this;
            Logger = base.Logger;

            KKAPI.MainGame.GameAPI.RegisterExtraBehaviour<GameEvent>(GUID);

            var sep = Path.DirectorySeparatorChar;

            CharacterApi.RegisterExtraBehaviour<CharaEvent>(GUID, 900);
            var AdvancedConfig = new ConfigurationManagerAttributes { IsAdvanced = true };

            UpdateFrequency = Config.Bind("Story Mode", "Update Frequency", OutfitUpdate.Daily);
            SundayDate = Config.Bind("Story Mode", "Sunday Date Special", true, "Date will wear something different on Sunday, not really useful unless Update Frequency is not daily/period ");

            //Cache
            UpdateCache = Config.Bind("Cache", "Update Cache Buttons", false, new ConfigDescription("", null, new ConfigurationManagerAttributes() { HideSettingName = true, HideDefaultButton = true, CustomDrawer = new Action<ConfigEntryBase>(UpdateCacheData) }));
            Lastversion = Config.Bind("Cache", "Check if version changed, clear cache", "0", new ConfigDescription("", null, new ConfigurationManagerAttributes() { Browsable = false }));

            //Accessories
            ExtremeAccKeeper = Config.Bind("Accessories", "KEEP ALL ACCESSORIES", false, new ConfigDescription("Keep all accessories a character starts with\nUsed for Characters whos bodies require accessories such as amputee types\nNot Recommended for use with characters wth unnecessary accessories", null, AdvancedConfig));
            HairMatch = Config.Bind("Accessories", "Force Hair Color on accessories", false, "Match items with Custom Hair Component to Character's Hair Color.");
            DestinationHairstyle = Config.Bind("Accessories", "Destination Hairstyle", false, "Remove source hair or coordinating hair.");

            //Main Game
            AccKeeper = Config.Bind("Main Game", "On Coordinate Load Support", true, new ConfigDescription("Keep head and tail accessories\nUsed for characters who have accessory based hair and avoid them going bald\nWorks best with a Cosplay Party Ready character marked by Additional Card Info", null, new ConfigurationManagerAttributes() { IsAdvanced = true, Order = 4 }));
            RandomizeUnderwear = Config.Bind("Main Game", "Randomize Underwear", false, "Loads underwear from Underwear folder (Does not apply to Gym/Swim outfits)\nWill probably break some outfits that depends on underwear outside of Gym/Swim if not set up with Additional Card Info plugin");
            RandomizeUnderwearOnly = Config.Bind("Main Game", "Randomize Underwear Only", false, "Its an option");
            ForceRandomUnderwear = Config.Bind("Main Game", "Force underwear parts", false, "Doesn't force Top or Bottom");
            EnableSetting = Config.Bind("Main Game", "Enable Cosplay Party", true, new ConfigDescription("Doesn't require Restart\nDoesn't Disable On Coordinate Load Support or Force Hair Color", null, new ConfigurationManagerAttributes() { Order = 5 }));

            //Sets
            EnableSets = Config.Bind("Outfit Sets", "Enable Outfit Sets", true, new ConfigDescription("Outfits in set folders can be pulled from a group for themed sets", null, new ConfigurationManagerAttributes() { Order = 3 }));
            IndividualSets = Config.Bind("Outfit Sets", "Do not Find Matching Sets", false, new ConfigDescription("Don't look for other sets that are shared per coordinate type", null, AdvancedConfig));
            FullSet = Config.Bind("Outfit Sets", "Assign available sets only", false, new ConfigDescription("Prioritize sets in order: Uniform > Gym > Swim > Club > Casual > Nightwear\nDisabled priority reversed: example Nightwear set will overwrite all clothes if same folder is found", null, AdvancedConfig));

            //Additional Outfit
            EnableDefaults = Config.Bind("Additional Outfits", "Enable Default in rolls", false, new ConfigDescription("Adds default outfit to roll tables", null, AdvancedConfig));

            //prob
            H_EXP_Choice = Config.Bind("Probability", "Outfit Picker logic", Hexp.RandConstant, new ConfigDescription("Randomize: Each outfit can be from different H States\nRandConstant: Randomizes H State, but will choose the same level if outfit is found (will get next highest if Enable Default is not enabled)\nMaximize: Do I really gotta say?", null, AdvancedConfig));

            //Maker
            Makerview = Config.Bind("Maker", "Enable Maker Mode", false, new ConfigDescription("", null, AdvancedConfig));
            ResetMaker = Config.Bind("Maker", "Reset Sets", false, new ConfigDescription("Will overwrite current day outfit in storymode if you wanted to view that version.", null, AdvancedConfig));
            ChangeOutfit = Config.Bind("Maker", "Change generated outfit", false, new ConfigDescription("Pick new coordinates in maker", null, AdvancedConfig));

            //Other Mods
            UnderwearStates = Config.Bind("Other Mods", "Randomize Underwear: ACC_States", true, "Attempts to write logic for AccStateSync and Accessory states to use.");

            CoordinatePath = Config.Bind("Coordinate Location", "Path to coordinate folder", new DirectoryInfo(UserData.Path).FullName + "Coordinate" + sep + "CosplayParty", "Coordinate Path");
            UpdateFolders = Config.Bind("Coordinate Location", "Folder Options", false, new ConfigDescription("", null, new ConfigurationManagerAttributes() { HideSettingName = true, HideDefaultButton = true, CustomDrawer = new Action<ConfigEntryBase>(FolderUpdateGUI) }));

            DirectoryFinder.CheckMissingFiles();
            StartCoroutine(Wait());
            IEnumerator Wait()
            {
                yield return null;

                var startingindex = Manager.Scene.ActiveScene.buildIndex;
                do
                {
                    yield return null;

                } while (startingindex == Manager.Scene.ActiveScene.buildIndex);
#if TRACE
                Stopwatch = Stopwatch.StartNew();
#endif

                Constants.PluginCheck();
#if TRACE
                Stopwatch.Stop();
                Settings.Logger.LogWarning($"Took {Stopwatch.ElapsedMilliseconds} ms for  Constants.PluginCheck();");
#endif

                if (!Constants.PluginResults["Additional_Card_Info"] && !CharacterApi.RegisteredHandlers.Any(x => x.ExtendedDataId == "Additional_Card_Info")) //provide access to info even if plugin-doesn't exist
                {
                    CharacterApi.RegisterExtraBehaviour<Dummy>("Additional_Card_Info");
                }

                yield return null;

                Logger.LogMessage($"Following warnings/errors are related to coordinates Cosplay Party has cached");

                DirectoryFinder.Organize();

                DataStruct.SetPath(Paths.CachePath + sep + GUID + ".data");

                if (Version != Lastversion.Value)
                {
                    DataStruct.Reset();
                    Lastversion.Value = Version;
                }
                else
                {
                    DataStruct.StartUpLoad();
                }
                yield return null;
                Logger.LogMessage($"End of Cache process");
            }
#if TRACE
            Stopwatch = Stopwatch.StartNew();
#endif

            Constants.ExpandedOutfit();
#if TRACE
            Stopwatch.Stop();
            Logger.LogWarning($"Took {Stopwatch.ElapsedMilliseconds} ms for Constants.ExpandedOutfit()");
#endif

            MakerAPI.RegisterCustomSubCategories += CharaEvent.RegisterCustomSubCategories;
            MakerAPI.MakerExiting += (s, e) => CharaEvent.MakerAPI_MakerExiting();
            MakerAPI.MakerStartedLoading += (s, e) => CharaEvent.Firstpass = 0;
        }

        private readonly static GUIContent FindNewCards = new GUIContent("Find New Cards", "Only Find New cards");
        private readonly static GUIContent UpdateAllCards = new GUIContent("Update Cache", "Force Update All Cards");
        private readonly static GUIContent ResetCards = new GUIContent("Reset Cache", "Reset Cache and start with Coordinate Location Path");
        private readonly static GUIContent RerollSets = new GUIContent("Reroll Choosen Sets", "Reset character outfits.");

        private void UpdateCacheData(ConfigEntryBase configEntry)
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button(FindNewCards))
            {
                DataStruct.FindNewCards();
            }
            if (GUILayout.Button(UpdateAllCards))
            {
                DataStruct.Update();
            }
            if (GUILayout.Button(ResetCards))
            {
                DataStruct.Reset();
            }
            if (GUILayout.Button(RerollSets))
                OutfitDecider.ResetDecider();
            GUILayout.EndVertical();
        }

        private readonly static GUIContent Organize = new GUIContent("Organize Folder Work", $"Arrange cards in organize folder to the correct folder if possible");

        private void FolderUpdateGUI(ConfigEntryBase configEntryBase)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button(Organize))
                DirectoryFinder.Organize();

            if (GUILayout.Button(new GUIContent("Make Folder Structure in coordinate folder", $"folder: {CoordinatePath.Value}")))
                DirectoryFinder.CheckMissingFiles();

            GUILayout.EndVertical();
        }
    }
}