using BepInEx;
using BepInEx.Configuration;
using KKAPI.Studio;

namespace Cosplay_Academy
{
    [BepInProcess("KoikatsuSunshine")]
    public partial class Settings : BaseUnityPlugin
    {
        public void Awake()
        {
            if (StudioAPI.InsideStudio)
            {
                return;
            }

            StandardSettings();
        }
    }
}