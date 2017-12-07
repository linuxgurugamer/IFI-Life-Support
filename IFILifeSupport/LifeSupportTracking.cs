using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP;
using KSP.UI.Screens;

namespace IFILifeSupport
{

    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class IFI_LIFESUPPORT_TRACKING : UnityEngine.MonoBehaviour
    {
        public static IFI_LIFESUPPORT_TRACKING Instance;
        internal static int LS_ID;

        private int IFITIM = 0;
        // Stock APP Toolbar - Stavell
        private ApplicationLauncherButton IFI_Button = null;
        private Texture2D IFI_button_grn = null;
        private Texture2D IFI_button_cau = null;
        private Texture2D IFI_button_danger = null;
        private bool IFI_Texture_Load = false;

        private bool KerbalEVARescueDetect = false;
        private double IFITimer;
        private int IFICWLS = 25;
        private string[,] LS_Status_Hold;

        class LS_Status_Row
        {
            public string[] data = new string[MAX_STATUSES];
        }
        private List<LS_Status_Row> LS_Status_Cache;
        private int[] LS_Status_Width;
        private int[] LS_Status_Spacing;
        private int LS_Status_Hold_Count;
        // Make sure LS remaining Display conforms to Kerbin time setting.
        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }
        private bool Went_to_Main = false;

        GUIStyle vSmallScrollBar;
        GUIStyle hSmallScrollBar;

        const int VESSEL = 0;
        const int LOCATION = 1;
        const int CREW = 2;
        const int LSAVAIL = 3;
        const int DAYS_REM = 4;
        const int SLURRYAVAIL = 5;
        const int SLURRY_DAYS_TO_PROCESS = 6;
        //const int SLURRYRATE = ???;
        const int SLUDGEAVAIL = 7;
        const int SLUDGE_DAYS_TO_PROCESS = 8;
        const int SLUDGE_PROCESS_RATE = 8;
        const int SLURRYCONVRATE = 9;
        const int SLUDGECONVRATE = 10;
        const int SLUDGE_OUTPUT_RATE = 11;
        const int LIFESUPPORT_OUTPUT_RATE = 12;

        const int MAX_STATUSES = 13;

        string lblGreenColor = "00ff00";
        string lblDrkGreenColor = "ff9d00";
        string lblBlueColor = "3DB1FF";
        string lblYellowColor = "FFD966";
        string lblRedColor = "f90000";

        private void OnGUIApplicationLauncherReady()
        {
            // Create the button in the KSP AppLauncher

            if (IFI_Button == null)
            {
                IFI_Button = ApplicationLauncher.Instance.AddModApplication(GUIToggle, GUIToggle,
                                null, null,
                                null, null,
                                ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.MAPVIEW,
                                IFI_button_grn);
            }

            if (!HighLogic.LoadedSceneIsEditor)
            {
                Life_Support_Update();
            }
        }

        private void GUIToggle()
        {
            Life_Support_Update();
            LifeSupportDisplay.LSDisplayActive = !LifeSupportDisplay.LSDisplayActive;
            if (HighLogic.LoadedSceneIsEditor)
            {
                InitStatusCache();
                ClearStageSummaryList();
                GetStageSummary();
            }
        }

        private void ResetButton()
        {
            IFI_Button.SetTexture(IFI_button_grn);
        }

        public void Life_Support_Update()
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedSceneIsEditor || !HighLogic.LoadedSceneIsGame)
                return; //Don't do anything while the game is loading or in editor
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                Went_to_Main = true;
                return; // Don't Run at main menu and reset IFITimer
            }
            if (Went_to_Main)
            {
                IFITimer = Convert.ToInt32(Planetarium.GetUniversalTime());
                Went_to_Main = false;
            }

            double Elapsed_Time = IFI_Get_Elasped_Time();
            IFI_Button.SetTexture(IFI_button_grn);

            int LS_ALERT_LEVEL = 1;
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                LS_Status_Hold = new string[FlightGlobals.Vessels.Count(), MAX_STATUSES];
                LS_Status_Width = new int[MAX_STATUSES];
                LS_Status_Spacing = new int[MAX_STATUSES];

                LS_Status_Hold_Count = 0;
                Debug.Log("######## Looking for Ships ######");
                for (int idx = 0; idx < FlightGlobals.Vessels.Count; idx++)
                //foreach (Vessel vessel in FlightGlobals.Vessels)
                {
                    Vessel vessel = FlightGlobals.Vessels[idx];
                    if (vessel && (
                        vessel.vesselType == VesselType.Ship || vessel.vesselType == VesselType.Lander ||
                        vessel.vesselType == VesselType.Station || vessel.vesselType == VesselType.Rover ||
                        vessel.vesselType == VesselType.Base || vessel.vesselType == VesselType.Probe) ||
                        vessel.vesselType == VesselType.EVA)
                    {
                        Debug.Log(" Found Vessel");//float distance = (float)Vector3d.Distance(vessel.GetWorldPos3D(), FlightGlobals.ActiveVessel.GetWorldPos3D());
                        string TVname;
                        int IFI_Crew = 0;
                        double LSAval;
                        double SlurryAvail;
                        double SludgeAvail;
                        string IFI_Location = "";
                        double IFI_ALT = 0.0;
                        TVname = vessel.vesselName; // vessel.name;
                        if (!vessel.loaded)
                        {
                            IFI_Crew = vessel.protoVessel.GetVesselCrew().Count;
                            IFI_ALT = vessel.protoVessel.altitude;
                            IFI_Location = vessel.mainBody.name;
                        }
                        else
                        {
                            IFI_Crew = vessel.GetCrewCount();
                            IFI_Location = vessel.mainBody.name;
                            IFI_ALT = vessel.altitude;
                        }
                        if (IFI_Crew > 0.0)
                        {

                            IFIDebug.IFIMess("IFI_LIFESUPPORT_TRACKING.Life_Support_Update, IFI_Location: " + IFI_Location +
                                ",  FlightGlobals.GetHomeBodyName(): " + FlightGlobals.GetHomeBodyName() + ",   IFI_ALT: " + IFI_ALT);

                            double LS_Use = LifeSupportRate.GetRate();

                            LSAval = IFIGetAllResources(Constants.LIFESUPPORT, vessel, vessel.loaded);
                            SlurryAvail = IFIGetAllResources(Constants.SLURRY, vessel, vessel.loaded);
                            SludgeAvail = IFIGetAllResources(Constants.SLUDGE, vessel, vessel.loaded);
                            Log.Info("Initial LS_Use: " + LS_Use + ",  LSAval: " + LSAval);

                            if (LifeSupportRate.IntakeAirAvailable(vessel))
                            {
                                if (vessel.mainBody.name == FlightGlobals.GetHomeBodyName())
                                    LS_Use *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment; //  0.20;
                                else
                                    LS_Use *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment; // 0.60;
                            }
                            Log.Info("After Atmo adjust, LS_Use: " + LS_Use + ",   IFI_Crew: " + IFI_Crew + ",   Elapsed_Time: " + Elapsed_Time);

                            //{
                            //    if (IFI_Location == "Laythe" && IFI_ALT <= 6123) { LS_Use *= 0.60; }
                            //}

                            double days_rem = LSAval / IFI_Crew / LS_Use / 3600 / HoursPerDay;
                            Log.Info("LSAval: " + LSAval + ", IFI_Crew: " + IFI_Crew + ", LS_Use: " + LS_Use + ", HoursPerDay: " + HoursPerDay);
                            LS_Use *= IFI_Crew;
                            LS_Use *= Elapsed_Time;
                            // IF No EC use more LS resources
                            if (IFIGetAllResources("ElectricCharge", vessel, vessel.loaded) < 0.1)
                            {
                                LS_Use *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment; // 1.2;
                            }

                            if (LS_Use > 0.0)
                            {

                                double rtest = IFIUSEResources(Constants.LIFESUPPORT, vessel, vessel.loaded, LS_Use, IFI_Crew);
                                double stest = IFIUSEResources(Constants.SLURRY, vessel, vessel.loaded, -rtest, IFI_Crew);
                                Log.Info("Elapsed_Time: " + Elapsed_Time);
                                Log.Info("LifeSupport rtest: " + rtest);
                                Log.Info("Organicslurry stest: " + stest);
                            }

#if false

// duplicated code, no need to do double duty
                            //Debug.Log("Vessel with crew onboard Found: " + TVname + "   Crew=" + IFI_Crew +"    LifeSupport = "+ LSAval +"  Body:"+IFI_Location+"   ALT:"+IFI_ALT);
                            double LS_RR = LifeSupportRate.GetRate();
                            Log.Info("LS_RR 1: " + LS_RR);
                            if (LifeSupportRate.IntakeAirAvailable(vessel))
                            {
                                if (IFI_Location == FlightGlobals.GetHomeBodyName())
                                    LS_RR *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment; //  0.20; 
                                else
                                    LS_RR *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment; // 0.60; 
                            }
                            Log.Info("LS_RR 2: " + LS_RR);
                            if (IFIGetAllResources("ElectricCharge", vessel, vessel.loaded) < 0.1)
                            {
                                LS_RR *=  HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment; // 1.2;
                            }

                            double days_rem = LSAval / IFI_Crew / LS_RR / 3600 / HoursPerDay;
                            Log.Info("LS_RR 3: " + LS_RR);
                            Log.Info("LSAval: " + LSAval + ", IFI_Crew: " + IFI_Crew + ", LS_RR: " + LS_RR + ", HoursPerDay: " + HoursPerDay);
#endif


                            //LS_Status_Hold[LS_Status_Hold_Count, VESSEL] = String.Format("<color=#{0}>{1}</color>", lblGreenColor, TVname);  //TVname;
                            LS_Status_Hold[LS_Status_Hold_Count, VESSEL] = Colorized(lblGreenColor, TVname);  //TVname;

                            //LS_Status_Hold[LS_Status_Hold_Count, LOCATION] = String.Format("<color=#{0}>{1}</color>", lblBlueColor, IFI_Location); // IFI_Location;
                            LS_Status_Hold[LS_Status_Hold_Count, LOCATION] = Colorized(lblBlueColor, IFI_Location); // IFI_Location;
                            string H_Crew = Convert.ToString(IFI_Crew);
                            if (vessel.vesselType == VesselType.EVA)
                            {
                                H_Crew = "EVA";
                            }
                            LS_Status_Hold[LS_Status_Hold_Count, CREW] = H_Crew;
                            if (vessel.vesselType == VesselType.EVA && KerbalEVARescueDetect)
                            {
                                LS_Status_Hold[LS_Status_Hold_Count, LSAVAIL] = "RESCUE";
                                LS_Status_Hold[LS_Status_Hold_Count, DAYS_REM] = "RESCUE";
                                LSAval = 200.0;
                                days_rem = 10.0;
                            }
                            else
                            {
                                string color = lblGreenColor;
                                if (days_rem < 3)
                                    color = lblYellowColor;
                                if (days_rem < 1)
                                    color = lblRedColor;
                                //LS_Status_Hold[LS_Status_Hold_Count, LSAVAIL] = String.Format("<color=#{0}>{1}</color>", color, Convert.ToString(Math.Round(LSAval, 2))); // Convert.ToString(Math.Round(LSAval, 4));
                                //LS_Status_Hold[LS_Status_Hold_Count, DAYS_REM] = String.Format("<color=#{0}>{1}</color>", color, Convert.ToString(Math.Round(days_rem, 2))); // Convert.ToString(Math.Round(days_rem, 2));
                                LS_Status_Hold[LS_Status_Hold_Count, LSAVAIL] = Colorized(color, Convert.ToString(Math.Round(LSAval, 2))); // Convert.ToString(Math.Round(LSAval, 4));
                                LS_Status_Hold[LS_Status_Hold_Count, DAYS_REM] = Colorized(color, Convert.ToString(Math.Round(days_rem, 2))); // Convert.ToString(Math.Round(days_rem, 2));
                            }

                            double slurryRate = 0;
                            double sludgeRate = 0;
                            Log.Info("vessel: " + vessel.name);
                            if (vessel.loaded)
                            {
                                for (int idx2 = 0; idx2 < vessel.parts.Count; idx2++)
                                {
                                    Log.Info("part: " + vessel.parts[idx2].partInfo.title);
                                    for (int m = 0; m < vessel.parts[idx2].Modules.Count; m++)
                                    {
                                        PartModule tmpPM = vessel.parts[idx2].Modules[m];

                                        if (vessel.parts[idx2].Modules[m].moduleName == "ModuleResourceConverter")
                                        {
                                            ModuleResourceConverter m1 = (ModuleResourceConverter)tmpPM;
                                            for (int i = 0; i < m1.inputList.Count; i++)
                                            //foreach (ResourceRatio inp in m1.inputList)
                                            {
                                                ResourceRatio inp = m1.inputList[i];
                                                if (inp.ResourceName == Constants.SLURRY)
                                                    slurryRate += inp.Ratio;
                                                if (inp.ResourceName == Constants.SLUDGE)
                                                    sludgeRate += inp.Ratio;

                                            }
                                        }
                                        if (vessel.parts[idx2].Modules[m].moduleName == "AnimatedGenerator")
                                        {
                                            AnimatedGenerator m1 = (AnimatedGenerator)tmpPM;

                                            for (int i = 0; i < m1.inputList.Count; i++)
                                            //foreach (ResourceRatio inp in m1.inputList)
                                            {
                                                ResourceRatio inp = m1.inputList[i];

                                                if (inp.ResourceName == Constants.SLURRY)
                                                    slurryRate += inp.Ratio;
                                                if (inp.ResourceName == Constants.SLUDGE)
                                                    sludgeRate += inp.Ratio;

                                            }
                                        }

                                    }
                                }
                            }
                            else
                            {
                                Log.Info("Processing unloaded vessel");

                                for (int idx3 = 0; idx3 < vessel.protoVessel.protoPartSnapshots.Count; idx3++)
                                //foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                                {
                                    ProtoPartSnapshot p = vessel.protoVessel.protoPartSnapshots[idx3];
                                    Part part = p.partPrefab;

                                    Log.Info("part: " + part.partInfo.title);
                                    for (int m = 0; m < part.Modules.Count; m++)
                                    {
                                        PartModule tmpPM = part.Modules[m];

                                        if (part.Modules[m].moduleName == "ModuleResourceConverter")
                                        {
                                            ModuleResourceConverter m1 = (ModuleResourceConverter)tmpPM;
                                            for (int i = 0; i < m1.inputList.Count; i++)
                                            //foreach (ResourceRatio inp in m1.inputList)
                                            {
                                                ResourceRatio inp = m1.inputList[i]; if (inp.ResourceName == Constants.SLURRY)
                                                    slurryRate += inp.Ratio;
                                                if (inp.ResourceName == Constants.SLUDGE)
                                                    sludgeRate += inp.Ratio;

                                            }
                                        }
                                        if (part.Modules[m].moduleName == "AnimatedGenerator")
                                        {
                                            AnimatedGenerator m1 = (AnimatedGenerator)tmpPM;

                                            for (int i = 0; i < m1.inputList.Count; i++)
                                            //foreach (ResourceRatio inp in m1.inputList)
                                            {
                                                ResourceRatio inp = m1.inputList[i];
                                                if (inp.ResourceName == Constants.SLURRY)
                                                    slurryRate += inp.Ratio;
                                                if (inp.ResourceName == Constants.SLUDGE)
                                                    sludgeRate += inp.Ratio;

                                            }
                                        }

                                    }
                                }
                            }

                            if (vessel.vesselType != VesselType.EVA)
                            {
                                // doesn't hurt to do the secsPerDay calc every time, since this only runs once every 3 seconds
                                double secsPerDay = 3600 * (GameSettings.KERBIN_TIME ? 6 : 24);

                                // Need to scan for all greenhouses here
                                //LS_Status_Hold[LS_Status_Hold_Count, 5] = Convert.ToString(Math.Round(SlurryAvail, 5));
                                LS_Status_Hold[LS_Status_Hold_Count, SLURRYAVAIL] = SlurryAvail.ToString("N2");

                                // Need to scan for all sludge convertors here
                                //LS_Status_Hold[LS_Status_Hold_Count, 7] = Convert.ToString(Math.Round(SludgeAvail, 5));
                                LS_Status_Hold[LS_Status_Hold_Count, SLUDGEAVAIL] = SludgeAvail.ToString("N2");


                                Log.Info("Slurry: " + SlurryAvail + ",    rate: " + (secsPerDay * slurryRate));
                                if (slurryRate > 0)
                                    LS_Status_Hold[LS_Status_Hold_Count, SLURRY_DAYS_TO_PROCESS] = (SlurryAvail / (secsPerDay * slurryRate)).ToString("N1");
                                else
                                    LS_Status_Hold[LS_Status_Hold_Count, SLURRY_DAYS_TO_PROCESS] = "n/a";
                                if (sludgeRate > 0)
                                    LS_Status_Hold[LS_Status_Hold_Count, SLUDGE_DAYS_TO_PROCESS] = (SludgeAvail / (secsPerDay * sludgeRate)).ToString("N1");
                                else
                                    LS_Status_Hold[LS_Status_Hold_Count, SLUDGE_DAYS_TO_PROCESS] = "n/a";

                                LS_Status_Hold[LS_Status_Hold_Count, SLURRYCONVRATE] = (secsPerDay * slurryRate).ToString("N1");
                                LS_Status_Hold[LS_Status_Hold_Count, SLUDGECONVRATE] = (secsPerDay * sludgeRate).ToString("N1");
                            }

                            LS_Status_Hold_Count += 1;

                            if (LS_ALERT_LEVEL < 2 && days_rem < 3)
                            {
                                IFI_Button.SetTexture(IFI_button_cau);
                                LS_ALERT_LEVEL = 2;
                                if (LifeSupportDisplay.WarpCancel && days_rem < HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().warpCancellationLeadTime && TimeWarp.CurrentRate > 1)
                                {
                                    TimeWarp.SetRate(0, false);
                                }
                            }
                            if (LS_ALERT_LEVEL < 3 && days_rem <= 1)
                            {
                                IFI_Button.SetTexture(IFI_button_danger);
                                LS_ALERT_LEVEL = 3;
                                if (LifeSupportDisplay.WarpCancel && days_rem < HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().warpCancellationLeadTime && TimeWarp.CurrentRate > 1)
                                {
                                    TimeWarp.SetRate(0, false);
                                }
                            }
                        }
                    }

                }
            }
        }

        public void Awake()
        {
            IFITimer = Convert.ToInt32(Planetarium.GetUniversalTime());
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReady);
            DontDestroyOnLoad(this);

            CancelInvoke();
            InvokeRepeating("display_active", 1, 1);
        }

        public void Start()
        {
            Instance = this;
            if (!IFI_Texture_Load)
            {
                if (IFI_button_grn == null)
                {
                    IFI_button_grn = new Texture2D(38, 38, TextureFormat.ARGB32, false);
                    IFI_button_cau = new Texture2D(38, 38, TextureFormat.ARGB32, false);
                    IFI_button_danger = new Texture2D(38, 38, TextureFormat.ARGB32, false);
                }
                double IHOLD = IFI_Get_Elasped_Time();

                if (GameDatabase.Instance.ExistsTexture("IFILS/Textures/IFI_LS_GRN")) IFI_button_grn = GameDatabase.Instance.GetTexture("IFILS/Textures/IFI_LS_GRN", false);
                if (GameDatabase.Instance.ExistsTexture("IFILS/Textures/IFI_LS_CAU")) IFI_button_cau = GameDatabase.Instance.GetTexture("IFILS/Textures/IFI_LS_CAU", false);
                if (GameDatabase.Instance.ExistsTexture("IFILS/Textures/IFI_LS_DAN")) IFI_button_danger = GameDatabase.Instance.GetTexture("IFILS/Textures/IFI_LS_DAN", false);
                IFI_Texture_Load = true;
            }
            LS_ID = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;

        }




        private double IFIGetAllResources(string IFIResource, Vessel IV, bool ISLoaded)
        {
            double IFIResourceAmt = 0.0;
            double num2 = 0;
            int id = PartResourceLibrary.Instance.GetDefinition(IFIResource).id;
            if (ISLoaded)
            {
                if (IV == null)
                {
                    Log.Info("IV is null");
                    return 0;
                }
                if (IV.rootPart == null)
                {
                    Log.Info("rootPart is null");
                    return 0;
                }
                IV.GetConnectedResourceTotals(id, out IFIResourceAmt, out num2, true);
                //IV.rootPart.GetConnectedResourceTotals(id, out IFIResourceAmt, out num2, true);
                Log.Info("IFIGetAllResources, IFIResource: " + IFIResource + ",   IFIResourceAmt: " + IFIResourceAmt);
            }
            else
            {
                for (int idx3 = 0; idx3 < IV.protoVessel.protoPartSnapshots.Count; idx3++)
                //foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                {
                    ProtoPartSnapshot p = IV.protoVessel.protoPartSnapshots[idx3];

                    for (int idx4 = 0; idx4 < p.resources.Count; idx4++)
                    //foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        ProtoPartResourceSnapshot r = p.resources[idx4];
                        if (r.resourceName == IFIResource)
                        {
                            IFIResourceAmt += r.amount;
                        }
                    }
                }
            }
#if false
            if (ISLoaded)
            {
                if (IV.vesselType != VesselType.EVA)
                {
                    foreach (Part p in IV.parts)
                    {
                        foreach (PartResource pr in p.Resources)
                        {
                            if (pr.resourceName == IFIResource)
                            {
                                if (pr.flowState)
                                {
                                    IFIResourceAmt += pr.amount;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (PartResource pr in IV.rootPart.Resources)
                    {
                        if (pr.resourceName.Equals(IFIResource))
                        {
                            if (pr.flowState)
                            {
                                IFIResourceAmt += pr.amount;
                            }
                        }
                    }
                }
            }
            else // Kerbal EVA code
            {

                foreach (ProtoPartSnapshot p in IV.protoVessel.protoPartSnapshots)
                {

                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (r.resourceName == IFIResource)
                        {


                            double IHold = r.amount;
                            IFIResourceAmt += IHold;
                        }
                    }
                }

            }
#endif
            return IFIResourceAmt;
        }

        private double IFIUSEResources(string IFIResource, Vessel IV, bool ISLoaded, double UR_Amount, int CREWHOLD)
        {
            double Temp_Resource = UR_Amount;

            if (ISLoaded)
            {

                double ALL_Resources = IFIGetAllResources(IFIResource, IV, true);
                Log.Info("IFIUSEResources: Vessel: " + IV.vesselName + ",  crewCount: " + IV.rootPart.protoModuleCrew.Count() + ",  IFIResource: " + IFIResource);

#if DEBUG
                foreach (var p in IV.Parts)
                {
                    Log.Info("Part: " + p.partInfo.name + ", crewCount: " + p.protoModuleCrew.Count());
                }
#endif
                if (IFIResource == Constants.LIFESUPPORT)
                {
                    if (ALL_Resources == 0.0)
                    {
                        IFI_Check_Kerbals(IV, UR_Amount);
                        return 0.0;
                    }
                    else
                    {
                        Log.Info("Vessel: " + IV.vesselName + ",  crewCount: " + IV.rootPart.protoModuleCrew.Count());

                        List<string> toDelete = new List<string>();
                        // check to see if this kerbal is on the vessel
                        for (int idx = 0; idx < IV.Parts.Count; idx++)
                        //foreach (var p in IV.Parts)
                        {
                            var p = IV.Parts[idx];
                            for (int i1 = p.protoModuleCrew.Count(); i1 > 0; i1--)
                            {
                                StarvingKerbal sk;
                                if (starvingKerbals.TryGetValue(p.protoModuleCrew[i1 - 1].name, out sk))
                                {
                                    Log.Info("Vessel: " + IV.vesselName + ",   protoModuleCrew: " + p.protoModuleCrew[i1 - 1].name);


                                    {
                                        toDelete.Add(sk.name);
                                        p.protoModuleCrew[i1 - 1].type = ProtoCrewMember.KerbalType.Crew;
                                        KerbalRoster.SetExperienceTrait(p.protoModuleCrew[i1 - 1], sk.trait);

                                        IFIDebug.IFIMess(IV.vesselName + " Kerbal returned to crew status due to LS - " + sk.name + ", trait restored to: " + sk.trait);
                                        string message = ""; message += IV.vesselName + "\n\n"; message += sk.name + "\n Was returned to duty due to ::";
                                        message += "Life Support Restored";
                                        message += "::";
                                        MessageSystem.Message m = new MessageSystem.Message("Kerbal returned to duty from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                                        MessageSystem.Instance.AddMessage(m);
                                    }
                                }
                            }
                        }
                        foreach (var s in toDelete)
                            starvingKerbals.Remove(s);
                        toDelete.Clear();

                    }
                }
                if (ALL_Resources < UR_Amount)
                {
                    double TEST_Mod = (UR_Amount - ALL_Resources) * 100000;
                    Temp_Resource = IV.rootPart.RequestResource(IFIResource, ALL_Resources);
                }
                else
                {
                    Temp_Resource = IV.rootPart.RequestResource(IFIResource, UR_Amount);
                }
            }
            else
            {
                KerbalEVARescueDetect = false;
                int PartCountForShip = 0;
                for (int idx = 0; idx < IV.protoVessel.protoPartSnapshots.Count; idx++)
                //foreach (ProtoPartSnapshot p in IV.protoVessel.protoPartSnapshots)
                {
                    ProtoPartSnapshot p = IV.protoVessel.protoPartSnapshots[idx];
                    PartCountForShip++;

                    for (int idx2 = 0; idx2 < p.resources.Count; idx2++)
                    //foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        ProtoPartResourceSnapshot r = p.resources[idx2];
                        if (r.resourceName == IFIResource)
                        {
                            if (UR_Amount <= 0.0) break;
                            double IHold = r.amount;
                            // Fix for Kerbal rescue Missions
                            Int16 RescueTest = -1;
                            ConfigNode RT = p.pVesselRef.discoveryInfo;
                            System.Int16.TryParse(RT.GetValue("state"), out RescueTest);

                            if (PartCountForShip <= 2 && CREWHOLD == 1 && IHold <= 0.0 && RescueTest == 29)
                            {
                                // Add Resources to Rescue Contract POD 1 time
                                IFIDebug.IFIMess("#### IFI LIfeSupport #### Rescue POD Found with No LS Resource TAG Flagging --" + Convert.ToString(RescueTest));
                                KerbalEVARescueDetect = true;
                                IHold = r.maxAmount;
                                r.amount = IHold;
                                UR_Amount = 0.0;
                                return 0.0;
                            }
                            if (RescueTest == 29)
                            {
                                KerbalEVARescueDetect = true;
                                return 0.0;
                            } // DO NOT USE LS ON RESCUE POD TILL player gets  CLOSE
                            UR_Amount -= IHold;
                            if (UR_Amount <= 0.0)
                            {
                                IHold -= Temp_Resource;
                                r.amount = IHold;
                                UR_Amount = 0.0;
                            }
                            else
                            {
                                r.amount = 0.0;
                            }

                            Temp_Resource = UR_Amount;
                        }
                    }
                    //
                    if (UR_Amount <= 0.0) break;
                }
                //if (IV.isEVA && )
                //{
                //    IFIDebug.IFIMess("#### IFI LIfeSupport #### Rescue Kerbal Found with No LS Resource TAG Flagging");
                //    UR_Amount = 0.0;
                //}
            }
            return UR_Amount;
        }

        public class StarvingKerbal
        {
            public string name;
            public string trait;
            public double startTime;

            public StarvingKerbal(string n, string t)
            {
                name = n;
                trait = t;
                startTime = Planetarium.GetUniversalTime();
            }
        }

        static Dictionary<string, StarvingKerbal> starvingKerbals = new Dictionary<string, StarvingKerbal>();
        private void CrewTestEVA(Vessel IV, double l)
        {
#if true
            StarvingKerbal sk;
            if (starvingKerbals.TryGetValue(IV.rootPart.protoModuleCrew[0].name, out sk))
            {
                if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().inactiveTimeBeforeDeath > Planetarium.GetUniversalTime())
                {
                    return;
                }
            }
            else
            {
                if (IV.loaded)
                {
                    Part p = IV.rootPart;
                    ProtoCrewMember iCrew = p.protoModuleCrew[0];

                    sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                    IFIDebug.IFIMess(" EVA Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = "\n\n\n";
                    message += iCrew.name + ":\n Was turned into a  tourist for Life Support Failure.";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed to Tourist on EVA", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                }
            }
#endif
            float rand;
            int CUR_CWLS = IFICWLS;
            CUR_CWLS += (Convert.ToInt16(l) * 10);
            rand = UnityEngine.Random.Range(0.0f, 100.0f);
            if (CUR_CWLS > rand)
            {
                if (IV.loaded)
                {
                    Part p = IV.rootPart;
                    ProtoCrewMember iCrew = p.protoModuleCrew[0];

                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().EVAkerbalsCanDie)
                    {
                        iCrew.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        p.Die();
                        IFIDebug.IFIMess(" EVA Kerbal Killed due to no LS - " + iCrew.name);
                        string message = "\n\n\n";
                        message += iCrew.name + ":\n Was killed for Life Support Failure.";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death on EVA", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                    }
#if false
                    else
                    {
                        StarvingKerbal sk;
                        if (!starvingKerbals.TryGetValue(iCrew.name, out sk))
                        {
                            sk = new StarvingKerbal(iCrew.name, iCrew.trait);

                            IFIDebug.IFIMess(" EVA Kerbal turned into tourist due to no LS - " + iCrew.name);
                            string message = "\n\n\n";
                            message += iCrew.name + ":\n Was turned into a  tourist for Life Support Failure.";
                            MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed to Tourist on EVA", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                            MessageSystem.Instance.AddMessage(m);
                            Log.Info("Old experienceTrait: " + iCrew.trait);
                            starvingKerbals.Add(sk.name, sk);
                            iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                            KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                        }
                    }
#endif
                }
                else
                {
                    // Removed Killing Kerbals on EVA when not loaded to fix ghosting bug. 
                }
            }

        }

        private void CrewTest(int REASON, Part p, double l)
        {
            int CUR_CWLS = IFICWLS;
            CUR_CWLS += (Convert.ToInt16(l) * 10);
            float rand;
            ProtoCrewMember iCrew;
            for (int i = 0; i < p.protoModuleCrew.Count; i++)
            {
#if true
                iCrew = p.protoModuleCrew[i];
                StarvingKerbal sk;
                if (starvingKerbals.TryGetValue(iCrew.name, out sk))
                {
                    if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().inactiveTimeBeforeDeath > Planetarium.GetUniversalTime())
                    {
                        return;
                    }
                }
                else
                {
                    sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                    IFIDebug.IFIMess(p.vessel.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                    message += "No Life Support Remaining";
                    message += "::";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                }
#endif
                rand = UnityEngine.Random.Range(0.0f, 100.0f);
                IFIDebug.IFIMess("!!!!!!!!");
                IFIDebug.IFIMess("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                IFIDebug.IFIMess("Crew Death Chance = " + Convert.ToString(CUR_CWLS));
                IFIDebug.IFIMess("Crew Death Roll = " + Convert.ToString(rand));
                IFIDebug.IFIMess("!!!!!!!!");

                if (CUR_CWLS > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        p.RemoveCrewmember(iCrew);// Remove crew from part
                        iCrew.Die();  // Kill crew after removal or death will reset to active.
                        IFIDebug.IFIMess(p.vessel.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Life Support Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                    }
#if false
                    else
                    {
                        StarvingKerbal sk;
                        if (!starvingKerbals.TryGetValue(iCrew.name, out sk))
                        {
                            sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                            Log.Info("Old experienceTrait: " + iCrew.trait);
                            starvingKerbals.Add(sk.name, sk);
                            iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                            KerbalRoster.SetExperienceTrait(iCrew, "Tourist");

                            IFIDebug.IFIMess(p.vessel.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                            string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                            message += "No Life Support Remaining";
                            message += "::";
                            MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                            MessageSystem.Instance.AddMessage(m);
                        }
                    }
#endif
                }
            }
        }

        private void CrewTestProto(int REASON, ProtoPartSnapshot p, double l)
        {

            int CUR_CWLS = IFICWLS;
            CUR_CWLS += (Convert.ToInt16(l) * 10);
            float rand;

            ProtoCrewMember iCrew;
            for (int i = 0; i < p.protoModuleCrew.Count; i++)
            {

#if true
                iCrew = p.protoModuleCrew[i];
                StarvingKerbal sk;
                if (starvingKerbals.TryGetValue(iCrew.name, out sk))
                {
                    if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().inactiveTimeBeforeDeath > Planetarium.GetUniversalTime())
                    {
                        return;
                    }
                }
                else
                {
                    sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                    IFIDebug.IFIMess(p.pVesselRef.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                    message += "No Life Support Remaining";
                    message += "::";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                }
#endif


                rand = UnityEngine.Random.Range(0.0f, 100.0f);
                IFIDebug.IFIMess("!!!!!!!!");
                IFIDebug.IFIMess("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                IFIDebug.IFIMess("Crew Death Chance = " + Convert.ToString(CUR_CWLS));
                IFIDebug.IFIMess("Crew Death Roll = " + Convert.ToString(rand));
                IFIDebug.IFIMess("!!!!!!!!");

                if (CUR_CWLS > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        iCrew.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        p.RemoveCrew(iCrew);

                        IFIDebug.IFIMess(p.pVesselRef.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Life Support Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport Failure", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                    }
#if false
                    else
                    {
                        StarvingKerbal sk;
                        if (!starvingKerbals.TryGetValue(iCrew.name, out sk))
                        {
                            sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                            Log.Info("Old experienceTrait: " + iCrew.trait);
                            starvingKerbals.Add(sk.name, sk);
                            iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                            KerbalRoster.SetExperienceTrait(iCrew, "Tourist");

                            IFIDebug.IFIMess(p.pVesselRef.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                            string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                            message += "No Life Support Remaining";
                            message += "::";
                            MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                            MessageSystem.Instance.AddMessage(m);
                        }
                    }
#endif
                }
            }
        }

        private void IFI_Check_Kerbals(Vessel IV, double l) // Find All Kerbals Hiding on Vessel 
        {
            if (IV.vesselType == VesselType.EVA)
            {
                try
                {
                    // following was commented out, need to test
                    CrewTestEVA(IV, l);
                }
                catch (Exception ex) { IFIDebug.IFIMess("Vessel IFI Exception ++Finding Kerbals++ eva " + ex.Message); }
            }
            else
            {
                if (IV.loaded)
                {
                    for (int idx = 0; idx < IV.parts.Count; idx++)
                    //foreach (Part p in IV.parts)
                    {
                        Part p = IV.parts[idx];
                        int IFIcrew = p.protoModuleCrew.Count;
                        if (IFIcrew > 0)
                        {
                            CrewTest(0, p, l);
                        }

                    }
                }
                else
                {
                    for (int idx = 0; idx < IV.protoVessel.protoPartSnapshots.Count; idx++)
                    //foreach (ProtoPartSnapshot p in IV.protoVessel.protoPartSnapshots)
                    {
                        ProtoPartSnapshot p = IV.protoVessel.protoPartSnapshots[idx];
                        int IFIcrew = p.protoModuleCrew.Count;
                        if (IFIcrew > 0)
                        {
                            CrewTestProto(0, p, l);
                        }

                    }
                }
            }
        }
        Vessel lastVesselChecked = null;
        IFILS1.LifeSupportLevel lastLSlevel = IFILS1.LifeSupportLevel.none;
        bool lastShowInResourcePanel = false;
        public void display_active()
        {
            IFITIM++;
            Log.Info("display_active, IFITIM: " + IFITIM);
            if (!HighLogic.LoadedSceneIsEditor && ((/* LifeSupportDisplay.LSDisplayActive && */ IFITIM > 3) || (TimeWarp.CurrentRate > 800) || IFITIM > 100))
            {
                Life_Support_Update();
                IFITIM = 0;
            }
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
                Went_to_Main = true;



            // HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level > IFILS1.LifeSupportLevel.classic && 

            if (HighLogic.LoadedScene == GameScenes.FLIGHT &&
                (lastVesselChecked != FlightGlobals.ActiveVessel ||
                lastLSlevel != HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level ||
                lastShowInResourcePanel != HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel))
            {
                lastVesselChecked = FlightGlobals.ActiveVessel;
                lastLSlevel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level;
                lastShowInResourcePanel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel;

                for (int idx = 0; idx < FlightGlobals.ActiveVessel.Parts.Count; idx++)
                //foreach (var p in FlightGlobals.ActiveVessel.Parts)
                {
                    var p = FlightGlobals.ActiveVessel.Parts[idx];

                    for (int idx2 = 0; idx2 < p.Resources.Count; idx2++)
                    //foreach (var r in p.Resources)
                    {
                        var r = p.Resources[idx2];

                        if (r.resourceName == "Sludge")
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel &&
                                HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                                r.isVisible = true;
                            else
                                r.isVisible = false;

                        }
                        if (r.resourceName == Constants.SLURRY)
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel &&
                                HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                                r.isVisible = true;
                            else
                                r.isVisible = false;
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (LifeSupportDisplay.LSDisplayActive)
            {

                vSmallScrollBar = new GUIStyle(GUI.skin.verticalScrollbar);
                vSmallScrollBar.fixedWidth = 8f;

                hSmallScrollBar = new GUIStyle(GUI.skin.horizontalScrollbar);
                hSmallScrollBar.fixedHeight = 0f;
                GUI.skin = HighLogic.Skin;
                if (!HighLogic.LoadedSceneIsEditor && HighLogic.LoadedSceneIsGame)
                {
                    string TITLE = "IFI Life Support Vessel Status Display ";

                    LifeSupportDisplay.infoWindowPos = GUILayout.Window(99988, LifeSupportDisplay.infoWindowPos, LSInfoWindow, TITLE); //, LifeSupportDisplay.layoutOptions);
                }
                else
                    if (HighLogic.LoadedSceneIsEditor)
                {
                    string TITLE = "IFI Life Support Vessel Info ";

                    LifeSupportDisplay.editorInfoWindowPos = GUILayout.Window(99988, LifeSupportDisplay.editorInfoWindowPos, LSEditorInfoWindow, TITLE); //, LifeSupportDisplay.layoutOptions);

                }
            }
        }

        public void InitStatusCache()
        {
            Log.Info("InitStatusCache");
            if (LS_Status_Cache != null)
                LS_Status_Cache.Clear();
            else
                LS_Status_Cache = new List<LS_Status_Row>();
        }
        public void ClearStageSummaryList()
        {
            maxStage = -1;
            if (stageSummaryList != null)
                stageSummaryList.Clear();
            else
                stageSummaryList = new Dictionary<int, StageSummary>();
        }
        void ClearStatusWidths()
        {
            for (int i = 0; i < MAX_STATUSES; i++)
            {
                LS_Status_Spacing[i] = 0;
                LS_Status_Width[i] = 0;
            }
        }

        LS_Status_Row lsStatusRow;

        void GetWidth(string s, int i, bool init = false, bool InitStatusRow = false)
        {
            float minWidth, maxWidth;

            if (InitStatusRow)
            {
                lsStatusRow = new LS_Status_Row();
            }

            GUI.skin.label.CalcMinMaxWidth(new GUIContent(s), out minWidth, out maxWidth);
            maxWidth += 10;
            if (init)
                LS_Status_Width[i] = (int)maxWidth;
            else
                LS_Status_Width[i] = Math.Max(LS_Status_Width[i], (int)maxWidth);
            lsStatusRow.data[i] = s;
        }
        void AddSpacing(int col, int spacing)
        {
            LS_Status_Spacing[col] += spacing;
        }
        void FinishStatusRow()
        {
            LS_Status_Cache.Add(lsStatusRow);
            lsStatusRow = null;
        }

        static bool fullDisplay = false;

        class StageSummary
        {
            public int stage;
            public int crew;
            public double LifeSupport;
            public double OrganicSlurry;
            public double Sludge;
            public double SlurryProcessRate;
            public double SludgeProcessRate;

            public double LifeSupportOutputRate;
            public double SludgeOutputRate;

            public StageSummary(int s)
            {
                stage = s;
                crew = 0;
                LifeSupport = 0;
                OrganicSlurry = 0;
                Sludge = 0;
                SlurryProcessRate = 0;
                SludgeProcessRate = 0;
                LifeSupportOutputRate = 0;
                SludgeOutputRate = 0;
            }
        }
        Dictionary<int, StageSummary> stageSummaryList = new Dictionary<int, StageSummary>();
        int maxStage = -1;
        public void GetStageSummary()
        {
            Log.Info("GetStageSummary");
            List<Part> parts = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts : new List<Part>();
            maxStage = -1;
            if (parts != null && parts.Count > 0)
            {
                Log.Info("Parts.count: " + parts.Count);
                foreach (Part part in parts)
                {
                    Log.Info("part: " + part.partInfo.name);
                    int stage;
                    if (LifeSupportDisplay.Summarize)
                        stage = 0;
                    else
                        stage = part.inverseStage + 1;
                    maxStage = Math.Max(maxStage, stage);
                    StageSummary stageSummary;
                    bool newStage = false;
                    if (!stageSummaryList.TryGetValue(stage, out stageSummary))
                    {
                        stageSummary = new StageSummary(stage);
                        newStage = true;
                    }
                    stageSummary.crew += part.CrewCapacity;
                    foreach (PartResource partResource in part.Resources)
                    {
                        if (partResource.resourceName == Constants.LIFESUPPORT)
                            stageSummary.LifeSupport += partResource.maxAmount;
                        if (partResource.resourceName == Constants.SLURRY)
                            stageSummary.OrganicSlurry += partResource.maxAmount;
                        if (partResource.resourceName == Constants.SLUDGE)
                            stageSummary.Sludge += partResource.maxAmount;
                    }
                    for (int m = 0; m < part.Modules.Count; m++)
                    {
                        if (part.Modules[m].moduleName == "ModuleResourceConverter")
                        {
                            ModuleResourceConverter m1 = (ModuleResourceConverter)part.Modules[m];
                            for (int i = 0; i < m1.inputList.Count; i++)
                            {
                                ResourceRatio inp = m1.inputList[i];
                                if (inp.ResourceName == Constants.SLURRY)
                                    stageSummary.SlurryProcessRate += inp.Ratio;
                                if (inp.ResourceName == Constants.SLUDGE)
                                    stageSummary.SludgeProcessRate += inp.Ratio;
                            }
                            for (int i = 0; i < m1.outputList.Count; i++)
                            {
                                ResourceRatio output = m1.outputList[i];
                                if (output.ResourceName == Constants.LIFESUPPORT)
                                    stageSummary.LifeSupportOutputRate += output.Ratio;
                                if (output.ResourceName == Constants.SLUDGE)
                                    stageSummary.SludgeOutputRate += output.Ratio;
                            }
                        }
                        if (part.Modules[m].moduleName == "AnimatedGenerator")
                        {
                            AnimatedGenerator m1 = (AnimatedGenerator)part.Modules[m];

                            for (int i = 0; i < m1.inputList.Count; i++)
                            {
                                ResourceRatio inp = m1.inputList[i];

                                if (inp.ResourceName == Constants.SLURRY)
                                    stageSummary.SlurryProcessRate += inp.Ratio;
                                if (inp.ResourceName == Constants.SLUDGE)
                                    stageSummary.SludgeProcessRate += inp.Ratio;
                            }
                            for (int i = 0; i < m1.outputList.Count; i++)
                            {
                                ResourceRatio output = m1.outputList[i];
                                if (output.ResourceName == Constants.LIFESUPPORT)
                                    stageSummary.LifeSupportOutputRate += output.Ratio;
                                if (output.ResourceName == Constants.SLUDGE)
                                    stageSummary.SludgeOutputRate += output.Ratio;
                            }
                        }
                    }
                    if (newStage)
                    {
                        if (stageSummary.crew > 0 || stageSummary.LifeSupport > 0 || stageSummary.OrganicSlurry > 0 || stageSummary.Sludge > 0 ||
                            stageSummary.SlurryProcessRate > 0 || stageSummary.SludgeProcessRate > 0 ||
                            stageSummary.LifeSupportOutputRate > 0 || stageSummary.SludgeOutputRate > 0)
                            stageSummaryList.Add(stage, stageSummary);
                        stageSummary = null;
                    }

                }
            }
        }
        string Colorized(string color, string txt)
        {
            return String.Format("<color=#{0}>{1}</color>", color, txt);
        }
        void GetColorized(int i, ref string txt)
        {
            switch (i)
            {
                case SLURRYAVAIL:
                case SLURRYCONVRATE:
                case SLURRY_DAYS_TO_PROCESS:
                    txt = Colorized(lblGreenColor, txt);
                    break;
                case SLUDGEAVAIL:
                case SLUDGECONVRATE:
                case SLUDGE_DAYS_TO_PROCESS:
                case SLUDGE_OUTPUT_RATE:
                    txt = Colorized(lblDrkGreenColor, txt);
                    break;
                case LIFESUPPORT_OUTPUT_RATE:
                    txt = Colorized(lblBlueColor, txt);
                    break;
            }
        }
        const int SPACING = 30;
        private void LSEditorInfoWindow(int windowId)
        {

            var bold = new GUIStyle(GUI.skin.label);
            bold.fontStyle = FontStyle.Bold;

            InitStatusCache();
            ClearStatusWidths();
            double secsPerDay = 3600 * (GameSettings.KERBIN_TIME ? 6 : 24);

            GetWidth("Stage", LOCATION, true, true);
            GetWidth("Crew", CREW, true);
            GetWidth("   Life\nSupport", LSAVAIL, true);
            GetWidth("Days", DAYS_REM, true);
            if (LifeSupportDisplay.ShowRecyclers && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
            {
                AddSpacing(DAYS_REM, SPACING);
                GetWidth("   Slurry\nCapacity", SLURRYAVAIL, true);
                GetWidth("Daily\nProcess\nRate", SLURRY_DAYS_TO_PROCESS, true);
                AddSpacing(SLURRY_DAYS_TO_PROCESS, SPACING);
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                {
                    GetWidth("Sludge\nOutput\nRate", SLUDGE_OUTPUT_RATE, true);
                    GetWidth("Sludge\nCapacity", SLUDGEAVAIL, true);
                    GetWidth("Daily\nProcess\nRate", SLUDGE_PROCESS_RATE, true);
                    AddSpacing(SLUDGE_PROCESS_RATE, SPACING);
                }

                GetWidth("LifeSupport\nDaily Output\nRate", LIFESUPPORT_OUTPUT_RATE, true, false);

            }
            FinishStatusRow();

            for (int i = 0; i <= maxStage; i++)
            {
                StageSummary ss;
                if (stageSummaryList.TryGetValue(i, out ss))
                {

                    GetWidth(ss.stage.ToString(), LOCATION, false, true);
                    GetWidth(ss.crew.ToString(), CREW);
                    GetWidth(ss.LifeSupport.ToString(), LSAVAIL);
                    if (ss.crew > 0)
                        GetWidth((ss.LifeSupport / ss.crew).ToString("n2"), DAYS_REM);
                    else
                        GetWidth("n/a", DAYS_REM);
                    if (LifeSupportDisplay.ShowRecyclers && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                    {
                        GetWidth(ss.OrganicSlurry.ToString(), SLURRYAVAIL);
                        GetWidth((secsPerDay * ss.SlurryProcessRate).ToString("n2"), SLURRY_DAYS_TO_PROCESS);
                        if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                        {
                            GetWidth((secsPerDay * ss.SludgeOutputRate).ToString("n2"), SLUDGE_OUTPUT_RATE);
                            GetWidth(ss.Sludge.ToString(), SLUDGEAVAIL);
                            if (ss.SludgeProcessRate > 0)
                                GetWidth((secsPerDay * ss.SludgeProcessRate).ToString("n2"), SLUDGE_PROCESS_RATE);
                            else
                                GetWidth("n/a", SLUDGE_PROCESS_RATE);
                        }

                        GetWidth((secsPerDay * ss.LifeSupportOutputRate).ToString("n2"), LIFESUPPORT_OUTPUT_RATE);
                    }
                    FinishStatusRow();
                }
            }


            GUILayout.BeginVertical();
            Log.Info("Displaying headers");

            GUILayout.BeginHorizontal();
            for (int i = 0; i < MAX_STATUSES; i++)
            {
                LS_Status_Width[i] += LS_Status_Spacing[i];
                if (LS_Status_Cache[0].data[i] != null)
                {
                    Log.Info("i: " + LS_Status_Cache[0].data[i]);
                    string txt = LS_Status_Cache[0].data[i];

                    GetColorized(i, ref txt);

                    GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                }
            }
            GUILayout.Label(" ", GUILayout.Width(15));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            LifeSupportDisplay.infoScrollPos = GUILayout.BeginScrollView(LifeSupportDisplay.infoScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(345));
            for (int i1 = 1; i1 < LS_Status_Cache.Count; i1++)
            {
                GUILayout.BeginHorizontal();
                for (int i = 1; i < MAX_STATUSES; i++)
                {
                    if (LS_Status_Cache[i1].data[i] != null)
                    {
                        string txt = LS_Status_Cache[i1].data[i];
                        GetColorized(i, ref txt);
                        GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            bool b = GUILayout.Toggle(LifeSupportDisplay.Summarize, "Summarize");
            if (b != LifeSupportDisplay.Summarize)
            {
                LifeSupportDisplay.Summarize = b;
                InitStatusCache();
                ClearStageSummaryList();
                GetStageSummary();
            }
            GUILayout.FlexibleSpace();
            b = GUILayout.Toggle(LifeSupportDisplay.ShowRecyclers, "Show Recyclers");
            if (b != LifeSupportDisplay.ShowRecyclers)
            {
                LifeSupportDisplay.ShowRecyclers = b;
                Editor.Instance.DefineFilters();
                LifeSupportDisplay.ReinitEditorInfoWindowPos();
            }
            //GUILayout.FlexibleSpace();
            //GUILayout.Button(">>", GUILayout.Width(22), GUILayout.Height(22));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUI.DragWindow();
        }


        private void LSInfoWindow(int windowId)
        {
            var bold = new GUIStyle(GUI.skin.label);
            bold.fontStyle = FontStyle.Bold;
            InitStatusCache();

            //var rightJustify = new GUIStyle(GUI.skin.label);
            //rightJustify.alignment = TextAnchor.MiddleRight;

            GetWidth("Vessel", VESSEL, true, true);
            GetWidth("Location", LOCATION, true);
            GetWidth("Crew", CREW, true);
            GetWidth("   Life\nSupport", LSAVAIL, true);
            GetWidth("     Days\nRemaining", DAYS_REM, true);
            if (fullDisplay && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
            {
                GetWidth("Slurry (rate)", SLURRYAVAIL, true);
                GetWidth("Process\n Time", SLURRY_DAYS_TO_PROCESS, true);
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                {
                    GetWidth("Sludge (rate)", SLUDGEAVAIL, true);
                    GetWidth("Process\n Time", SLUDGE_DAYS_TO_PROCESS, true, false);
                }
            }
            FinishStatusRow();
            for (int LLC = 0; LLC < LS_Status_Hold_Count; LLC++)
            {
                GetWidth(LS_Status_Hold[LLC, VESSEL], VESSEL, false, true);
                GetWidth(LS_Status_Hold[LLC, LOCATION], LOCATION);
                GetWidth("  " + LS_Status_Hold[LLC, CREW], CREW);
                GetWidth("  " + LS_Status_Hold[LLC, LSAVAIL], LSAVAIL);
                GetWidth("  " + LS_Status_Hold[LLC, DAYS_REM], DAYS_REM);

                if (fullDisplay && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                {
                    if (LS_Status_Hold[LLC, SLURRYAVAIL] != null && LS_Status_Hold[LLC, SLURRYAVAIL] != "")
                    {
                        if (LS_Status_Hold[LLC, SLURRYCONVRATE] != null && LS_Status_Hold[LLC, SLURRYCONVRATE] != "")
                            GetWidth(LS_Status_Hold[LLC, SLURRYAVAIL] + "(" + LS_Status_Hold[LLC, SLURRYCONVRATE] + ")", SLURRYAVAIL);
                        else
                            GetWidth(" ", SLURRYAVAIL);
                    }
                    else
                        GetWidth(" ", SLURRYAVAIL);

                    if (LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] != null && LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] != "n/a" && LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] != "")
                        GetWidth(LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] + "d", SLURRY_DAYS_TO_PROCESS);
                    else
                        GetWidth(" ", SLURRY_DAYS_TO_PROCESS);

                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                    {
                        if (LS_Status_Hold[LLC, SLUDGEAVAIL] != null && LS_Status_Hold[LLC, SLUDGEAVAIL] != "")
                        {
                            if (LS_Status_Hold[LLC, SLUDGECONVRATE] != null && LS_Status_Hold[LLC, SLUDGECONVRATE] != "")
                                GetWidth(LS_Status_Hold[LLC, SLUDGEAVAIL] + "(" + LS_Status_Hold[LLC, SLUDGECONVRATE] + ")", SLUDGEAVAIL);
                            else
                                GetWidth(LS_Status_Hold[LLC, SLUDGEAVAIL], SLUDGEAVAIL);
                        }
                        else
                            GetWidth(" ", SLUDGEAVAIL);

                        if (LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] != null && LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] != "n/a" && LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] != "")
                            GetWidth(LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] + "d", SLUDGE_DAYS_TO_PROCESS);
                        else
                            GetWidth(" ", SLUDGE_DAYS_TO_PROCESS);
                    }
                }
                FinishStatusRow();
            }

            GUILayout.BeginVertical();
            Log.Info("Displaying headers");

            GUILayout.BeginHorizontal();
            for (int i = 0; i < MAX_STATUSES; i++)
            {
                LS_Status_Width[i] += LS_Status_Spacing[i];
                if (LS_Status_Cache[0].data[i] != null)
                {
                    Log.Info("i: " + LS_Status_Cache[0].data[i]);
                    string txt = LS_Status_Cache[0].data[i];

                    GetColorized(i, ref txt);

                    GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                }
            }
            GUILayout.Label(" ", GUILayout.Width(15));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            LifeSupportDisplay.infoScrollPos = GUILayout.BeginScrollView(LifeSupportDisplay.infoScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(345));
            for (int i1 = 1; i1 < LS_Status_Cache.Count; i1++)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < MAX_STATUSES; i++)
                {
                    if (LS_Status_Cache[i1].data[i] != null)
                    {
                        string txt = LS_Status_Cache[i1].data[i];
                        GetColorized(i, ref txt);
                        GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            LifeSupportDisplay.WarpCancel = GUILayout.Toggle(LifeSupportDisplay.WarpCancel, "Auto Cancel Warp on Low Life Support");
            GUILayout.FlexibleSpace();
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.classic)
            {

                if (fullDisplay)
                {
                    if (GUILayout.Button("<<", GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        fullDisplay = false;
                        LifeSupportDisplay.ReinitInfoWindowPos();
                    }
                }
                else
                {
                    if (GUILayout.Button(">>", GUILayout.Width(22), GUILayout.Height(22)))
                        fullDisplay = true;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private double IFI_Get_Elasped_Time()
        {
            double CurrTime = Planetarium.GetUniversalTime();
            double IHOLD = CurrTime - IFITimer;

            IFITimer = CurrTime;
            return IHOLD;
        }


    }
}