using BepInEx;
using BepInEx.Configuration;
using KKAPI.Studio;

namespace CosplayParty
{
    [BepInProcess("KoikatsuSunshine")]
    public partial class Settings : BaseUnityPlugin
    {
        public static ConfigEntry<bool> RandomDresscode { get; private set; }

        public static ConfigEntry<string>[] SpecificCategories = new ConfigEntry<string>[Constants.GameCoordinateSize];
        public static ConfigEntry<string>[] SpecificCategoriesByPeriod = new ConfigEntry<string>[5];

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

            SpecificCategoriesByPeriod[0] = Config.Bind("DresscodeByPeriod", "Morning", "", "specified coordinate subfolder name or randomize");
            SpecificCategoriesByPeriod[1] = Config.Bind("DresscodeByPeriod", "Daytime", "", "specified coordinate subfolder name or randomize");
            SpecificCategoriesByPeriod[2] = Config.Bind("DresscodeByPeriod", "Eevening", "", "specified coordinate subfolder name or randomize");
            SpecificCategoriesByPeriod[3] = Config.Bind("DresscodeByPeriod", "Night", "", "specified coordinate subfolder name or randomize");
            SpecificCategoriesByPeriod[4] = Config.Bind("DresscodeByPeriod", "MyRoom", "", "specified coordinate subfolder name or randomize");
        }
    }
}