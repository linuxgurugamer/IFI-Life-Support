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
        private int[] LS_Status_Width;
        private int LS_Status_Hold_Count;
        // Make sure LS remaining Display conforms to Kerbin time setting.
        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }
        private bool Went_to_Main = false;

        GUIStyle smallScrollBar;
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
        const int SLURRYCONVRATE = 9;
        const int SLUDGECONVRATE = 10;
        const int MAX_STATUSES = 11;

  

        private void OnGUIApplicationLauncherReady()
        {
            // Create the button in the KSP AppLauncher

            if (IFI_Button == null)
            {
                IFI_Button = ApplicationLauncher.Instance.AddModApplication(GUIToggle, GUIToggle,
                                null, null,
                                null, null,
                                ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.MAPVIEW,
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
        }

        private void ResetButton()
        {
            IFI_Button.SetTexture(IFI_button_grn);
        }

        public void Life_Support_Update()
        {
            Log.Info("Life_Support_Update 1");
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
            Log.Info("Life_Support_Update 2");
            double Elapsed_Time = IFI_Get_Elasped_Time();
            IFI_Button.SetTexture(IFI_button_grn);

            int LS_ALERT_LEVEL = 1;
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                LS_Status_Hold = new string[FlightGlobals.Vessels.Count(), MAX_STATUSES];
                LS_Status_Width = new int[MAX_STATUSES];

                LS_Status_Hold_Count = 0;
                Debug.Log("######## Looking for Ships ######");
                foreach (Vessel vessel in FlightGlobals.Vessels)
                {

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
                            var lblGreenColor = "00ff00";
                            var lblBlueColor = "3DB1FF";
                            var lblYellowColor = "FFD966";
                            var lblRedColor = "f90000";

                            LS_Status_Hold[LS_Status_Hold_Count, VESSEL] = String.Format("<color=#{0}>{1}</color>", lblGreenColor, TVname);  //TVname;

                            LS_Status_Hold[LS_Status_Hold_Count, LOCATION] = String.Format("<color=#{0}>{1}</color>", lblBlueColor, IFI_Location); // IFI_Location;
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
                                LS_Status_Hold[LS_Status_Hold_Count, LSAVAIL] = String.Format("<color=#{0}>{1}</color>", color, Convert.ToString(Math.Round(LSAval, 2))); // Convert.ToString(Math.Round(LSAval, 4));
                                LS_Status_Hold[LS_Status_Hold_Count, DAYS_REM] = String.Format("<color=#{0}>{1}</color>", color, Convert.ToString(Math.Round(days_rem, 2))); // Convert.ToString(Math.Round(days_rem, 2));
                            }

                            double slurryRate = 0;
                            double sludgeRate = 0;
                            Log.Info("vessel: " + vessel.name);
                            if (vessel.loaded)
                            {
                                for (int i = 0; i < vessel.parts.Count; i++)
                                {
                                    Log.Info("part: " + vessel.parts[i].partInfo.title);
                                    for (int m = 0; m < vessel.parts[i].Modules.Count; m++)
                                    {
                                        PartModule tmpPM = vessel.parts[i].Modules[m];

                                        if (vessel.parts[i].Modules[m].moduleName == "ModuleResourceConverter")
                                        {
                                            ModuleResourceConverter m1 = (ModuleResourceConverter)tmpPM;
                                            foreach (ResourceRatio inp in m1.inputList)
                                            {
                                                if (inp.ResourceName == Constants.SLURRY)
                                                    slurryRate += inp.Ratio;
                                                if (inp.ResourceName == Constants.SLUDGE)
                                                    sludgeRate += inp.Ratio;

                                            }
                                        }
                                        if (vessel.parts[i].Modules[m].moduleName == "AnimatedGenerator")
                                        {
                                            AnimatedGenerator m1 = (AnimatedGenerator)tmpPM;

                                            foreach (ResourceRatio inp in m1.inputList)
                                            {
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

                                foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                                {
                                    Part part = p.partPrefab;

                                    Log.Info("part: " + part.partInfo.title);
                                    for (int m = 0; m < part.Modules.Count; m++)
                                    {
                                        PartModule tmpPM = part.Modules[m];

                                        if (part.Modules[m].moduleName == "ModuleResourceConverter")
                                        {
                                            ModuleResourceConverter m1 = (ModuleResourceConverter)tmpPM;
                                            foreach (ResourceRatio inp in m1.inputList)
                                            {
                                                if (inp.ResourceName == Constants.SLURRY)
                                                    slurryRate += inp.Ratio;
                                                if (inp.ResourceName == Constants.SLUDGE)
                                                    sludgeRate += inp.Ratio;

                                            }
                                        }
                                        if (part.Modules[m].moduleName == "AnimatedGenerator")
                                        {
                                            AnimatedGenerator m1 = (AnimatedGenerator)tmpPM;

                                            foreach (ResourceRatio inp in m1.inputList)
                                            {
                                                if (inp.ResourceName == Constants.SLURRY)
                                                    slurryRate += inp.Ratio;
                                                if (inp.ResourceName == Constants.SLUDGE)
                                                    sludgeRate += inp.Ratio;

                                            }
                                        }

                                    }
                                }
                            }

                            // doesn't hurt to do the secsPerDay calc every time, since this only runs once every 3 seconds
                            double secsPerDay = 3600 * (GameSettings.KERBIN_TIME ? 6 : 24);

                            // Need to scan for all greenhouses here
                            //LS_Status_Hold[LS_Status_Hold_Count, 5] = Convert.ToString(Math.Round(SlurryAvail, 5));
                            LS_Status_Hold[LS_Status_Hold_Count, SLURRYAVAIL] = SlurryAvail.ToString("N2");

                            // Need to scan for all sludge convertors here
                            //LS_Status_Hold[LS_Status_Hold_Count, 7] = Convert.ToString(Math.Round(SludgeAvail, 5));
                            LS_Status_Hold[LS_Status_Hold_Count, SLUDGEAVAIL] = SludgeAvail.ToString("N2");


                            Log.Info("Slurry: " + SlurryAvail + ",    rate: " + (secsPerDay * slurryRate));
                            LS_Status_Hold[LS_Status_Hold_Count, SLURRY_DAYS_TO_PROCESS] = (SlurryAvail / (secsPerDay * slurryRate)).ToString("N1");
                            LS_Status_Hold[LS_Status_Hold_Count, SLUDGE_DAYS_TO_PROCESS] = (SludgeAvail / (secsPerDay * sludgeRate)).ToString("N1");

                            LS_Status_Hold[LS_Status_Hold_Count, SLURRYCONVRATE] = (secsPerDay * slurryRate).ToString("N1");
                            LS_Status_Hold[LS_Status_Hold_Count, SLUDGECONVRATE] =  (secsPerDay * sludgeRate).ToString("N1");


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

            smallScrollBar = new GUIStyle(GUI.skin.verticalScrollbar);
            //smallScrollBar.fixedWidth = 8f;

            hSmallScrollBar = new GUIStyle(GUI.skin.horizontalScrollbar);
            hSmallScrollBar.fixedHeight = 0f;
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
                foreach (ProtoPartSnapshot p in IV.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
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
                foreach (var p in IV.Parts)
                {
                    Log.Info("Part: " + p.partInfo.name + ", crewCount: " + p.protoModuleCrew.Count());
                }
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
                        foreach (var p in IV.Parts)
                        {
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
                foreach (ProtoPartSnapshot p in IV.protoVessel.protoPartSnapshots)
                {
                    PartCountForShip++;
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
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
                    foreach (Part p in IV.parts)
                    {

                        int IFIcrew = p.protoModuleCrew.Count;
                        if (IFIcrew > 0)
                        {
                            CrewTest(0, p, l);
                        }

                    }
                }
                else
                {
                    foreach (ProtoPartSnapshot p in IV.protoVessel.protoPartSnapshots)
                    {

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

            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level > IFILS1.LifeSupportLevel.classic && HighLogic.LoadedScene == GameScenes.FLIGHT && lastVesselChecked != FlightGlobals.ActiveVessel)
            {
                lastVesselChecked = FlightGlobals.ActiveVessel;

                foreach (var p in FlightGlobals.ActiveVessel.Parts)
                {
                    foreach (var r in p.Resources)
                    {
                        if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced && r.resourceName == "Sludge")
                            r.isVisible = true;

                        if (r.resourceName == "OrganicSlurry")
                            r.isVisible = true;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (LifeSupportDisplay.LSDisplayActive && !HighLogic.LoadedSceneIsEditor && HighLogic.LoadedSceneIsGame)
            {
                GUI.skin = HighLogic.Skin;
                string TITLE = "IFI Vessel Life Support Status Display ";

                LifeSupportDisplay.infoWindowPos = GUILayout.Window(99988, LifeSupportDisplay.infoWindowPos, LSInfoWindow, TITLE, LifeSupportDisplay.layoutOptions);
            }
        }

        void GetWidth(string s, int i, bool init = false)
        {
            float minWidth, maxWidth;
            
            GUI.skin.label.CalcMinMaxWidth(new GUIContent(s), out minWidth, out maxWidth);
            maxWidth += 10;
            if (init)
                LS_Status_Width[i] = (int)maxWidth;
            else
                LS_Status_Width[i] = Math.Max(LS_Status_Width[i], (int)maxWidth);
        }

        private void LSInfoWindow(int windowId)
        {
            GetWidth("Vessel", VESSEL, true);
            GetWidth("Location", LOCATION, true);
            GetWidth("Crew", CREW, true);
            GetWidth("   Life\nSupport", LSAVAIL, true);
            GetWidth("     Days\nRemaining", DAYS_REM, true);
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
            {
                GetWidth("Slurry (rate)", SLURRYAVAIL, true);
                GetWidth("Process\n Time", SLURRY_DAYS_TO_PROCESS, true);
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                {
                    GetWidth("Sludge (rate)", SLUDGEAVAIL, true);
                    GetWidth("Process\n Time", SLUDGE_DAYS_TO_PROCESS, true);
                }
            }
            for (int LLC = 0; LLC < LS_Status_Hold_Count; LLC++)
            {
                GetWidth(LS_Status_Hold[LLC, VESSEL], VESSEL);
                GetWidth(LS_Status_Hold[LLC, LOCATION], LOCATION);
                GetWidth("  " + LS_Status_Hold[LLC, CREW], CREW);
                GetWidth("  " + LS_Status_Hold[LLC, LSAVAIL], LSAVAIL);
                GetWidth("  " + LS_Status_Hold[LLC, DAYS_REM], DAYS_REM);
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                {
                    GetWidth(LS_Status_Hold[LLC, SLURRYAVAIL] + "(" + LS_Status_Hold[LLC, SLURRYCONVRATE] + ")", SLURRYAVAIL);
                    GetWidth(LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] + "d", SLURRY_DAYS_TO_PROCESS);
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                    {
                        GetWidth(LS_Status_Hold[LLC, SLUDGEAVAIL] + "(" + LS_Status_Hold[LLC, SLUDGECONVRATE] + ")"
                            , SLUDGEAVAIL);
                        GetWidth(LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] + "d", SLUDGE_DAYS_TO_PROCESS);
                    }
                }
            }

            GUILayout.BeginHorizontal(); // used to be 400
            GUILayout.Label("Vessel", GUILayout.Width(LS_Status_Width[VESSEL]));
            GUILayout.Label("Location", GUILayout.Width(LS_Status_Width[LOCATION]));
            GUILayout.Label("Crew", GUILayout.Width(LS_Status_Width[CREW]));
            GUILayout.Label("   Life\nSupport", GUILayout.Width(LS_Status_Width[LSAVAIL]));
            GUILayout.Label("     Days\nRemaining", GUILayout.Width(LS_Status_Width[DAYS_REM]));

            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
            {
                GUILayout.Label("Slurry (rate)", GUILayout.Width(LS_Status_Width[SLURRYAVAIL]));
                GUILayout.Label("Process\n Time", GUILayout.Width(LS_Status_Width[SLURRY_DAYS_TO_PROCESS]));
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                {
                    GUILayout.Label("Sludge (rate)", GUILayout.Width(LS_Status_Width[SLUDGEAVAIL]));
                    GUILayout.Label("Process\n Time", GUILayout.Width(LS_Status_Width[SLUDGE_DAYS_TO_PROCESS]));
                }
            }
            // Following needed to keep horizontal scroll bar from showing up
            GUILayout.Label(" ", GUILayout.Width(15));
            GUILayout.EndHorizontal();

            //var rightJustify = new GUIStyle(GUI.skin.label);
            //rightJustify.alignment = TextAnchor.MiddleRight;


            LifeSupportDisplay.infoScrollPos = GUILayout.BeginScrollView(LifeSupportDisplay.infoScrollPos, false, false, hSmallScrollBar, smallScrollBar, GUILayout.Height(345));
            if (LS_Status_Hold_Count > 0)
            {
                int LLC = 0;
                
                while (LLC < LS_Status_Hold_Count)
                {
                    GUILayout.BeginHorizontal(); // used to be 400
                    GUILayout.Label(LS_Status_Hold[LLC, VESSEL], GUILayout.Width(LS_Status_Width[VESSEL]));
                    GUILayout.Label(LS_Status_Hold[LLC, LOCATION], GUILayout.Width(LS_Status_Width[LOCATION]));
                    GUILayout.Label("  " + LS_Status_Hold[LLC, CREW], GUILayout.Width(LS_Status_Width[CREW]));
                    GUILayout.Label("  " + LS_Status_Hold[LLC, LSAVAIL], GUILayout.Width(LS_Status_Width[LSAVAIL]));
                    GUILayout.Label("  " + LS_Status_Hold[LLC, DAYS_REM], GUILayout.Width(LS_Status_Width[DAYS_REM]));

                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                    {
                        GUILayout.Label(LS_Status_Hold[LLC, SLURRYAVAIL] + "(" + LS_Status_Hold[LLC, SLURRYCONVRATE] + ")", GUILayout.Width(LS_Status_Width[SLURRYAVAIL]));

                        GUILayout.Label(LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] + "d",  GUILayout.Width(LS_Status_Width[SLURRY_DAYS_TO_PROCESS]));

                        if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                        {
                            GUILayout.Label(LS_Status_Hold[LLC, SLUDGEAVAIL] + "(" + LS_Status_Hold[LLC, SLUDGECONVRATE] + ")"
                                , GUILayout.Width(LS_Status_Width[SLUDGEAVAIL]));
                            GUILayout.Label(LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] + "d", GUILayout.Width(LS_Status_Width[SLUDGE_DAYS_TO_PROCESS]));
                        }
                    }

                    LLC++;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            LifeSupportDisplay.WarpCancel = GUI.Toggle(new Rect(10, 416, 400, 20), LifeSupportDisplay.WarpCancel, "Auto Cancel Warp on Low Life Support");

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