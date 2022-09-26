using BepInEx;
using BepInEx.Configuration;
using KKAPI.MainGame;
using KKAPI.Studio;
namespace Cosplay_Academy
{
    [BepInProcess("Koikatu")]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("Koikatsu Party VR")]
    public partial class Settings : BaseUnityPlugin
    {
        public static ConfigEntry<bool> TeacherDress { get; private set; }

        public static ConfigEntry<bool> GrabUniform { get; private set; }

        public static ConfigEntry<int> AfterSchoolcasualchance { get; private set; }

        public static ConfigEntry<bool> AfterSchoolCasual { get; private set; }

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
            KeepOldBehavior = Config.Bind("Story Mode", "Koikatsu Probability behavior", true, "Old Behavior: Koikatsu Club Members have a chance (Probabilty slider) of spawning with a koikatsu outfit rather than reloading");
            TeacherDress = Config.Bind("Story Mode", "Teachers dress up", true, new ConfigDescription("Teachers probably would like to dress up if everyone does it.", null, AdvancedConfig));

            //Additional Outfit
            GrabUniform = Config.Bind("Additional Outfits", "Grab Normal uniforms for afterschool", true, new ConfigDescription("", null, AdvancedConfig));
            AfterSchoolCasual = Config.Bind("Additional Outfits", "After School Casual", true, new ConfigDescription("Everyone can be in casual wear after school", null));

            //Probability
            AfterSchoolcasualchance = Config.Bind("Probability", "Casual getup afterschool", 50, new ConfigDescription("Chance of wearing casual clothing after school", new AcceptableValueRange<int>(0, 100)));
        }
    }
}