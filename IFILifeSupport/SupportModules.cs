using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;



namespace IFILifeSupport
{

    public class IFI_Resources : PartModule
    {
        //public const int BASE_SECS_PER_DAY = 21600;
        //public const int BASE_MULTIPLIER = 1; // 10


        internal string info = "";
        bool initted = false;
        void LateUpdate()
        {
            if (!initted)
                Start();
        }

        public void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            if (!HighLogic.LoadedSceneIsEditor && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
            {
                GameEvents.onPartResourceFlowModeChange.Add(onPartResourceFlowModeChange);
            }

            initted = true;
        }

        void OnDestroy()
        {
            if (Log != null)
                Log.Info("SupportModules.OnDestroy");
            if (initted && HighLogic.LoadedSceneIsEditor)
            {
                Log.Info("IFI_Resources.OnDestory");
                initted = false;
            }
            else
            {
                GameEvents.onPartResourceFlowModeChange.Remove(onPartResourceFlowModeChange);
            }
        }


        void onPartResourceFlowModeChange(GameEvents.HostedFromToAction<PartResource, PartResource.FlowMode> data)
        {
            Log.Info("onPartResourceFlowModeChange");
            PartResource host = data.host;
            Part part = host.part;

            Log.Info("host.part: " + part.partInfo.description);
            Log.Info("data.from.displayDescription: " + data.from.displayDescription());
            Log.Info("data.to.displayDescription." + data.to.displayDescription());

        }

        internal static string[] RESOURCES = new string[] { Constants.LIFESUPPORT, Constants.SLURRY, Constants.SLUDGE };

    }

    public class IFI_Basic : IFI_Resources
    {
        new void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            Log.Info("IFI_Basic");
            info = "IFI_Basic";
        }
    }
    public class IFI_Improved : IFI_Resources
    {
        new void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            Log.Info("IFI_Improved");
            info = "IFI_Improved";
        }
    }
    public class IFI_Advanced : IFI_Resources
    {
        new void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            info = "IFI_Advanced";
            Log.Info("IFI_Advanced");
        }
    }
    public class IFI_Extreme : IFI_Resources
    {
        new void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            info = "IFI_Extreme";
            Log.Info("IFI_Extreme");
        }
    }
}
