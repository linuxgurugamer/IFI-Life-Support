using UnityEngine;
using ToolbarControl_NS;
using KSP_Log;

namespace IFILifeSupport
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        internal static Log Log = null;
        static bool _gamePaused = false;
        static double  lastTime = 0;
        public static bool GamePaused
        { 
            get 
            { 
                if (_gamePaused)
                {
                    if (Planetarium.GetUniversalTime() != lastTime)
                        _gamePaused = false;
                }
                lastTime = Planetarium.GetUniversalTime();
                return _gamePaused; 
            } 
        }

        void Awake()
        {
            if (Log == null)
#if DEBUG
                Log = new Log("IFILS", Log.LEVEL.INFO);
#else
                Log = new Log("IFILS", Log.LEVEL.ERROR);
#endif
            GameEvents.onGamePause.Add(onGamePause);
            GameEvents.onGameUnpause.Add(onGameUnpause);
     
            DontDestroyOnLoad(this);
        }

        void onGamePause() { lastTime = Planetarium.GetUniversalTime(); _gamePaused = true; }
        void onGameUnpause() { lastTime = Planetarium.GetUniversalTime(); _gamePaused = false; }
        void Start()
        {
            ToolbarControl.RegisterMod(IFI_LifeSupportTrackingDisplay.MODID, IFI_LifeSupportTrackingDisplay.MODNAME);
        }
    }
}