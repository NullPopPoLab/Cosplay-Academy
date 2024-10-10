using BepInEx;
using BepInEx.Configuration;
using KKAPI.MainGame;
using KKAPI.Studio;
using System.Collections.Generic;

namespace CosplayParty
{
    [BepInProcess("Koikatu")]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("Koikatsu Party VR")]
    public partial class Settings : BaseUnityPlugin
    {
        public static ConfigEntry<bool> TeacherDress { get; private set; }

//        public static ConfigEntry<bool> GrabUniform { get; private set; }

//        public static ConfigEntry<bool> AfterSchoolCasual { get; private set; }

        public static ConfigEntry<string>[] SpecificCategories = new ConfigEntry<string>[Constants.GameCoordinateSize];

        public void Awake()
        {
            if (StudioAPI.InsideStudio)
            {
                return;
            }
            Hooks.Init();

            StandardSettings();

            var AdvancedConfig = new ConfigurationManagerAttributes { IsAdvanced = true };

            //StoryMode
            StoryModeChange = Config.Bind("Story Mode", "Koikatsu Outfit Change", false, "Experimental: probably has a performance impact when reloading the character when they enter/leave the club\nKoikatsu Club Members will change when entering the club room and have a chance of not changing depending on experience and lewdness");
//            KeepOldBehavior = Config.Bind("Story Mode", "Koikatsu Probability behavior", true, "Old Behavior: Koikatsu Club Members have a chance (Probabilty slider) of spawning with a koikatsu outfit rather than reloading");
            TeacherDress = Config.Bind("Story Mode", "Teachers dress up", true, new ConfigDescription("Teachers probably would like to dress up if everyone does it.", null, AdvancedConfig));

            //Additional Outfit
//            GrabUniform = Config.Bind("Additional Outfits", "Grab Normal uniforms for afterschool", true, new ConfigDescription("", null, AdvancedConfig));
//            AfterSchoolCasual = Config.Bind("Additional Outfits", "After School Casual", true, new ConfigDescription("Everyone can be in casual wear after school", null));

            //Dresscode
            SpecificCategories[0] = Config.Bind("Dresscode", "In school", "", "specified coordinate subfolder name or randomize");
            SpecificCategories[1] = Config.Bind("Dresscode", "After school", "", "specified coordinate subfolder name or randomize");
            SpecificCategories[2] = Config.Bind("Dresscode", "Gym", "!gym", "specified coordinate subfolder name or randomize");
            SpecificCategories[3] = Config.Bind("Dresscode", "Swimwear", "!swim", "specified coordinate subfolder name or randomize");
            SpecificCategories[4] = Config.Bind("Dresscode", "Club", "", "specified coordinate subfolder name or randomize");
            SpecificCategories[5] = Config.Bind("Dresscode", "Casual", "", "specified coordinate subfolder name or randomize");
            SpecificCategories[6] = Config.Bind("Dresscode", "Night", "!nighty", "specified coordinate subfolder name or randomize");
        }
    }
}