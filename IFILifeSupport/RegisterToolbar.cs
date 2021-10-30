using UnityEngine;
using ToolbarControl_NS;
using KSP_Log;

namespace IFILifeSupport
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        internal static Log Log = null;
        void Awake()
        {
            if (Log == null)
#if DEBUG
                Log = new Log("IFILS", Log.LEVEL.INFO);
#else
                Log = new Log("IFILS", Log.LEVEL.ERROR);
#endif

        }

        void Start()
        {
            ToolbarControl.RegisterMod(IFI_LIFESUPPORT_TRACKING.MODID, IFI_LIFESUPPORT_TRACKING.MODNAME);
        }
    }
}