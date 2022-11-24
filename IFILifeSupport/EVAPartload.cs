using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using static IFILifeSupport.RegisterToolbar;



namespace IFILifeSupport
{
    public static class IFIDebug
    {
        public static bool IsON = false;
#if false
        public static void IFIMess(string IFIMessage)
        {
#if !DEBUG
            if (IsON)
#endif
            {
                Log.Warning("*IFI DEBUG--" + IFIMessage);
            }
        }
#endif
#if !DEBUG
        public static void Toggle()
        {
            if (IsON) { IsON = false; } else { IsON = true; }
        }
#endif
    }

    // [KSPAddon(KSPAddon.Startup.MainMenu, true)]

    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class ADDEVAS : MonoBehaviour
    {
        public void Awake()
        {
            LoadingScreen screen = FindObjectOfType<LoadingScreen>();
            if (screen == null)
            {
                Log.Warning("IFI Can't find LoadingScreen type. Aborting execution");
                return;
            }
            List<LoadingSystem> list = LoadingScreen.Instance.loaders;

            if (list != null)
            {
                //GameObject IFIObject = new GameObject("PartUpdate");
                //PartUpdate loader = IFIObject.AddComponent<PartUpdate>();

                //Log.Warning(string.Format("Adding PartUpdate to the loading screen {0}", list.Count));
                //list.Insert(1, loader);

                Debug.Log(" IFI Preload LS EVA Actions Set ++++ ");
                GameEvents.onCrewOnEva.Add(OnCrewOnEva11);
                GameEvents.onCrewBoardVessel.Add(OnCrewBoardVessel);
            }

        }
        void OnDestroy()
        {
            GameEvents.onCrewOnEva.Remove(OnCrewOnEva11);
            GameEvents.onCrewBoardVessel.Remove(OnCrewBoardVessel);
        }


        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> action)
        {
            Log.Warning(" IFI DEBUG -- OnCrewBoardVessel fired ----");
            double IFIResourceAmt = 0.0;
            double IFIResElectric = 0.0;
            for (int i = 0; i < action.from.Resources.Count; i++)
            //foreach (PartResource pr in action.from.Resources)
            {
                PartResource pr = action.from.Resources[i];
                string IIResource = pr.resourceName;
                Log.Warning(" Resource Name " + IIResource);
                if (IIResource == Constants.LIFESUPPORT)
                {
                    IFIResourceAmt += pr.amount;
                }
                else if (IIResource == Constants.ELECTRIC_CHARGE)
                {
                    // IFIResElectric += pr.amount;
                }
            }
            Log.Warning(" Electric Found " + Convert.ToString(IFIResElectric));
            IFIResourceAmt = action.from.RequestResource(Constants.LIFESUPPORT, IFIResourceAmt);
            IFIResourceAmt = action.to.RequestResource(Constants.LIFESUPPORT, 0.0 - IFIResourceAmt);
            //IFIResElectric = (action.from.RequestResource(Constants.ELECTRIC_CHARGE, IFIResElectric)) - 0.001;
            //IFIResElectric = action.to.RequestResource(Constants.ELECTRIC_CHARGE, 0.0 - IFIResElectric);
            Log.Warning("IFI Life Support Message: EVA - Ended - " + action.from.name + " Boarded Vessel - LS Return = " + Convert.ToString(IFIResourceAmt) + " and  Electric" + Convert.ToString(IFIResElectric));
        }

        private void OnCrewOnEva11(GameEvents.FromToAction<Part, Part> action) //Kerbal goes on EVA takes LS With them
        {

            Log.Warning("IFI DEBUG -- OnCrewOnEva fired ----");
            double resourceRequest = 0.0;//* Take 4 hours of LS on each eva.
            double IFIResElectric = resourceRequest * 1.5;
            double IFIResReturn = 0.0;
            try
            {
                for (int i = 0; i < action.to.Resources.Count; i++)
                //foreach (PartResource pr in action.to.Resources)
                {
                    PartResource pr = action.to.Resources[i];
                    if (pr.resourceName.Equals(Constants.LIFESUPPORT))
                    {
                        pr.amount = pr.maxAmount;
                        resourceRequest += pr.maxAmount;
                    }


                }
            }
            catch (Exception ex) { Log.Warning(" IFI Exception +ON EVA RESOURCE TRANSFER+ " + ex.Message); }
            //IFIResReturn = action.from.RequestResource(Constants.ELECTRIC_CHARGE, resourceRequest * 1.5);
            //IFIResElectric -= IFIResReturn;
            //IFIResReturn = action.to.RequestResource(Constants.ELECTRIC_CHARGE, IFIResElectric);
            //IFIResElectric = resourceRequest * 1.5;
            //IFIResElectric -= IFIResReturn;
            IFIResReturn = 0.0;
            IFIResReturn = action.from.RequestResource(Constants.LIFESUPPORT, resourceRequest);
            resourceRequest -= IFIResReturn;
            resourceRequest = action.to.RequestResource(Constants.LIFESUPPORT, resourceRequest);
            Log.Warning("IFI Life Support Message: EVA - Started - " + action.to.name + " Exited Vessel - Took " + Convert.ToString(IFIResReturn) + " Life Support  and " + Convert.ToString(IFIResElectric) + " Electric Charge ");
        }
    }

}
