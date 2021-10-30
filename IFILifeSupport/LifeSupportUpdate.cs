using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;
using static IFILifeSupport.RegisterToolbar;


namespace IFILifeSupport
{
    // IFILS_Main.lsTracking
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    internal class IFILS_Main : MonoBehaviour
    {
        internal static LifeSupportUpdate lsu = null;
        internal static IFI_LIFESUPPORT_TRACKING lsTracking = null;
        public void Awake()
        {
            Log.Info("IFILS_Main.Awake");
            if (lsTracking == null)
                lsTracking = gameObject.AddComponent<IFI_LIFESUPPORT_TRACKING>();
            if (lsu == null)
                lsu = gameObject.AddComponent<LifeSupportUpdate>();
        }
        public void FixedUpdate()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && lsu != null)
            {
                Log.Info("IFILS_Main, Destorying lsu");
                if (lsu != null) Destroy(lsu);
                if (lsTracking != null) Destroy(lsTracking);
                lsu = null;
                lsTracking = null;
            }
        }
    }

//    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    class LifeSupportUpdate : MonoBehaviour
    {
        private double lastUpdateTime;

        int LS_ALERT_LEVEL = 1;

        public static Dictionary<string, StarvingKerbal> starvingKerbals = new Dictionary<string, StarvingKerbal>();

        public void Awake()
        {
            // IFITimer = Convert.ToInt32(Planetarium.GetUniversalTime());
            lastUpdateTime = Planetarium.GetUniversalTime();
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            Log.Info("LifeSupportUpdate.Start");
            CancelInvoke();
            lastUpdateTime = Planetarium.GetUniversalTime();
            InvokeRepeating("IFIFixedUpdate", 0, 2); // maybe change to 10 later
        }

        void OnDestroy()
        {
            Log.Info("LifeSupportUpdate.OnDestroy");
        }
        void IFIFixedUpdate()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                //Destroy(this);
                return;
            }
            Log.Info("display_active, TimeWarp.rate: " + TimeWarp.CurrentRate +
                ", RefreshInterval: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().RefreshInterval);
            if (!HighLogic.LoadedSceneIsEditor &&
                (TimeWarp.CurrentRate > 800 ||
                Planetarium.GetUniversalTime() - lastUpdateTime >= HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().RefreshInterval))
            {
                Life_Support_Update();
                lastUpdateTime = Planetarium.GetUniversalTime();
            }
        }

        public void Life_Support_Update()
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedSceneIsEditor || !HighLogic.LoadedSceneIsGame)
                return; //Don't do anything while the game is loading or in editor
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {

                double Elapsed_Time = IFI_Get_Elasped_Time();
                Log.Info("Life_Support_Update.Elapsed_Time: " + Elapsed_Time + ", FlightGlobals.Vessels.Count: " + FlightGlobals.Vessels.Count);

                InitDisplayData();
                IFILS_Main.lsTracking.LS_Status_Hold_Count = 0;

                for (int idx = 0; idx < FlightGlobals.Vessels.Count; idx++)
                {
                    Vessel vessel = FlightGlobals.Vessels[idx];
                    Log.Info("Vessel # " + idx + ", type: " + vessel.vesselType);
                    if (vessel && (
                        vessel.vesselType == VesselType.Ship || vessel.vesselType == VesselType.Lander ||
                        vessel.vesselType == VesselType.Station || vessel.vesselType == VesselType.Rover ||
                        vessel.vesselType == VesselType.Base || vessel.vesselType == VesselType.Probe) ||
                        vessel.vesselType == VesselType.EVA)
                    {
                        int IFI_Crew = 0;
                        double slurryRate = 0;
                        double sludgeRate = 0;
                        string IFI_Location = "";
                        double IFI_ALT = 0.0;
                        double LS_Use = 0;
                        double LSAval = 0;
                        double days_rem = 0;

                        Log.Info("Life_Support_Update.vessel: " + vessel.name);

                        GetCrewInfo(vessel, out IFI_Crew, out IFI_ALT, out IFI_Location);

                        //ProcessCrew();
                        if (IFI_Crew > 0.0)
                        {
                            LS_Use = CalcLS(vessel, IFI_Crew, Elapsed_Time, out LSAval, out days_rem);
                            Log.Info("Life_Support_Update, LS_Use: " + LS_Use);
                            if (LS_Use > 0.0)
                            {
                                double before = IFIGetAllResources(Constants.LIFESUPPORT, vessel);
                                double usedLS = IFIUSELifeSupport(vessel, LS_Use, IFI_Crew);
                                double after = IFIGetAllResources(Constants.LIFESUPPORT, vessel);
                                Log.Info("Life_Support_Update, before: " + before + ", usedLS: " + usedLS + ", after: " + after);


                                double slurrySaved = IFIUSEResources(Constants.SLURRY, vessel, -usedLS, IFI_Crew);
                                Log.Info("Life_Support_Update, usedLS: " + usedLS + ",   slurrySaved: " + slurrySaved);
                                //if (!vessel.loaded)
                                {
                                    double slurryRecovered = IFIUSEResources(Constants.SLURRY, vessel, -(LS_Use - usedLS), IFI_Crew);
                                    Log.Info("vessel.loaded: " + vessel.loaded);
                                    Log.Info("LS_Use: " + LS_Use + ", leftoverLS: " + usedLS + ",  slurryRecovered: " + slurryRecovered);
                                    Log.Info("Elapsed_Time: " + Elapsed_Time);
                                }
                                if (vessel.vesselType == VesselType.EVA && KerbalEVARescueDetect)
                                {
                                    LSAval = 200.0;
                                    days_rem = 10.0;
                                }
                            }
                            double after2 = IFIGetAllResources(Constants.LIFESUPPORT, vessel);
                            Log.Info("Life_Support_Update, after 2: " + after2);
                        }

                        if (vessel.loaded)
                        {
                            ProcessLoadedVessel(vessel, ref slurryRate, ref sludgeRate);
                        }
                        else
                        {
                            ProcessUnloadedVessel(vessel.protoVessel, ref slurryRate, ref sludgeRate);
                        }

                        Log.Info("Total slurryRate; " + slurryRate);
                        ProcessSludgeAndSlurry(vessel, Elapsed_Time, IFI_Crew, slurryRate, sludgeRate);
                        double after3 = IFIGetAllResources(Constants.LIFESUPPORT, vessel);

                        DoDisplayCalcs(vessel, IFI_Crew, IFI_ALT, IFI_Location, Elapsed_Time, days_rem, LSAval, slurryRate, sludgeRate);
                        Log.Info("after 4: " + after3);

                    }
                }
            }
        }


        private double IFIGetAllResources(string IFIResource, Vessel vessel)
        {
            double IFIResourceAmt = 0.0;
            double num2 = 0;
            int id = PartResourceLibrary.Instance.GetDefinition(IFIResource).id;
            if (vessel.loaded)
            {
                if (vessel == null)
                {
                    Log.Info("IV is null");
                    return 0;
                }
                if (vessel.rootPart == null)
                {
                    Log.Info("rootPart is null");
                    return 0;
                }
                vessel.GetConnectedResourceTotals(id, out IFIResourceAmt, out num2, true);
                //IV.rootPart.GetConnectedResourceTotals(id, out IFIResourceAmt, out num2, true);
                Log.Info("IFIGetAllResources, IFIResource: " + IFIResource + ",   IFIResourceAmt: " + IFIResourceAmt);
            }
            else
            {
                for (int idx3 = 0; idx3 < vessel.protoVessel.protoPartSnapshots.Count; idx3++)
                {
                    ProtoPartSnapshot p = vessel.protoVessel.protoPartSnapshots[idx3];

                    for (int idx4 = 0; idx4 < p.resources.Count; idx4++)
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

        bool ResourceConverterActive(ProtoPartSnapshot p, Part part, int m)
        {
            int j = m;
            if (j >= part.Modules.Count || part.Modules[j].moduleName != p.modules[m].moduleName)
            {
                if (j < part.Modules.Count)
                {
                    Log.Warning("Expected " + p.modules[m].moduleName + " at index " + m + ", got " + part.Modules[j].moduleName);

                    for (j = m; j < part.Modules.Count; ++j)
                    {
                        if (part.Modules[j].moduleName == p.modules[m].moduleName)
                        {
                            Log.Warning("Found " + p.modules[m].moduleName + " at index " + j);
                            break;
                        }
                    }
                }
            }
            if (j < part.Modules.Count)
            {
                bool active = false;
                Boolean.TryParse(p.modules[j].moduleValues.GetValue("IsActivated"), out active);
                return active;
            }
            return false;
        }


        double CalcLS(Vessel vessel, int IFI_Crew, double Elapsed_Time, out double LSAval, out double days_rem)
        {
            Log.Info("CalcLS");
            double LS_Use = LifeSupportRate.GetRate();

            LSAval = IFIGetAllResources(Constants.LIFESUPPORT, vessel);

            Log.Info("Initial IFI_Crew: " + IFI_Crew + ", Elapsed_Time: " + Elapsed_Time + ", LS_Use: " + LS_Use + ",  LSAval: " + LSAval);

            if (LifeSupportRate.IntakeAirAvailable(vessel))
            {
                Log.Info("IntakeAirAvailable");
                if (vessel.mainBody.name == FlightGlobals.GetHomeBodyName())
                    LS_Use *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment; //  0.20;
                else
                    LS_Use *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment; // 0.60;
            }

            days_rem = LSAval / IFI_Crew / (LS_Use * 60 * (GameSettings.KERBIN_TIME ? 6 : 24));
            Log.Info("After Atmo adjust, LS_Use: " + LS_Use + ",   IFI_Crew: " + IFI_Crew + ",   Elapsed_Time: " + Elapsed_Time + ", LSAval: " + LSAval); // + ", HoursPerDay: " + HoursPerDay);
            LS_Use *= IFI_Crew;
            LS_Use *= Elapsed_Time / 60;
            // IF No EC use more LS resources
            if (IFIGetAllResources("ElectricCharge", vessel) < 0.1)
            {
                LS_Use *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment;
            }
            Log.Info("After EC check, total LS_Use: " + LS_Use);
            return LS_Use;
        }

        void ProcessLoadedVessel(Vessel vessel, ref double slurryRate, ref double sludgeRate)
        {
            Log.Info("ProcessLoadedVessel");
            for (int idx2 = 0; idx2 < vessel.parts.Count; idx2++)
            {
                Log.Info("part: " + vessel.parts[idx2].partInfo.title);
                for (int m = 0; m < vessel.parts[idx2].Modules.Count; m++)
                {
                    PartModule tmpPM = vessel.parts[idx2].Modules[m];

                    if (vessel.parts[idx2].Modules[m].moduleName == "ModuleIFILifeSupport")
                    {
                        ModuleIFILifeSupport m1 = (ModuleIFILifeSupport)tmpPM;
                        Log.Info("Vessel part: " + vessel.parts[idx2].partInfo.title + ",  ModuleIFILifeSupport IsActivated: " + m1.IsActivated);
                        if (m1.IsActivated)
                        {
                            for (int i = 0; i < m1.inputList.Count; i++)
                            //foreach (ResourceRatio inp in m1.inputList)
                            {
                                ResourceRatio inp = m1.inputList[i];
                                if (inp.ResourceName == Constants.SLURRY)
                                    slurryRate += inp.Ratio;
                                if (inp.ResourceName == Constants.SLUDGE)
                                    sludgeRate += inp.Ratio;

                                Log.Info("m1.name: " + m1.name + ",  slurryRate: " + slurryRate + ",  sludgeRate: " + sludgeRate);
                            }
                        }
                    }
#if false
                    if (vessel.parts[idx2].Modules[m].moduleName == "AnimatedGenerator")
                    {
                        AnimatedGenerator m1 = (AnimatedGenerator)tmpPM;
                        if (m1.IsActivated)
                        {
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
#endif
                }
            }
        }

        void ProcessUnloadedVessel(ProtoVessel protoVessel, ref double slurryRate, ref double sludgeRate)
        {
            Log.Info("Processing unloaded vessel");

            for (int idx3 = 0; idx3 < protoVessel.protoPartSnapshots.Count; idx3++)
            {
                ProtoPartSnapshot p = protoVessel.protoPartSnapshots[idx3];
                Part part = p.partPrefab;

                Log.Info("part: " + part.partInfo.title);
                for (int m = 0; m < part.Modules.Count; m++)
                {
                    PartModule tmpPM = part.Modules[m];

                    if (part.Modules[m].moduleName == "ModuleIFILifeSupport")
                    {
                        ModuleIFILifeSupport m1 = (ModuleIFILifeSupport)tmpPM;

                        if (ResourceConverterActive(p, part, m))
                        {
                            for (int i = 0; i < m1.inputList.Count; i++)
                            {
                                ResourceRatio inp = m1.inputList[i];
                                if (inp.ResourceName == Constants.SLURRY)
                                    slurryRate += inp.Ratio;
                                if (inp.ResourceName == Constants.SLURRY)
                                    sludgeRate += inp.Ratio;

                            }
                        }

                    }
#if false
                    if (part.Modules[m].moduleName == "AnimatedGenerator")
                    {
                        AnimatedGenerator m1 = (AnimatedGenerator)tmpPM;
                        if (ResourceConverterActive(p, part, m))
                        {
                            for (int i = 0; i < m1.inputList.Count; i++)
                            //foreach (ResourceRatio inp in m1.inputList)
                            {
                                ResourceRatio inp = m1.inputList[i];
                                if (inp.ResourceName == Constants.SLURRY)
                                    slurryRate += inp.Ratio;
                                if (inp.ResourceName == Constants.SLURRY)
                                    sludgeRate += inp.Ratio;

                            }
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
                    StarvingKerbal.CrewTestEVA(IV, l);
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
                            StarvingKerbal.CrewTest(0, p, l);
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
                            StarvingKerbal.CrewTestProto(0, p, l);
                        }

                    }
                }
            }
        }

        void ProcessSludgeAndSlurry(Vessel vessel, double Elapsed_Time, int IFI_Crew, double slurryRate, double sludgeRate)
        {
            slurryRate *= IFI_Resources.BASE_MULTIPLIER;
            sludgeRate *= IFI_Resources.BASE_MULTIPLIER;

            Log.Info("ProcessSludgeAndSlurry, Elapsed_Time: " + Elapsed_Time);
            double proposedSlurryProcessed = slurryRate * Elapsed_Time;
            double proposedSludgeProcessed = sludgeRate * Elapsed_Time;
            Log.Info("slurryRate: " + slurryRate + ", slurryProcessed: " + proposedSlurryProcessed);
            Log.Info("sludgeRate: " + sludgeRate + ", sludgeProcessed: " + proposedSludgeProcessed);


            double beforeSlurry = IFIGetAllResources(Constants.SLURRY, vessel);

            double slurryProcessed = IFIUSEResources(Constants.SLURRY, vessel, proposedSlurryProcessed, IFI_Crew);

            double afterSlurry = IFIGetAllResources(Constants.SLURRY, vessel);
            // Log.Info("beforeSlurry: " + beforeSlurry + ",  slurryProcessed: " + slurryProcessed + ", afterSlurry: " + afterSlurry);



            //double sludgeUnprocessed = IFIUSEResources(Constants.SLURRY, vessel, vessel.loaded, sludgeProcessed, IFI_Crew);
            double sludgeUnprocessed = 0;
            //double sludgeProcessed = IFIUSEResources(Constants.SLURRY, vessel, vessel.loaded, slurryProcessed , IFI_Crew);

            //Log.Info("line 382: slurryProcessed: " + proposedSlurryProcessed +
            //    ",    proposedSlurryProcessed-slurryUnprocessed: " + (proposedSlurryProcessed - slurryProcessed));

            double lsRecovered = sludgeUnprocessed + slurryProcessed;
            double before = IFIGetAllResources(Constants.LIFESUPPORT, vessel);
            double lsLost = IFIUSELifeSupport(vessel, -lsRecovered, IFI_Crew);
            double after = IFIGetAllResources(Constants.LIFESUPPORT, vessel);

            //Log.Info("sludgeUnprocessed: " + sludgeUnprocessed + ", slurryProcessed: " + slurryProcessed +
            //    ", lsRecovered: " + lsRecovered + ", lsLost: " + lsLost);
        }

        private bool KerbalEVARescueDetect = false;


        private double IFIUSELifeSupport(Vessel vessel, double UR_Amount, int CREWHOLD)
        {
            UR_Amount *= IFI_Resources.BASE_MULTIPLIER;
            double Temp_Resource = UR_Amount;
            string IFIResource = Constants.LIFESUPPORT;

            bool giveBack = (UR_Amount < 0);

            //Log.Info("IFIUSEResources 1, resource: " + IFIResource + ", ISLoaded: " + vessel.loaded + "   UR_Amount: " + UR_Amount);
            if (vessel.loaded)
            {

                double ALL_Resources = IFIGetAllResources(IFIResource, vessel);
                Log.Info("IFIUSEResources: Vessel: " + vessel.vesselName + ",  crewCount: " + vessel.rootPart.protoModuleCrew.Count() + ",  IFIResource: " + IFIResource + ",  ALL_Resources: " + ALL_Resources + ", UR_Amount: " + UR_Amount);

                if (ALL_Resources == 0.0 && !giveBack)
                {
                    IFI_Check_Kerbals(vessel, UR_Amount);
                    return 0.0;
                }
                else
                {
                    //Log.Info("Vessel: " + vessel.vesselName + ",  crewCount: " + vessel.rootPart.protoModuleCrew.Count());

                    List<string> toDelete = new List<string>();

                    // check to see if this kerbal is on the vessel
                    for (int idx = 0; idx < vessel.Parts.Count; idx++)
                    {
                        var p = vessel.Parts[idx];
                        for (int i1 = p.protoModuleCrew.Count(); i1 > 0; i1--)
                        {
                            StarvingKerbal sk;
                            if (starvingKerbals.TryGetValue(p.protoModuleCrew[i1 - 1].name, out sk))
                            {
                                Log.Info("Vessel: " + vessel.vesselName + ",   protoModuleCrew: " + p.protoModuleCrew[i1 - 1].name);

                                toDelete.Add(sk.name);
                                p.protoModuleCrew[i1 - 1].type = ProtoCrewMember.KerbalType.Crew;
                                KerbalRoster.SetExperienceTrait(p.protoModuleCrew[i1 - 1], sk.trait);

                                IFIDebug.IFIMess(vessel.vesselName + " Kerbal returned to crew status due to LS - " + sk.name + ", trait restored to: " + sk.trait);
                                string message = ""; message += vessel.vesselName + "\n\n"; message += sk.name + "\n Was returned to duty due to ::";
                                message += "Life Support Restored";
                                message += "::";
                                MessageSystem.Message m = new MessageSystem.Message("Kerbal returned to duty from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                                MessageSystem.Instance.AddMessage(m);

                            }
                        }
                    }
                    foreach (var s in toDelete)
                        starvingKerbals.Remove(s);
                    toDelete.Clear();

                }
                if (ALL_Resources >= UR_Amount || giveBack)
                {
                    Log.Info("RequestResource: " + IFIResource + ",  requesting UR_Amount: " + UR_Amount);

                    double amount = Util.GetResourceTotal(vessel, IFIResource);
                    Temp_Resource = vessel.rootPart.RequestResource(IFIResource, UR_Amount, ResourceFlowMode.ALL_VESSEL);
                    double amount2 = Util.GetResourceTotal(vessel, IFIResource);
                    Log.Info("Before request amount: " + amount + ",  After request: amount2: " + amount2);
                }
                else
                {
                    Log.Info("RequestResource (not enough for full request): " + IFIResource + ",  UR_Amount: " + UR_Amount);
                    Temp_Resource = vessel.rootPart.RequestResource(IFIResource, UR_Amount, ResourceFlowMode.ALL_VESSEL);
                }
                Log.Info("Temp_Resource (result from RequestResource): " + Temp_Resource);
            }
            else
            {
                KerbalEVARescueDetect = false;
                int PartCountForShip = 0;
                for (int idx = 0; idx < vessel.protoVessel.protoPartSnapshots.Count; idx++)
                {
                    ProtoPartSnapshot p = vessel.protoVessel.protoPartSnapshots[idx];
                    PartCountForShip++;
                    for (int idx2 = 0; idx2 < p.resources.Count; idx2++)
                    {
                        ProtoPartResourceSnapshot r = p.resources[idx2];
                        Log.Info("Resource in part: " + p.partInfo.title + ", : " + r.resourceName);
                        if (r.resourceName == IFIResource)
                        {
                            Log.Info("Resource found");

                            if ((UR_Amount <= 0.0 && !giveBack) ||
                                (UR_Amount >= 0 && giveBack))
                                break;
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
                            var r1 = p.resources[idx2];
                            Log.Info("IFIUSEResources 2, Resource: " + IFIResource + ",  r1.amount: " + r1.amount + ",   r.amount: " + r.amount + ",   UR_Amount: " + UR_Amount + ",  IHold: " + IHold);

                            Temp_Resource = UR_Amount;
                        }
                    }
                    //
                    if ((UR_Amount <= 0.0 && !giveBack) ||
                                 (UR_Amount >= 0 && giveBack))
                        break;
                }


                //if (IV.isEVA && )
                //{
                //    IFIDebug.IFIMess("#### IFI LIfeSupport #### Rescue Kerbal Found with No LS Resource TAG Flagging");
                //    UR_Amount = 0.0;
                //}
            }
            return UR_Amount / IFI_Resources.BASE_MULTIPLIER;
        }

        private double IFIUSEResources(string IFIResource, Vessel IV, double UR_Amount, int CREWHOLD)
        {
            UR_Amount *= IFI_Resources.BASE_MULTIPLIER;
            double Temp_Resource = UR_Amount;
            //IFIResource = IFIResource.ToLower();

            Log.Info("IFIUSEResources 1, resource: " + IFIResource + ", IV.loaded: " + IV.loaded + "   UR_Amount: " + UR_Amount);
            if (IV.loaded)
            {


                Log.Info("IFIUSEResources: Vessel: " + IV.vesselName + ",  crewCount: " + IV.rootPart.protoModuleCrew.Count() + ",  IFIResource: " + IFIResource);

#if false
                foreach (var p in IV.Parts)
                {
                    Log.Info("Part: " + p.partInfo.name + ", crewCount: " + p.protoModuleCrew.Count());
                }
#endif
                if (UR_Amount > 0)
                {
                    double ALL_Resources = IFIGetAllResources(IFIResource, IV);
                    if (ALL_Resources < UR_Amount)
                    {
                        //double TEST_Mod = (UR_Amount - ALL_Resources) * 100000;
                        Log.Info("RequestResource: " + IFIResource + ",  requesting ALL_Resources: " + ALL_Resources);

                        double amount = Util.GetResourceTotal(IV, IFIResource);
                        Temp_Resource = IV.rootPart.RequestResource(IFIResource, ALL_Resources, ResourceFlowMode.ALL_VESSEL);
                        double amount2 = Util.GetResourceTotal(IV, IFIResource);

                        Log.Info("Before request amount: " + amount + ",  After request: amount2: " + amount2);
                    }
                    else
                    {
                        Log.Info("RequestResource (not enough for full request): " + IFIResource + ",  UR_Amount: " + UR_Amount);
                        Temp_Resource = IV.rootPart.RequestResource(IFIResource, UR_Amount);
                    }
                    Log.Info("Temp_Resource (result from RequestResource): " + Temp_Resource);
                }
                else
                {
                    Temp_Resource = IV.rootPart.RequestResource(IFIResource, UR_Amount, ResourceFlowMode.ALL_VESSEL);
                    Log.Info("UR_Amount is negative: " + UR_Amount + ",   Temp_Resource (result from RequestResource): " + Temp_Resource);
                }
            }
            else
            {
                bool giveBack = (UR_Amount < 0);

                KerbalEVARescueDetect = false;
                int PartCountForShip = 0;
                for (int idx = 0; idx < IV.protoVessel.protoPartSnapshots.Count; idx++)
                {
                    ProtoPartSnapshot p = IV.protoVessel.protoPartSnapshots[idx];
                    PartCountForShip++;
                    for (int idx2 = 0; idx2 < p.resources.Count; idx2++)
                    {
                        ProtoPartResourceSnapshot r = p.resources[idx2];
                        Log.Info("Resource in part: " + p.partInfo.title + ", : " + r.resourceName);
                        if (r.resourceName == IFIResource)
                        {
                            Log.Info("Resource found");
                            //if (UR_Amount <= 0.0)
                            if ((UR_Amount <= 0.0 && !giveBack) ||
                                (UR_Amount >= 0 && giveBack))
                                break;
                            double IHold = r.amount;


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
                            var r1 = p.resources[idx2];
                            Log.Info("IFIUSEResources 2, Resource: " + IFIResource + ",  r1.amount: " + r1.amount + ",   r.amount: " + r.amount + ",   UR_Amount: " + UR_Amount + ",  IHold: " + IHold);

                            Temp_Resource = UR_Amount;
                        }
                    }
                    //
                    if ((UR_Amount <= 0.0 && !giveBack) ||
                                 (UR_Amount >= 0 && giveBack))
                        break;
                }


                //if (IV.isEVA && )
                //{
                //    IFIDebug.IFIMess("#### IFI LIfeSupport #### Rescue Kerbal Found with No LS Resource TAG Flagging");
                //    UR_Amount = 0.0;
                //}
            }
            return UR_Amount / IFI_Resources.BASE_MULTIPLIER;
        }


        private double IFI_Get_Elasped_Time()
        {
            double CurrTime = Planetarium.GetUniversalTime();
            if (Time.timeSinceLevelLoad < 3f || (!FlightGlobals.ready && HighLogic.LoadedSceneIsFlight))
            {
                lastUpdateTime = CurrTime;
                Log.Info("timeSinceLevelLoad: " + Time.timeSinceLevelLoad + ", FlightGlobals.ready: " + FlightGlobals.ready);
            }

            Log.Info("IFI_Get_Elapsed_Time, lastUpdateTime: " + lastUpdateTime + ", CurrTime: " + CurrTime);
            double IHOLD = CurrTime - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            return IHOLD;
        }

        void GetCrewInfo(Vessel vessel, out int IFI_Crew, out double IFI_ALT, out string IFI_Location)
        {
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
            Log.Info("GetCrewInfo, IFI_Crew: " + IFI_Crew + ", IFI_ALT: " + IFI_ALT + ", location: " + IFI_Location);
        }


        void InitDisplayData()
        {           
            IFI_LIFESUPPORT_TRACKING.toolbarControl.SetTexture("IFILS/Textures/IFI_LS_GRN_38", "IFILS/Textures/IFI_LS_GRN_24");
            IFILS_Main.lsTracking.LS_Status_Hold = new string[FlightGlobals.Vessels.Count(), IFI_LIFESUPPORT_TRACKING.MAX_STATUSES];
            IFILS_Main.lsTracking.ClearStatusWidths();
            LS_ALERT_LEVEL = 1;
        }

        void DoDisplayCalcs(Vessel vessel, int IFI_Crew, double IFI_ALT, string IFI_Location, double Elapsed_Time, double days_rem, double LSAval, double slurryRate, double sludgeRate)
        {

            //slurryRate *= IFI_Resources.BASE_SECS_PER_DAY;
            //sludgeRate *= IFI_Resources.BASE_SECS_PER_DAY;

            //LS_Status_Width = new int[MAX_STATUSES];
            //LS_Status_Spacing = new int[MAX_STATUSES];

//            IFILS_Main.lsTracking.LS_Status_Hold_Count = 0;

            Log.Info(" Found Vessel: " + vessel.vesselName);//float distance = (float)Vector3d.Distance(vessel.GetWorldPos3D(), FlightGlobals.ActiveVessel.GetWorldPos3D());

            IFI_Resources.UpdateDisplayValues(vessel);

            IFIDebug.IFIMess("IFI_LIFESUPPORT_TRACKING.Life_Support_Update, IFI_Location: " + IFI_Location +
                ",  FlightGlobals.GetHomeBodyName(): " + FlightGlobals.GetHomeBodyName() + ",   IFI_ALT: " + IFI_ALT);
            double SlurryAvail = IFIGetAllResources(Constants.SLURRY, vessel);
            double SludgeAvail = IFIGetAllResources(Constants.SLURRY, vessel);


            IFILS_Main.lsTracking.SetUpDisplayLines(vessel, IFI_Location, IFI_Crew, days_rem, LSAval, SlurryAvail, SludgeAvail, slurryRate, sludgeRate, ref IFILS_Main.lsTracking.LS_Status_Hold_Count);

            IFILS_Main.lsTracking.CheckAlertLevels(days_rem, ref LS_ALERT_LEVEL);
        }

    }
}
