using BepInEx;
using BepInEx.Configuration;
using KKAPI.Studio;

namespace Cosplay_Academy
{
    [BepInProcess("KoikatsuSunshine")]
    public partial class Settings : BaseUnityPlugin
    {
        public static ConfigEntry<string>[] SpecificCategories = new ConfigEntry<string>[Constants.GameCoordinateSize];

        public void Awake()
        {
            if (StudioAPI.InsideStudio)
            {
                return;
            }

            StandardSettings();

            //Dresscode
            SpecificCategories[0] = Config.Bind("Dresscode", "Casual", "", "specified coordinate subfolder name or randomize");
            SpecificCategories[1] = Config.Bind("Dresscode", "Swimwear", "!swim", "specified coordinate subfolder name or randomize");
            SpecificCategories[2] = Config.Bind("Dresscode", "Night", "!nighty", "specified coordinate subfolder name or randomize");
            SpecificCategories[3] = Config.Bind("Dresscode", "Bathroom", "!bath", "specified coordinate subfolder name or randomize");
        }
    }
}