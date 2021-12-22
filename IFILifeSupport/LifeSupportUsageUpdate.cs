using System;
using System.Collections;
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

    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    class LifeSupportUsageUpdate : MonoBehaviour
    {
        public static LifeSupportUsageUpdate Instance;

        private double lastUpdateTime;

        int LS_ALERT_LEVEL = 1;

        public static Dictionary<string, StarvingKerbal> starvingKerbals = new Dictionary<string, StarvingKerbal>();

        public void Awake()
        {
            Instance = this;
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        void Start()
        {
            Log.Info("LifeSupportUsageUpdate.Start, scene: " + HighLogic.LoadedScene);
            //CancelInvoke();
            lastUpdateTime = Planetarium.GetUniversalTime();
            //InvokeRepeating("IFILifeSupportFixedUpdate", 0, 2); // maybe change to 10 later
            StopAllCoroutines();
            StartCoroutine(IFILifeSupportFixedUpdate());
        }

        void OnDestroy()
        {
            Log.Info("LifeSupportUsageUpdate.OnDestroy");
            StopCoroutine(IFILifeSupportFixedUpdate());
        }

        IEnumerator IFILifeSupportFixedUpdate()
        {
            Log.Info("IFILifeSupportFixedUpdate started");
            bool refreshed = false;
            while (true)
            {
                if (!HighLogic.LoadedSceneIsGame || RegisterToolbar.GamePaused /*("IFILifeSupportFixedUpdate") */ )
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    continue;
                }
                if (!HighLogic.LoadedSceneIsEditor && HighLogic.LoadedSceneIsGame &&
                    (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER) &&
                    (!refreshed || TimeWarp.CurrentRate > 800 ||
                    Planetarium.GetUniversalTime() - lastUpdateTime >= HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().displayRefreshInterval))
                {
                    Life_Support_Update();
                    lastUpdateTime = Planetarium.GetUniversalTime();
                    refreshed = true;
                    yield return new WaitForSecondsRealtime(HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().displayRefreshInterval);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    refreshed = false;
                }
            }
        }

        public void Life_Support_Update()
        {
            double Elapsed_Time = IFI_Get_Elasped_Time();
            if (Elapsed_Time == 0)
                return;

            InitDisplayData();
            IFI_LifeSupportTrackingDisplay.Instance.LS_Status_Hold_Count = 0;

            //
            // Loop through all vessels
            //
            for (int idx = 0; idx < FlightGlobals.Vessels.Count; idx++)
            {
                Vessel vessel = FlightGlobals.Vessels[idx];
                if (vessel.vesselType != VesselType.SpaceObject)
                    Log.Info("Vessel # " + idx + ", type: " + vessel.vesselType);
                if (vessel && (vessel.vesselType >= VesselType.Probe && vessel.vesselType <= VesselType.EVA))
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

                   double LS_Needed =  LS_Use = CalcLS(vessel, IFI_Crew, Elapsed_Time, out LSAval, out days_rem);

                    if (IFI_Crew > 0.0)
                    {
                        //
                        // Figure out how much is needed for the Elapsed_Time
                        //

                        //if (LS_Use > 0.0)
                        {
                            //
                            // Special case for an EVA kerbal and a rescue
                            if (/* vessel.vesselType == VesselType.EVA && */ KerbalEVARescueDetect(vessel, IFI_Crew))
                            {
                                //LSAval = 200.0;
                                //days_rem = 10.0;
                            }

                            if (vessel.loaded)
                            {
                                double Temp_Resource;
                                double TotalLS_ResourcesAvail = IFIGetAllResources(Constants.LIFESUPPORT, vessel);
                                if (LS_Use > TotalLS_ResourcesAvail)
                                {
                                    Log.Info("RequestResource (not enough for full request): " + Constants.LIFESUPPORT + ",  requesting ALL_Resources: " + TotalLS_ResourcesAvail);

                                    double amount = Util.GetResourceTotal(vessel, Constants.LIFESUPPORT);
                                    Temp_Resource = vessel.rootPart.RequestResource(Constants.LIFESUPPORT, TotalLS_ResourcesAvail, ResourceFlowMode.ALL_VESSEL);
                                    Log.Info("LifeSupportUsageUpdate.IFIUSEResources 1, RequestedResource, part: " + vessel.rootPart.partInfo.title + ", Temp_Resource: " + Temp_Resource);
                                    double amount2 = Util.GetResourceTotal(vessel, Constants.LIFESUPPORT);
                                    LS_Use = 0;
                                    Log.Info("Before request amount: " + amount + ",  After request: amount2: " + amount2);
                                }
                                else
                                {
                                    Log.Info("RequestResource: " + Constants.LIFESUPPORT + ",  UR_Amount: " + LS_Use);
                                    Temp_Resource = vessel.rootPart.RequestResource(Constants.LIFESUPPORT, LS_Use);
                                }
                                Log.Info("Temp_Resource (result from RequestResource): " + Temp_Resource + ", LS_Use: " + LS_Use);
                                if (LS_Use <= 0)
                                    IFI_Check_Kerbals(vessel);

                            }
                            else
                            {
                                int PartCountForShip = 0;
                                for (int partIdx = 0; partIdx < vessel.protoVessel.protoPartSnapshots.Count; partIdx++)
                                {
                                    ProtoPartSnapshot p = vessel.protoVessel.protoPartSnapshots[partIdx];
                                    PartCountForShip++;
                                    for (int resIdx = 0; resIdx < p.resources.Count; resIdx++)
                                    {
                                        ProtoPartResourceSnapshot LSresource = p.resources[resIdx];
                                        if (LSresource.resourceName == Constants.LIFESUPPORT)
                                        {
                                            Log.Info("Resource found");
                                            double resUsedInPart = Math.Min(LSresource.amount, LS_Use);
                                            LSresource.amount -= resUsedInPart;
                                            LS_Use -= resUsedInPart;

                                            Log.Info("IFIUSEResources 2, Resource: " + Constants.LIFESUPPORT + ",  LSresource.amount: " + LSresource.amount + ",   r.amount: " + LSresource.amount + ",   LS_Use: " + LS_Use);
                                        }
                                    }
                                }
                                if (LS_Needed == LS_Use)
                                    IFI_Check_Kerbals(vessel);

                            }
                        }
                    }
                    else
                    {
                        //days_rem = 0;
                        //LSAval = 200.0;
                    }
                    if (vessel.loaded)
                    {
                        ProcessLoadedVessel(Elapsed_Time, vessel, ref slurryRate, ref sludgeRate);
                    }
                    else
                    {
                        ProcessUnloadedVessel(Elapsed_Time, vessel.protoVessel, ref slurryRate, ref sludgeRate);
                    }

                    DoDisplayCalcs(vessel, IFI_Crew, IFI_ALT, IFI_Location, Elapsed_Time, days_rem, LSAval, slurryRate, sludgeRate);

                }
            }
        }


        private static double IFIGetAllResources(string IFIResource, Vessel vessel)
        {
            double IFIResourceAmt = 0.0;
            double num2 = 0;
            int id = PartResourceLibrary.Instance.GetDefinition(IFIResource).id;
            if (vessel.loaded)
            {
                if (vessel.rootPart == null)
                {
                    return 0;
                }
                vessel.GetConnectedResourceTotals(id, out IFIResourceAmt, out num2, true);
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
            return IFIResourceAmt;
        }

        static bool ResourceConverterActive(ProtoPartSnapshot pps, int j)
        {
            Boolean.TryParse(pps.modules[j].moduleValues.GetValue("isEnabled"), out bool active);
            Boolean.TryParse(pps.modules[j].moduleValues.GetValue("checkForOxygen"), out bool checkForOxygen);

            if (checkForOxygen && !pps.pVesselRef.vesselRef.mainBody.atmosphereContainsOxygen)
                return false;

            return active;
        }


        double CalcLS(Vessel vessel, int IFI_Crew, double Elapsed_Time, out double LSAval, out double days_rem)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine();
            report.AppendLine("============================");
            report.AppendLine("LifeSupport Usage Calculation Report");
            report.AppendLine("============================");

            // 
            // First get the rate per minute, this will be adjusted below to the rate per second
            //  Done this way to try to avoid floating point roundoff errors
            //
            double LS_Use_Per_Minute = LifeSupportRate.GetRatePerMinute();

            LSAval = IFIGetAllResources(Constants.LIFESUPPORT, vessel);

            report.AppendLine("Initial IFI_Crew: " + IFI_Crew + ", Elapsed_Time: " + Elapsed_Time + ", LS_Use: " + LS_Use_Per_Minute + ",  LSAval: " + LSAval);

            if (LifeSupportRate.IntakeAirAvailable(vessel))
            {
                report.AppendLine("IntakeAirAvailable");

                if (vessel.mainBody.name == FlightGlobals.GetHomeBodyName())
                {
                    LS_Use_Per_Minute *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment; //  0.20;
                    report.AppendLine("HomeBody atmospheric adjustment: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment);
                }
                else
                {
                    LS_Use_Per_Minute *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment; // 0.60;
                    report.AppendLine("Atmospheric adjustment: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment);
                }
            }
            if (IFI_Crew > 0)
                days_rem = LSAval / IFI_Crew / (LS_Use_Per_Minute * 60 * (GameSettings.KERBIN_TIME ? 6 : 24));
            else
                days_rem = -1;
            report.AppendLine("After Atmo adjust, LS_Use: " + LS_Use_Per_Minute + ",   IFI_Crew: " + IFI_Crew + ",   Elapsed_Time: " + Elapsed_Time + ", LSAval: " + LSAval); // + ", HoursPerDay: " + HoursPerDay);
            LS_Use_Per_Minute *= IFI_Crew;

            //
            // Now get the usage per second multipled by the elaped time
            //
            var LS_Use = LS_Use_Per_Minute * Elapsed_Time / 60;

            //
            // IF No EC use more LS resources
            //
            if (IFIGetAllResources("ElectricCharge", vessel) < 0.1)
            {
                LS_Use *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment;
                report.AppendLine("No EC available, low EC Adjustement: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment);
            }
            report.AppendLine("Final LS_Use_Per_Second: " + LS_Use);
            report.AppendLine("============================");
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().Debug)
                Log.Info(report.ToString());
            return LS_Use;
        }

        void ProcessLoadedVessel(double Elapsed_Time, Vessel vessel, ref double slurryRate, ref double sludgeRate)
        {
            Log.Info("ProcessLoadedVessel, Elapsed_Time: " + Elapsed_Time);
            StringBuilder report = new StringBuilder();
            report.AppendLine();
            report.AppendLine("============================");
            report.AppendLine("ProcessLoadedVessel Report");
            report.AppendLine("============================");

            for (int idx2 = 0; idx2 < vessel.parts.Count; idx2++)
            {
                for (int m = 0; m < vessel.parts[idx2].Modules.Count; m++)
                {
                    PartModule tmpPM = vessel.parts[idx2].Modules[m];

                    if (vessel.parts[idx2].Modules[m].moduleName == "ModuleIFILifeSupport")
                    {
                        ModuleIFILifeSupport m1 = (ModuleIFILifeSupport)tmpPM;
                        report.AppendLine("Part: " + vessel.parts[idx2].partInfo.title + ", activated: " + m1.IsActivated);
                        Loaded_Converter_Update(Elapsed_Time, m1, ref report, ref  slurryRate, ref  sludgeRate);
                    }
                }
            }
        }

        void Loaded_Converter_Update(double Elapsed_Time, ModuleIFILifeSupport converter, ref StringBuilder report, ref double slurryRate, ref double sludgeRate)
        {
            Log.Info("Loaded_Converter_Update");
            double[] inputResource = new double[10];        // How much is needed for ElapsedTime
            double[] usedInputResource = new double[10];    // How much is available, never more than inputResource
            double[] outputResource = new double[10];


            if (converter.ModuleIsActive())
            {
                //Log.Info("Loaded_Converter_Update, ModuleIsActive:" + converter.part.partInfo.title);
                //Log.Info("converter.checkForOxygen: " + converter.checkForOxygen + ", converter.vessel.mainBody.atmosphereContainsOxygen: " + converter.vessel.mainBody.atmosphereContainsOxygen);
                if (converter.checkForOxygen && !converter.vessel.mainBody.atmosphereContainsOxygen)
                    return;

                double percentResAvail = 1;

                //
                // Simulate getting the resources to find out if enough of all of them is available.
                // If not, then get the lowest percentage of whicever resource is least available
                //
                converter.part.ResetSimulationResources();
                for (int i = 0; i < converter.inputList.Count; i++)
                {
                    inputResource[i] = converter.inputList[i].Ratio * Elapsed_Time;
                    usedInputResource[i] = converter.part.RequestResource(converter.inputList[i].ResourceName, inputResource[i], true);
                    percentResAvail = Math.Min(percentResAvail, usedInputResource[i] / inputResource[i]);
                    if (converter.inputList[i].ResourceName == Constants.SLURRY)
                    {
                        slurryRate += converter.inputList[i].Ratio;
                    }
                    if (converter.inputList[i].ResourceName == Constants.SLUDGE)
                    {
                        slurryRate += converter.inputList[i].Ratio;
                    }
                }

                //
                // Now that we have the percentage, actually request the resources
                //
                for (int i = 0; i < converter.inputList.Count; i++)
                {
                    inputResource[i] = converter.inputList[i].Ratio * Elapsed_Time * percentResAvail;
                    usedInputResource[i] = converter.part.RequestResource(converter.inputList[i].ResourceName, inputResource[i], false);
                    report.AppendLine("Input: " + converter.inputList[i].ResourceName +
                        ", ratio: " + converter.inputList[i].Ratio + ", percentResAvail: " + percentResAvail + ", amt: " + inputResource[i]);

                }

                //
                // And finally, take care of the output from the converter
                //
                for (int i = 0; i < converter.outputList.Count; i++)
                {
                    bool locked = false;
                    for (int r = 0; r < converter.part.Resources.Count; r++)
                        if (converter.part.Resources[r].resourceName == converter.outputList[i].ResourceName)
                        {
                            locked = !converter.part.Resources[r].flowState;
                            break;
                        }

                    outputResource[i] = converter.outputList[i].Ratio * Elapsed_Time * percentResAvail;
                    double resProduced = outputResource[i];

                    report.AppendLine("Output: " + converter.outputList[i].ResourceName);

                    //
                    // Fill up current part first, then the rest of the vessel
                    //
                    if (!locked)
                    {
                        Log.Info("Not locked");
                        int resourceid = PartResourceLibrary.Instance.GetDefinition(converter.outputList[i].ResourceName).id;
                        converter.part.GetConnectedResourceTotals(resourceid, out double amount, out double maxAmount, false);
                        resProduced = Math.Min(outputResource[i], maxAmount - amount);
                        converter.part.RequestResource(converter.outputList[i].ResourceName, -resProduced);
                        report.AppendLine("Resource produced: " + resProduced);
                    }
                    converter.part.RequestResource(converter.outputList[i].ResourceName, -(outputResource[i] - resProduced), ResourceFlowMode.ALL_VESSEL_BALANCE);

                    report.AppendLine("locked: " + locked + ", avail: " + resProduced + ", amt: " + -(outputResource[i] - resProduced));
                }
            }
            report.AppendLine();
            report.AppendLine("============================");
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().Debug)
                Log.Info(report.ToString());
            report.Clear();
        }


        public class AvailPR
        {
            public string partName;
            internal int index;
            internal ProtoPartResourceSnapshot pprs;

            public AvailPR(string partName, int idx, ProtoPartResourceSnapshot snapshot)
            {
                this.partName = partName;
                index = idx;
                pprs = snapshot;
            }
        }

        internal class PartWithIFI
        {
            internal int index;
            internal Part part;
            internal int moduleIndex;

            public PartWithIFI(int idx, Part snapshot, int modIndex)
            {
                index = idx;
                part = snapshot;
                moduleIndex = modIndex;
            }

        }

        internal class Resource
        {
            internal string resName;
            internal List<AvailPR> pprsList;

            internal Resource(string name)
            {
                resName = name;
                pprsList = new List<AvailPR>();
            }
        }

#if false
        internal class AvailPartResource
        {
            internal string partname;
            int index;

            ProtoPartResourceSnapshot pprs;

            internal AvailPartResource(string name, int idx, ProtoPartResourceSnapshot snapshot)
            {
                partname = name;
                index = idx;
                pprs = snapshot;

            }

            override public string ToString() { return "amount: " + pprs.amount.ToString() + ", maxAmount: " + pprs.maxAmount.ToString(); }

            public string Key() { return Key(partname, index, pprs); }
            static public string Key(string name, int idx, ProtoPartResourceSnapshot pprs) { return name + ":" + idx + ":" + pprs.resourceName; }
        }
#endif
        internal class ConverterResources
        {
            internal string resName;
            internal double ratio;
            internal bool dumpExcess;

            internal ConverterResources() { }
            internal ConverterResources(ConverterResources cr)
            {
                resName = cr.resName;
                ratio = cr.ratio;
                dumpExcess = cr.dumpExcess;
            }
        }

        void ProcessUnloadedVessel(double Elapsed_Time, ProtoVessel protoVessel, ref double slurryRate, ref double sludgeRate)
        {

            Log.Info("ProcessUnloadedVessel, vessel: " + protoVessel.vesselName);

            double[] inputResource = new double[10];        // How much is needed for ElapsedTime
            double[] inputResourceSim = new double[10];        // How much is needed for ElapsedTime
            double[] usedInputResource = new double[10];    // How much is available, never more than inputResource
            double[] outputResource = new double[10];

            Dictionary<string, Resource> availPartResource = new Dictionary<string, Resource>();

            //
            // Initialize all working info
            //

            availPartResource.Clear();
            List<ConverterResources> inputResources = new List<ConverterResources>();
            List<ConverterResources> inputResourcesSim = new List<ConverterResources>();
            List<ConverterResources> outputResources = new List<ConverterResources>();

            List<PartWithIFI> moduleLS = new List<PartWithIFI>();

            //
            // Start the report
            //
            StringBuilder report = new StringBuilder();
            report.AppendLine();
            report.AppendLine("============================");
            report.AppendLine("ProcessUnloadedVessel Report");
            report.AppendLine("============================");
            report.AppendLine("Elapsed_Time: " + Elapsed_Time);

            //
            // Loop through all the parts on the vessel, get all the resources and
            // identify all parts with ModuleIFILifeSupport
            //
            for (int partIdx = 0; partIdx < protoVessel.protoPartSnapshots.Count; partIdx++)
            {
                ProtoPartSnapshot p = protoVessel.protoPartSnapshots[partIdx];
                Part part = p.partPrefab;

                Log.Info("ProcessUnloadedVessel, Part: " + part.partInfo.title + ", partIdx: " + partIdx);

                //
                // Identify all resources on the vessel which aren't locked
                //
                foreach (ProtoPartResourceSnapshot res in p.resources)
                {
                    Log.Info("resource: " + res.resourceName + ", flowState: " + res.flowState);
                    if (res.flowState)
                    {
                        if (!availPartResource.ContainsKey(res.resourceName))
                        {
                            availPartResource.Add(res.resourceName, new Resource(res.resourceName));
                        }
                        availPartResource[res.resourceName].pprsList.Add(new AvailPR(part.partInfo.title, partIdx, res));
                    }
                }

                //
                // For each part, loop through all the modules
                // Identify all parts which have an active ModuleIFILifeSupport module
                //
                ConfigNode[] modules = part.partInfo.partConfig.GetNodes("MODULE");
                for (int modIdx = 0; modIdx < modules.Length; modIdx++)
                {
                    var v = modules[modIdx].GetValue("name");

                    if (v == "ModuleIFILifeSupport")
                    {
                        if (ResourceConverterActive(p, modIdx))
                        {
                            moduleLS.Add(new PartWithIFI(partIdx, part, modIdx));
                        }
                        break;
                    }
                }
            }
            Log.Info("Number of parts with ModuleIFILifeSupport: " + moduleLS.Count);
            //
            // For each part with ModuleIFILifeSupport, process
            //
            for (int modLSidx = 0; modLSidx < moduleLS.Count; modLSidx++)
            {
                PartWithIFI ifiPart = moduleLS[modLSidx];

                //
                // Processing starts here
                //
                inputResources.Clear();
                inputResourcesSim.Clear();
                outputResources.Clear();

                //
                // Get the input and output resources for this IFIModuleLifeSupport module
                //
                ConfigNode[] modules = ifiPart.part.partInfo.partConfig.GetNodes("MODULE");
                ConfigNode[] inputRes = modules[ifiPart.moduleIndex].GetNodes(ModuleIFILifeSupport.INPUT);
                ConfigNode[] outputRes = modules[ifiPart.moduleIndex].GetNodes(ModuleIFILifeSupport.OUTPUT);
                foreach (ConfigNode i in inputRes)
                {
                    ConverterResources cr = new ConverterResources();

                    cr.resName = i.GetValue("ResourceName");
                    var ratioStr = i.GetValue("Ratio");
                    cr.ratio = Double.Parse(ratioStr);
                    inputResources.Add(cr);
                    inputResourcesSim.Add(cr);
                }
                foreach (var i in outputRes)
                {
                    ConverterResources cr = new ConverterResources();

                    cr.resName = i.GetValue("ResourceName");
                    var ratioStr = i.GetValue("Ratio");
                    cr.ratio = Double.Parse(ratioStr);
                    var dumpExcessStr = i.GetValue("DumpExcess");
                    cr.dumpExcess = bool.Parse(dumpExcessStr);
                    outputResources.Add(cr);
                }

                //
                // Run through the inputResources list to calculate the percentage of resources which are available
                //
                double percentResAvail = 1;

                for (int i = 0; i < inputResourcesSim.Count; i++)
                {
                    if (availPartResource.ContainsKey(inputResources[i].resName))
                    {
                        inputResourceSim[i] = inputResources[i].ratio * Elapsed_Time;
                        double obtained = 0f;

                        int cnt = 1;

                        while (cnt > 0 && obtained < inputResourceSim[i])
                        {
                            cnt = 0;
                            foreach (AvailPR x1 in availPartResource[inputResources[i].resName].pprsList)
                                if (x1.pprs.amount > 0) cnt++;

                            if (cnt > 0)
                            {
                                double neededPerPart = inputResourceSim[i] / cnt;
                                foreach (AvailPR l in availPartResource[inputResources[i].resName].pprsList)
                                {
                                    if (l.pprs.amount > 0)
                                    {
                                        if (l.pprs.amount >= neededPerPart)
                                        {
                                            obtained += neededPerPart;
                                            l.pprs.amount -= neededPerPart;
                                        }
                                        else
                                        {
                                            obtained += l.pprs.amount;
                                            l.pprs.amount = 0;
                                        }
                                    }
                                }
                            }
                        }
                        if (inputResources[i].resName != "ElectricCharge" || !CheatOptions.InfiniteElectricity)
                            percentResAvail = Math.Min(percentResAvail, obtained / inputResourceSim[i]);
                    }
                }

                //
                // Now actually do the work, using the percentResAvail
                //
                report.AppendLine("percentResAvail: " + percentResAvail);
                for (int i = 0; i < inputResources.Count; i++)
                {
                    if (availPartResource.ContainsKey(inputResources[i].resName))
                    {
                        inputResource[i] = inputResources[i].ratio * Elapsed_Time * percentResAvail;
                        double obtained = 0f;

                        report.AppendLine("Input: " + inputResources[i].resName);
                        int cnt = 1;
                        while (cnt > 0 && obtained < inputResource[i])
                        {
                            cnt = 0;
                            foreach (var x1 in availPartResource[inputResources[i].resName].pprsList)
                                if (x1.pprs.amount > 0)
                                    cnt++;
                            if (cnt > 0)
                            {
                                double neededPerPart = inputResource[i] / cnt;
                                foreach (var l in availPartResource[inputResources[i].resName].pprsList)
                                {
                                    if (l.pprs.amount > 0)
                                    {
                                        var amt = Math.Min(l.pprs.amount, neededPerPart);
                                        report.AppendLine(l.partName + ", amt:" + amt);
                                        obtained += amt;
                                        l.pprs.amount -= amt;
                                    }
                                }
                            }
                        }
                        if (inputResources[i].resName != "ElectricCharge" || !CheatOptions.InfiniteElectricity)
                        {
                            usedInputResource[i] = obtained;
                            inputResources = inputResourcesSim;
                            report.AppendLine("Total: " + obtained);
                        }
                        else
                            report.AppendLine("InfiniteElectricity enabled");
                    }
                }
                report.AppendLine();


                //
                // Loop through the output resources list, putting in what's available, 
                // doing it evenly throughout the vessel
                //
                for (int i = 0; i < outputResources.Count; i++)
                {
                    report.AppendLine("Output: " + outputResources[i].resName);
                    outputResource[i] = outputResources[i].ratio * Elapsed_Time * percentResAvail;
                    int cnt = 1;
                    while (cnt > 0)
                    {
                        cnt = 0;
                        foreach (var l in availPartResource[outputResources[i].resName].pprsList)
                        {
                            if (l.pprs.amount < l.pprs.maxAmount)
                                cnt++;
                        }
                        double availPerPart = outputResource[i] / cnt;
                        double given = 0;
                        foreach (var l in availPartResource[outputResources[i].resName].pprsList)
                        {
                            if (l.pprs.amount < l.pprs.maxAmount)
                            {
                                var amt = Math.Min(availPerPart, l.pprs.maxAmount - l.pprs.amount);
                                l.pprs.amount += amt;
                                given += amt;
                                report.AppendLine(l.partName + ", " + l.pprs.resourceName + ": " + amt + ", amount: " + l.pprs.amount);
                            }
                        }
                        if (given == availPerPart)
                            cnt = 0;
                    }
                }
                report.AppendLine();
            }


            report.AppendLine();
            report.AppendLine("============================");
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().Debug)
                Log.Info(report.ToString());
        }


#if false
        void UnLoaded_Converter_Update(ModuleIFILifeSupport converter)
        {
            double Elapsed_Time = IFI_Get_Elasped_Time();


            double[] inputResource = new double[10];        // How much is needed for ElapsedTime
            double[] usedInputResource = new double[10];    // How much is available, never more than inputResource
            double[] outputResource = new double[10];

            double[] minPercent = new double[10];
            Log.Info("LifeSupportUsageUpdate.UnLoaded_Converter_Update, Elapsed_Time: " + Elapsed_Time + ",   lastUpdateTime: " + lastUpdateTime);

            if (converter.ModuleIsActive())
            {
                Log.Info("LifeSupportUsageUpdate.UnLoaded_Converter_Update, Elapsed_Time: " + Elapsed_Time + ",   lastUpdateTime: " + lastUpdateTime);
                Log.Info("ModuleIFILifeSupport.ModuleIsActive, vessel: " + converter.vessel.name + ",  part: " + converter.part.partInfo.title);
                //Log.Info("ModuleIFILifeSupport, inputList.Count: " + inputList.Count);
                double percentResAvail = 1;
                for (int i = 0; i < converter.inputList.Count; i++)
                {
                    inputResource[i] = converter.inputList[i].Ratio * Elapsed_Time;
                    usedInputResource[i] = converter.part.RequestResource(converter.inputList[i].ResourceName, inputResource[i]);
                    Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update 1, RequestedResource, part: " + converter.part.partInfo.title + ", usedInputResource[" + i + "]: " + usedInputResource[i]);
                    minPercent[i] = Math.Min(1, usedInputResource[i] / inputResource[i]);
                    percentResAvail = Math.Min(percentResAvail, minPercent[i]);
                    Log.Info("ModuleIFILifeSupport.Input Resource: " + converter.inputList[i].ResourceName + ",  amount requested: " + inputResource[i] + ", amount unavailable: " + (inputResource[i] - usedInputResource[i]));
                    Log.Info("ModuleIFILifeSupport.minPercent: " + minPercent[i]);
                }
                for (int i = 0; i < converter.inputList.Count; i++)
                {
                    if (percentResAvail < 1)
                    {
                        double percentUsed = (usedInputResource[i] / inputResource[i]);
                        double percentToreturn = percentResAvail - percentUsed;
                        converter.part.RequestResource(converter.inputList[i].ResourceName, percentToreturn * inputResource[i]);
                        Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update 2, RequestedResource, part: " + converter.part.partInfo.title + ", percentToreturn * inputResource[" + i + "]: " + percentToreturn * inputResource[i]);
                        Log.Info("ModuleIFILifeSupport.Returning Resource: " + converter.inputList[i].ResourceName + ", amount: " + percentToreturn * inputResource[i]);
                    }
                }

                Log.Info("ModuleIFILifeSupport, percentResAvail: " + percentResAvail + ", outputList.Count: " + converter.outputList.Count);

                for (int i = 0; i < converter.outputList.Count; i++)
                {
                    outputResource[i] = -1 * converter.outputList[i].Ratio * Elapsed_Time * percentResAvail;
                    converter.part.RequestResource(converter.outputList[i].ResourceName, outputResource[i]);
                    Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update 3, RequestedResource, part: " + converter.part.partInfo.title + ", outputResource[" + i + "]: " + outputResource[i]);
                    Log.Info("ModuleIFILifeSupport.Output Resource (should be negative): " + converter.outputList[i].ResourceName + ", amount: " + outputResource[i]);
                }
            }
        }
#endif

        private void IFI_Check_Kerbals(Vessel vessel) // Find All Kerbals Hiding on Vessel 
        {
            Log.Info("IFI_Check_Kerbals");
            if (vessel.vesselType == VesselType.EVA)
            {
                try
                {
                    StarvingKerbal.CrewTestEVA(vessel, 0.2f);
                }
                catch (Exception ex) { IFIDebug.IFIMess("Vessel IFI Exception ++Finding Kerbals++ eva " + ex.Message); }
            }
            else
            {
                if (vessel.loaded)
                {
                    for (int idx = 0; idx < vessel.parts.Count; idx++)
                    {
                        Part p = vessel.parts[idx];
                        int IFIcrew = p.protoModuleCrew.Count;
                        if (IFIcrew > 0)
                        {
                            StarvingKerbal.CrewTest(0, p,0.1f);
                        }

                    }
                }
                else
                {
                    for (int idx = 0; idx < vessel.protoVessel.protoPartSnapshots.Count; idx++)
                    {
                        ProtoPartSnapshot p = vessel.protoVessel.protoPartSnapshots[idx];
                        int IFIcrew = p.protoModuleCrew.Count;
                        if (IFIcrew > 0)
                        {
                            StarvingKerbal.CrewTestProto(0, p, 0.1f);
                        }

                    }
                }
            }
        }


        //private bool KerbalEVARescueDetect = false;

        //
        // Special case for Kerbal rescue missions
        //
        public static bool KerbalEVARescueDetect(Vessel v, int CREWHOLD)
        {
            Int16 RescueTest = -1;
            if (!v.loaded)
            {
                ProtoPartSnapshot p = v.protoVessel.protoPartSnapshots[0];
                ConfigNode RT = p.pVesselRef.discoveryInfo;
                System.Int16.TryParse(RT.GetValue("state"), out RescueTest);

            }
            if (v.Parts.Count == 1 && CREWHOLD == 1 && RescueTest == 29)
            {
                // Add Resources to Rescue Contract POD 1 time
                IFIDebug.IFIMess("#### IFI LIfeSupport #### Rescue POD Found with No LS Resource TAG Flagging --" + Convert.ToString(RescueTest));
                ProtoPartSnapshot p = v.protoVessel.protoPartSnapshots[0];
                for (int resIdx = 0; resIdx < p.resources.Count; resIdx++)
                {
                    ProtoPartResourceSnapshot r = p.resources[resIdx];
                    Log.Info("Resource in part: " + p.partInfo.title + ", : " + r.resourceName);
                    if (r.resourceName == Constants.LIFESUPPORT && r.amount == 0)
                    {
                        r.amount = 1;
                        break;
                    }
                }
                return true;
            }
            // DO NOT USE LS ON RESCUE POD TILL player gets  CLOSE
            if (RescueTest == 29)
            {
                return true;
            } 
            return false;
        }


#if false
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
                    Log.Info("LifeSupportUsageUpdate.IFIUSELifeSupport 1, RequestedResource, part: " + vessel.rootPart.partInfo.title + ", Temp_Resource: " + Temp_Resource);
                    double amount2 = Util.GetResourceTotal(vessel, IFIResource);
                    Log.Info("Before request amount: " + amount + ",  After request: amount2: " + amount2);
                }
                else
                {
                    Log.Info("RequestResource (not enough for full request): " + IFIResource + ",  UR_Amount: " + UR_Amount);
                    Temp_Resource = vessel.rootPart.RequestResource(IFIResource, UR_Amount, ResourceFlowMode.ALL_VESSEL);
                    Log.Info("LifeSupportUsageUpdate.IFIUSELifeSupport 2, RequestedResource, part: " + vessel.rootPart.partInfo.title + ", Temp_Resource: " + Temp_Resource);
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
#endif
#if false
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
                        Log.Info("LifeSupportUsageUpdate.IFIUSEResources 1, RequestedResource, part: " + IV.rootPart.partInfo.title + ", Temp_Resource: " + Temp_Resource);
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
                    Log.Info("LifeSupportUsageUpdate.IFIUSEResources 2, RequestedResource, part: " + IV.rootPart.partInfo.title + ", Temp_Resource: " + Temp_Resource);
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

#endif
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
                IFI_Crew = 0;
                foreach (var p in vessel.protoVessel.protoPartSnapshots)
                    IFI_Crew += p.protoModuleCrew.Count;
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
            IFI_LifeSupportTrackingDisplay.Toolbar.SetTexture("IFILS/Textures/IFI_LS_GRN_38", "IFILS/Textures/IFI_LS_GRN_24");
            IFI_LifeSupportTrackingDisplay.Instance.LS_Status_Hold = new string[FlightGlobals.Vessels.Count(), IFI_LifeSupportTrackingDisplay.MAX_STATUSES];
            IFI_LifeSupportTrackingDisplay.Instance.ClearStatusWidths();
            LS_ALERT_LEVEL = 1;
        }

        void DoDisplayCalcs(Vessel vessel, int IFI_Crew, double IFI_ALT, string IFI_Location, double Elapsed_Time, double days_rem, double LSAval, double slurryRate, double sludgeRate)
        {
            Log.Info("Found Vessel: " + vessel.vesselName);//float distance = (float)Vector3d.Distance(vessel.GetWorldPos3D(), FlightGlobals.ActiveVessel.GetWorldPos3D());

            IFI_Resources.UpdateDisplayValues(vessel);

            IFIDebug.IFIMess("IFI_LIFESUPPORT_TRACKING.Life_Support_Update, IFI_Location: " + IFI_Location +
                ",  FlightGlobals.GetHomeBodyName(): " + FlightGlobals.GetHomeBodyName() + ",   IFI_ALT: " + IFI_ALT);
            double SlurryAvail = IFIGetAllResources(Constants.SLURRY, vessel);
            double SludgeAvail = IFIGetAllResources(Constants.SLURRY, vessel);


            IFI_LifeSupportTrackingDisplay.Instance.SetUpDisplayLines(vessel, IFI_Location, IFI_Crew, days_rem, LSAval, SlurryAvail, SludgeAvail, slurryRate, sludgeRate, ref IFI_LifeSupportTrackingDisplay.Instance.LS_Status_Hold_Count);

            IFI_LifeSupportTrackingDisplay.Instance.CheckAlertLevels(days_rem, ref LS_ALERT_LEVEL);
        }

    }
}
