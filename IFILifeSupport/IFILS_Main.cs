using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;
using static IFILifeSupport.RegisterToolbar;
#if false

namespace IFILifeSupport
{
    // IFILS_Main.lsTracking
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    internal class IFILS_Main : MonoBehaviour
    {
         //static LifeSupportUsageUpdate lsu = null;
         //static IFI_LIFESUPPORT_TRACKING lsTracking = null;
        public void Awake()
        {
            Log.Info("IFILS_Main.Awake");
            DontDestroyOnLoad(this);
            StartCoroutine(SlowUpdate());
        }
        IEnumerator SlowUpdate()
        {
            while (true)
            {
                if (!HighLogic.LoadedSceneIsGame)
                {
                    //if (lsu != null)
                    {
                        Log.Info("IFILS_Main, Destroying lsu");
                         //if (lsTracking != null) Destroy(lsTracking);
                       //if (lsu != null) Destroy(lsu);
                       // lsu = null;
                        //lsTracking = null;
                    }
                }
                else
                {
                    //if (lsu == null)
                    //    lsu = gameObject.AddComponent<LifeSupportUsageUpdate>();
                    //if (lsTracking == null)
                        //lsTracking = gameObject.AddComponent<IFI_LIFESUPPORT_TRACKING>();
                }
                yield return new WaitForSecondsLogged(1f);
            }
        }
    }

}
#endif