using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using IFILifeSupport;
using System.Collections;
using static IFILifeSupport.RegisterToolbar;

namespace RequiredExperiments
{



    ///////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 
    /// </summary>
    class ModuleRequiredExperiment : PartModule, IModuleInfo
    {
        [KSPField]
        public string requiredExperimentID = "";

        [KSPField]
        public string modulesInitiallyDisabled = "";

        [KSPField]
        public float requiredCompleted = 0.001f; // between 0 and 1, 1 = 100%

        [KSPField]
        float delayBeforeAvailable = 0; // How many seconds after the experiment(s) have been completed before the part becomes active

        [KSPField]
        public string biomes = ""; // comma seperated field

    

        // Required Situation
        // default is all situations, this can be overridden by 
        // either specifying a requiredSitMask OR defining one or more
        // of the situation boolean flags as true.

        [KSPField]
        public int requiredSitMask = 0;

        [KSPField]
        public bool SrfLanded = false;

        [KSPField]
        public bool SrfSplashed = false;

        [KSPField]
        public bool FlyingLow = false;

        [KSPField]
        public bool FlyingHigh = false;

        [KSPField]
        public bool InSpaceLow = false;

        [KSPField]
        public bool InSpaceHigh = false;

        // [KSPField(guiActive = true, guiName = "Status:")]
        string statusReason = "";

        public int statuscount = 0;

        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string status01 = "";
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string status02 = "";
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string status03 = "";
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string status04 = "";
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string status05 = "";
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string status06 = "";

        [KSPField]
        public bool uniqueBodies = true;      // If true, then the experiment must have been completed in the planet's soi the vessel is in.

        [KSPField]
        public bool inCurrentSituation = true;

        [Persistent]
        ScienceExperiment experiment;

        [Persistent]
        double availableAtTime = -1; // used to know when modules should be enabled

        void SetStatusReason(string value)
        {
            //if (statusReason != "")
            //    statusReason += ", ";
            statusReason += value;
            //Fields["statusReason"].guiActive = (statusReason != "");
            if (value != "")
                Log.Info(value);

            switch (statuscount)
            {
                case 0:
                    {
                        Fields["status01"].guiActive = true;
                        Fields["status01"].guiActiveEditor = true;
                        Fields["status01"].guiName = name;
                        Fields.SetValue("status01", value);
                        statuscount++;
                        break;
                    }
                case 1:
                    {
                        Fields["status02"].guiActive = true;
                        Fields["status02"].guiActiveEditor = true;
                        Fields["status02"].guiName = name;
                        Fields.SetValue("status02", value);
                        statuscount++;
                        break;
                    }
                case 2:
                    {
                        Fields["status03"].guiActive = true;
                        Fields["status03"].guiActiveEditor = true;
                        Fields["status03"].guiName = name;
                        Fields.SetValue("status03", value);
                        statuscount++;
                        break;
                    }
                case 3:
                    {
                        Fields["status04"].guiActive = true;
                        Fields["status04"].guiActiveEditor = true;
                        Fields["status04"].guiName = name;
                        Fields.SetValue("status04", value);
                        statuscount++;
                        break;
                    }
                case 4:
                    {
                        Fields["status05"].guiActive = true;
                        Fields["status05"].guiActiveEditor = true;
                        Fields["status05"].guiName = name;
                        Fields.SetValue("status05", value);
                        statuscount++;
                        break;
                    }
                case 5:
                    {
                        Fields["status06"].guiActive = true;
                        Fields["status06"].guiActiveEditor = true;
                        Fields["status06"].guiName = name;
                        Fields.SetValue("status06", value);
                        statuscount++;
                        break;
                    }
            }
        }
        public void ClearStatus()
        {
            statusReason = "";
            Fields["status01"].guiActive = false;
            Fields["status01"].guiActiveEditor = false;
            Fields["status02"].guiActive = false;
            Fields["status02"].guiActiveEditor = false;
            Fields["status03"].guiActive = false;
            Fields["status03"].guiActiveEditor = false;
            Fields["status04"].guiActive = false;
            Fields["status04"].guiActiveEditor = false;
            Fields["status05"].guiActive = false;
            Fields["status05"].guiActiveEditor = false;
            Fields["status06"].guiActive = false;
            Fields["status06"].guiActiveEditor = false;

            statuscount = 0;
        }

        string[] biomeAr = null;

        const string MODULETITLE = "Required Experiments";


        // private IEnumerator coroutine;
        const float WAITTIME = 15f;

        string[] initiallyDisabledModulesAr;

        public class DisabledModule
        {
            public string modName;
            public List<string> disabledEvents = new List<string>();
            public List<string> disabledFields = new List<string>();
            public List<string> disabledActions = new List<string>();
        }
        Dictionary<string, DisabledModule> disabledModules = new Dictionary<string, DisabledModule>();


        void InitSituation()
        {
            if (SrfLanded || SrfSplashed || FlyingLow || FlyingHigh || InSpaceLow || InSpaceHigh)
            {
                requiredSitMask = 0;
                if (SrfLanded)
                    requiredSitMask += (int)ExperimentSituations.SrfLanded;
                if (SrfSplashed)
                    requiredSitMask += (int)ExperimentSituations.SrfSplashed;
                if (FlyingLow)
                    requiredSitMask += (int)ExperimentSituations.FlyingLow;
                if (FlyingHigh)
                    requiredSitMask += (int)ExperimentSituations.FlyingHigh;
                if (InSpaceLow)
                    requiredSitMask += (int)ExperimentSituations.InSpaceLow;
                if (InSpaceHigh)
                    requiredSitMask += (int)ExperimentSituations.InSpaceHigh;
            }
            else
            {
                int s = requiredSitMask & (int)ExperimentSituations.SrfLanded;
                SrfLanded = (s > 0);
                s = requiredSitMask & (int)ExperimentSituations.SrfSplashed;
                SrfSplashed = (s > 0);
                s = requiredSitMask & (int)ExperimentSituations.FlyingLow;
                FlyingLow = (s > 0);
                s = requiredSitMask & (int)ExperimentSituations.FlyingHigh;
                FlyingHigh = (s > 0);
                s = requiredSitMask & (int)ExperimentSituations.InSpaceLow;
                InSpaceLow = (s > 0);
                s = requiredSitMask & (int)ExperimentSituations.InSpaceHigh;
                InSpaceHigh = (s > 0);
            }
        }


#if DEBUG
        private Rect ifiDebugWinRect;
        void OnGUI()
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR || !HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            ifiDebugWinRect = GUILayout.Window(123532, ifiDebugWinRect, DrawIFIDebugWin, "IFI Debug Window");
        }

        void DrawIFIDebugWin(int windowID)
        {
#if true
            return;
#else
            GUILayout.BeginHorizontal(GUILayout.Width(400));
            if (requiredExperimentID != null)
                GUILayout.Label("requiredExperimentID: " + requiredExperimentID);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (experiment != null && experiment.experimentTitle != null)
                GUILayout.Label("experiment.title: " + experiment.experimentTitle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (biomes != null)
                GUILayout.Label("biomes: " + biomes);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("requiredSitMask: " + requiredSitMask);
            string sit = "";
            if (SrfLanded)
                sit += ExperimentSituations.SrfLanded + ", ";
            if (SrfSplashed)
                sit += ExperimentSituations.SrfSplashed + ", ";
            if (FlyingLow)
                sit += ExperimentSituations.FlyingLow + ", ";
            if (FlyingHigh)
                sit += ExperimentSituations.FlyingHigh + ", ";
            if (InSpaceLow)
                sit += ExperimentSituations.InSpaceLow + ", ";
            if (InSpaceHigh)
                sit += ExperimentSituations.InSpaceHigh;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (sit != null)
                GUILayout.Label("Situations: " + sit);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("uniquePlanets: " + uniqueBodies);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("All Conditions met: " + AllConditionsMet().ToString() + ", reason: " + statusReason);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("UnivTime: " + Planetarium.GetUniversalTime().ToString("n0") + ",  available at: " + availableAtTime.ToString("n0"));
            GUILayout.EndHorizontal();
            GUI.DragWindow();
#endif
        }

#endif
        void Start()
        {
            if (HighLogic.CurrentGame== null || requiredExperimentID == "" || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                return;
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;

            experiment = ResearchAndDevelopment.GetExperiment(requiredExperimentID);
            if (experiment == null)
            {
                Log.Error("Experiment not found: " + requiredExperimentID);
                return;
            }

            InitSituation();

            Log.Info("requiredExperimentID: " + requiredExperimentID);
            Log.Info("experiment.title: " + experiment.experimentTitle);
            Log.Info("biomes: " + biomes);
            Log.Info("requiredSitMask: " + requiredSitMask);
            Log.Info("uniquePlanets: " + uniqueBodies);

            char[] splitString = { ',' };

            initiallyDisabledModulesAr = modulesInitiallyDisabled.Split(splitString);
            if (initiallyDisabledModulesAr != null)
                for (int i = 0; i < initiallyDisabledModulesAr.Count(); i++)
                    initiallyDisabledModulesAr[i] = initiallyDisabledModulesAr[i].Trim();

            biomeAr = biomes.Split(splitString);

            bool updated = false;
            for (int idx = 0; idx < part.partInfo.moduleInfos.Count; idx++)
            {
                var s = part.partInfo.moduleInfos[idx];
                Log.Info("ModuleInfo, name: " + s.moduleName);
                Log.Info("s: " + s.ToString());
                if (s.moduleName == MODULETITLE)
                {
                    Log.Info("Updating info for ModuleRequiredExperiment");
                    s.info = GetInfo();
                    updated = true;
                    break;
                }
            }
            if (!updated)
            {
                var s = new AvailablePart.ModuleInfo
                {
                    info = GetInfo(),
                    moduleName = MODULETITLE,
                    moduleDisplayName = MODULETITLE
                };
                Log.Info("Adding info: " + MODULETITLE + ",   " + info);
                part.partInfo.moduleInfos.Add(s);
            }
            //CheckExperiments();
            StartCoroutine(Refresh());
            //InvokeRepeating("Refresh", 0, HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().refreshInterval);

            //GameEvents.OnScienceChanged.Add(onScienceChanged);
            //GameEvents.OnScienceRecieved.Add(onScienceReceived);            
        }

        IEnumerator Refresh()
        {
            while (true)
            {
                if (HighLogic.LoadedScene != GameScenes.EDITOR)
                {
                    Log.Info("Refresh, availableAtTime: " + availableAtTime.ToString("n0") + ",   currentUT: " + Planetarium.GetUniversalTime().ToString("n0"));
                    CheckExperiments();
                    CheckExperimentCompletions();
                    Log.Info("After CheckExperimentCompletions, availableAtTime: " + availableAtTime.ToString("n0") + ",   currentUT: " + Planetarium.GetUniversalTime().ToString("n0"));
                    if (Planetarium.GetUniversalTime() >= availableAtTime && availableAtTime > 0)
                    {
                        EnableModules();
                        availableAtTime = -1;
                    }
                    yield return Util. WaitForSecondsRealtimeLogged("ModuleRequiredExperiment.Refresh 1", 1f); //  HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().RefreshInterval);
                }
                else
                    yield return Util.WaitForSecondsRealtimeLogged("ModuleRequiredExperiment.Refresh 2", 0.1f);
            }
        }

        void Destroy()
        {
            //GameEvents.OnScienceChanged.Remove(onScienceChanged);
            //GameEvents.OnScienceRecieved.Remove(onScienceReceived);
            disabledModules.Clear();
        }

#if false
        void onScienceChanged(float f, TransactionReasons tr)
        {
            coroutine = CheckExperimentCompletions(WAITTIME);
            StartCoroutine(coroutine);
        }

        void onScienceReceived(float f, ScienceSubject ss, ProtoVessel pv, bool b)
        {
            coroutine = CheckExperimentCompletions(WAITTIME);
            StartCoroutine(coroutine);
        }


        IEnumerator CheckExperimentCompletions(float waitTime)
        {
            yield return new WaitForSecondsLogged(waitTime);
#else
        void CheckExperimentCompletions()
        {
#endif
            Log.Info("CheckExperimentCompletions");
            if (AllConditionsMet())
            {
                if (availableAtTime == -1)
                {
                    availableAtTime = Planetarium.GetUniversalTime() + delayBeforeAvailable;
                    //                EnableModules();
                }
            }
            else
            {
                Log.Info("All conditions NOT met");
                availableAtTime = -1;
            }
        }

        /// <summary>
        /// Enable all the disabled modules
        /// </summary>
        void EnableModules()
        {
            Log.Info("EnableModules");
            foreach (var dm in disabledModules)
            {
                foreach (var mod in part.Modules)
                    if (mod.moduleName == dm.Value.modName)
                    {
                        foreach (var e in dm.Value.disabledEvents)
                            mod.Events[e].active = true;
                        foreach (var f in dm.Value.disabledFields)
                            mod.Fields[f].guiActive = true;
                        foreach (var a in dm.Value.disabledActions)
                            mod.Actions[a].active = true;

                        dm.Value.disabledEvents.Clear();
                        dm.Value.disabledFields.Clear();
                        dm.Value.disabledActions.Clear();
                    }
            }
        }

        public string CurrentRawBiome(Vessel vessel)
        {
            Log.Info("CurrentRawBiome");
            if (vessel != null && vessel.landedAt != null && vessel.landedAt != string.Empty)
                return Vessel.GetLandedAtString(vessel.landedAt);
            string biome = ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude);
            return "" + biome;
        }

        #region SirDiazo_LandingHeight
        /// <summary>
        /// part's distace to CelestialBody CoM for distance sort
        /// </summary>
        public class partDist
        {
            public Part prt;
            public float dist;
        }

        /// <summary>
        /// The actual calcualtions here.  Only change made was to replace the LandingHeight doubleMax function with Math.Max
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public double heightToLand(Vessel vessel) //leave public so other mods can call
        {
            double landHeight = 0;
            double landHeightBackup = 0;
            bool firstRay = true;



            if (vessel.LandedOrSplashed) //if landed or splashed, height is 0
            {
                landHeight = 0;
                landHeightBackup = 0;
                //Debug.Log("LH-A");
            }
            else if ((vessel.mainBody.ocean && (vessel.altitude - Math.Max(vessel.pqsAltitude, 0)) > 2400) || (!vessel.mainBody.ocean && (vessel.altitude - vessel.pqsAltitude) > 2400)) //raycast goes wierd outside physics range
            {
                //Debug.Log("LH " + FlightGlobals.ActiveVessel.altitude + "|" + FlightGlobals.ActiveVessel.pqsAltitude + "|" + FlightGlobals.ActiveVessel.mainBody.Radius);
                if (vessel.mainBody.ocean)
                {
                    landHeight = vessel.altitude - Math.Max(vessel.pqsAltitude, 0); //more then 2400 above ground, just use vessel CoM
                    landHeightBackup = vessel.altitude - Math.Max(vessel.pqsAltitude, 0); //more then 2400 above ground, just use vessel CoM
                }
                else
                {
                    landHeight = vessel.altitude - vessel.pqsAltitude; //more then 2400 above ground, just use vessel CoM
                    landHeightBackup = vessel.altitude - vessel.pqsAltitude; //more then 2400 above ground, just use vessel CoM
                }
                //Debug.Log("LH-B");
            }
            else //inside physics range, goto raycast
            {
                //Debug.Log("LH-c");
                List<Part> partToRay = new List<Part>(); //list of parts to ray
                if (vessel.Parts.Count < 50) //if less then 50 parts, just raycast all parts
                {
                    partToRay = vessel.Parts;
                }
                else //if over 50 parts, only raycast the 30 parts closest to ground. difference between 30 and 50 parts to make the sort worth the processing cost, no point in running the sort on 31 parts as the sort costs more processor power then 1 raycast
                {
                    List<partDist> partHeights = new List<partDist>(); //only used above 50 parts, links part to distance to ground
                    for (int idx = 0; idx < vessel.Parts.Count; idx++)
                    //foreach (Part p in vessel.Parts)
                    {
                        Part p = vessel.Parts[idx];
                        partHeights.Add(new partDist() { prt = p, dist = Vector3.Distance(p.transform.position, vessel.mainBody.position) }); //create list of parts and their distance to ground
                                                                                                                                              //print("a: " + Vector3.Distance(p.transform.position, FlightGlobals.ActiveVessel.mainBody.position));
                    }
                    partHeights.Sort((i, j) => i.dist.CompareTo(j.dist)); //sort parts so parts closest to ground are at top of list
                    for (int i = 0; i < 30; i = i + 1)
                    {
                        partToRay.Add(partHeights[i].prt); //make list of 30 parts closest to ground
                                                           //print("b: " + i + " " + partHeights[i].prt.name + " " + partHeights[i].dist + " " + Vector3.Distance(FlightGlobals.ActiveVessel.CoM, FlightGlobals.ActiveVessel.mainBody.position));
                    }

                }

                ////test code START
                //GameObject go = pHit.collider.gameObject;
                //Debug.Log("LH Hit GO " + go.layer + "|" + go.name + "|" + go.tag +"|"+go.GetType()+"|"+pHit.distance);
                //if(go.GetComponent<PQ>() != null)
                //{
                //    Debug.Log("LH Hit PQS!");
                //}
                //else
                //{
                //    Debug.Log("LH Hit something else");
                //}
                //foreach (Component co in go.GetComponents<UnityEngine.Object>())
                //{
                //    Debug.Log("LH Co in Go hit " + co.name + "|" + co.tag + "|" + co.GetType());
                //}
                //foreach (Part cop in go.GetComponentsInParent<Part>())
                //{
                //    Debug.Log("LH  part hit " + FlightGlobals.ActiveVessel.Parts.Contains(cop));
                //}

                //test code END
                //Debug.Log("LH Alt " + FlightGlobals.ActiveVessel.pqsAltitude + "|" + FlightGlobals.ActiveVessel.altitude);
                //Debug.Log("LH START!");
                // Debug.Log("LH distances" + FlightGlobals.ActiveVessel.mainBody.Radius + "|" + FlightGlobals.ActiveVessel.PQSAltitude());
                foreach (Part p in partToRay)
                {
                    try
                    {
                        if (p.collider.enabled) //only check part if it has collider enabled
                        {

                            Vector3 partEdge = p.collider.ClosestPointOnBounds(FlightGlobals.currentMainBody.position); //find collider edge closest to ground
                            RaycastHit pHit;
                            Vector3 rayDir = (FlightGlobals.currentMainBody.position - partEdge).normalized;
                            Ray pRayDown = new Ray(partEdge, rayDir);
                            LayerMask testMask = 32769; //hit layer 0 for parts and layer 15 for groud/buildings
                                                        // Debug.Log("LH test mask " + testMask.ToString());
                                                        //LayerMask testMask = 32769;
                                                        //if (Physics.Raycast(pRayDown, out pHit, (float)(FlightGlobals.ActiveVessel.mainBody.Radius + FlightGlobals.ActiveVessel.altitude), pRayMask)) //cast ray
                            if (Physics.Raycast(pRayDown, out pHit, (float)(vessel.mainBody.Radius + vessel.altitude), testMask)) //cast ray
                            {
                                // Debug.Log("LH hit dist orig " + pHit.distance);
                                if (pHit.collider.gameObject.layer == 15 || pHit.collider.gameObject.layer == 0 && !vessel.parts.Contains(pHit.collider.gameObject.GetComponentInParent<Part>()))
                                {//valid hit, accept all layer 15 hits but only those on layer 0 when part hit is not part of ActiveVessel
                                    float hitDist = pHit.distance; //need to do math so make a local variable for it

                                    if (vessel.mainBody.ocean)
                                    {
                                        if (vessel.PQSAltitude() < 0) //if negative, over ocean and pqs layer is seabed
                                        {
                                            hitDist = hitDist + (float)vessel.PQSAltitude(); //reduce distance of raycast by distance from seabed to sea level, remebed PQSAltitude is negative so 'add' it to reduce the distance
                                                                                             //Debug.Log("LH hit dist after ocean" + hitDist);
                                        }
                                    }

                                    if (firstRay) //first ray this update, always set height to this
                                    {


                                        landHeight = hitDist;

                                        firstRay = false;
                                        //Debug.Log("LH 3 " + landHeight);

                                    }
                                    else
                                    {

                                        landHeight = Math.Min(landHeight, hitDist);
                                        //Debug.Log("LH 3a " + landHeight + "|" + pHit.distance);

                                    }
                                }

                            }
                            else if (!firstRay) //error trap, ray hit nothing
                            {
                                landHeight = Math.Min(landHeight, vessel.altitude - vessel.pqsAltitude);
                                firstRay = false;
                            }
                            //Debug.Log("LH hit dist orig " + pHit.distance);
                        }
                    }
                    catch
                    {
                        landHeight = vessel.altitude - vessel.pqsAltitude;
                        firstRay = false;
                        //Debug.Log("LH 2");
                    }

                }
                //if(landHeight == 0)
                //{
                //    landHeight = landHeightBackup;
                //}
                if (!firstRay && landHeight == 0)
                {
                    landHeight = vessel.altitude - vessel.pqsAltitude;
                    firstRay = false;
                }
                if (landHeight < 1) //if we are in the air, always display an altitude of at least 1
                {
                    //Debug.Log("LH 4 " + landHeight);
                    landHeight = 1;
                    //Debug.Log("LH 4");
                }
                //Debug.Log("LH 5 " + landHeight);
            }

            //if (FlightGlobals.ActiveVessel.mainBody.ocean) //if mainbody has ocean we land on water before the seabed
            //{
            //    //Debug.Log("LH 5");
            //    if (landHeight > FlightGlobals.ActiveVessel.altitude)
            //    {
            //        landHeight = FlightGlobals.ActiveVessel.altitude;
            //        //Debug.Log("LH 6");
            //    }
            //}

            return landHeight;
        }

        #endregion

        /// <summary>
        /// Check to see if all the experiments have been completed for the current locatoion
        /// </summary>
        /// <returns></returns>
        bool AllConditionsMet()
        {
            Log.Info("AllConditionsMet");
            var currentBiome = CurrentRawBiome(this.vessel);
            var currentSituation = this.vessel.situation;

            ExperimentSituations curExpSituation;
            var terrainHeight = vessel.mainBody.TerrainAltitude(vessel.latitude, vessel.longitude, true);
            double altitude = heightToLand(this.vessel) + terrainHeight;

            if (terrainHeight < 0.0 && vessel.mainBody.ocean && altitude <= 0.0)
            {
                curExpSituation = ExperimentSituations.SrfSplashed;
            }
            else if (altitude <= terrainHeight)
            {
                curExpSituation = ExperimentSituations.SrfLanded;
            }
            else if (vessel.mainBody.atmosphere && altitude < vessel.mainBody.atmosphereDepth)
            {
                curExpSituation = ((altitude >= (double)vessel.mainBody.scienceValues.flyingAltitudeThreshold) ? ExperimentSituations.FlyingHigh : ExperimentSituations.FlyingLow);
            }
            else
            {
                curExpSituation = ((altitude >= (double)vessel.mainBody.scienceValues.spaceAltitudeThreshold) ? ExperimentSituations.InSpaceHigh : ExperimentSituations.InSpaceLow);
            }
            //Log.Info("altitude: " + altitude + ",   terrainheight: " + terrainHeight);

            //Log.Info("scienceKey: " + scienceKey);


            var celst = ExperimentTracker.GetCompletedExperiment(requiredExperimentID);
            foreach (var c in celst)
            {
                Log.Info("CompletedExperiment, Biome: " + c.biome + ",  Body: " + c.body + ",   situation: " + c.situation
                    );
            }
#if false
            ScienceExperiment Experiment = ResearchAndDevelopment.GetExperiment(requiredExperimentID);
            if (Experiment != null)
            {
                // If any biomes are specified, then that will require the planet name as well
                if (biomes != "")
                {
                    string scienceKey = vessel.mainBody.name + curExpSituation.ToString() + currentBiome;
                    foreach (var s in Experiment.Results)
                    {
                        //Log.Info("ExperimentResults, key: " + s.Key + ", value: " + s.Value);
                        if (s.Key == scienceKey)
                        {
                            //Log.Info("Key found, returning true");
                            return true;
                        }
                    }
                }
                else
                {
                    // If no biomes are specified, but unique planets are, then use the planet name + situation, and check substring
                    if (uniquePlanets)
                    {
                        string scienceKey = vessel.mainBody.name + curExpSituation.ToString();
                        foreach (var s in Experiment.Results)
                        {
                            if (s.Key.Substring(0, scienceKey.Length) == scienceKey)
                            {
                                //Log.Info("Key found, returning true");
                                return true;
                            }
                        }
                    }
                    else
                    {
                        foreach (var s in Experiment.Results)
                        {
                            if (s.Key.Contains(curExpSituation.ToString()))
                                return true;
                        }
                    }
                }
            }

            return false;
#else
            ClearStatus();

            if (inCurrentSituation)
            {
                //string scienceKey = vessel.mainBody.name + curExpSituation.ToString() + currentBiome;
                bool inCurrentBody = false;
                bool inCurrentBiome = false;
                bool inCurrentSit = false;

                for (int c = 0; c < celst.Count(); c++)
                {
                    inCurrentBody = false;
                    if (celst[c].body == vessel.mainBody.name)
                    {
                        inCurrentBody = true;
                        inCurrentBiome = false;
                        if (celst[c].biome == currentBiome || curExpSituation == ExperimentSituations.InSpaceHigh || curExpSituation == ExperimentSituations.InSpaceLow)
                        {
                            inCurrentBiome = true;
                            inCurrentSit = false;
                            if (celst[c].situation == curExpSituation)
                            {
                                inCurrentSit = true;
                                break;
                            }
                        }
                    }
                }
                if (!inCurrentBody)
                    SetStatusReason("Experiment not done in current body");
                if (!inCurrentBiome && curExpSituation != ExperimentSituations.InSpaceHigh && curExpSituation != ExperimentSituations.InSpaceLow)
                    SetStatusReason("Experiment not done in current Biome");
                if (!inCurrentSit)
                    SetStatusReason("Experiment not done in current situation");
                if (!inCurrentBody || !inCurrentBiome || !inCurrentSit)
                    return false;
            }

            if (uniqueBodies)
            {
                for (int c = 0; c < celst.Count(); c++)
                {
                    if (celst[c].body == vessel.mainBody.name)
                    {
                        return CheckBiomesAndSituations(celst);
                    }
                }
            }
            else
            {
                return CheckBiomesAndSituations(celst);
            }

            return true;
#endif
        }

        bool CheckBiomesAndSituations(List<ExperimentTracker.CompletedExperiment> celst)
        {
            if (biomes != "")
            {
                int biomeCnt = biomeAr.Count();
                int reqBiomeCnt = celst.Count();
                Log.Info("biomeCnt: " + biomeCnt);

                for (int i = 0; i < biomeAr.Count(); i++)
                {
                    for (int c = 0; c < celst.Count(); c++)
                    {
                        if (biomeAr[i] == celst[c].biome)
                        {
                            reqBiomeCnt--;
                        }
                    }
                }


                if (reqBiomeCnt > 0)
                {
                    SetStatusReason("Experiment not completed in a required biomes");
                    return false;
                }
            }

            if (SrfLanded)
            {
                bool b = false;
                for (int c = 0; c < celst.Count(); c++)
                {
                    Log.Info("situation: " + celst[c].situation.ToString());
                    if (celst[c].situation == ExperimentSituations.SrfLanded)
                    {
                        if (celst[c].science / celst[c].scienceCap >= requiredCompleted)
                            return true;
                        else
                            SetStatusReason("Not enough science completed yet");
                    }
                }
                if (!b)
                {
                    if (statusReason == "")
                        SetStatusReason("Not SrfLanded yet");
                    return false;
                }
            }
            if (SrfSplashed)
            {
                bool b = false;
                for (int c = 0; c < celst.Count(); c++)
                    if (celst[c].situation == ExperimentSituations.SrfSplashed)
                    {
                        if (celst[c].science / celst[c].scienceCap >= requiredCompleted)
                            return true;
                        else
                            SetStatusReason("Not enough science completed yet");
                    }

                if (!b)
                {
                    if (statusReason == "")
                        SetStatusReason("Not SrfSplashed yet");
                    return false;
                }
            }
            if (FlyingLow)
            {
                bool b = false;
                for (int c = 0; c < celst.Count(); c++)
                    if (celst[c].situation == ExperimentSituations.FlyingLow)
                    {
                        if (celst[c].science / celst[c].scienceCap >= requiredCompleted)
                            return true;
                        else
                            SetStatusReason("Not enough science completed yet");
                    }

                if (!b)
                {
                    if (statusReason == "")
                        SetStatusReason("Not FlyingLow yet");
                    return false;
                }
            }
            if (FlyingHigh)
            {
                bool b = false;
                for (int c = 0; c < celst.Count(); c++)
                    if (celst[c].situation == ExperimentSituations.FlyingHigh)
                    {
                        if (celst[c].science / celst[c].scienceCap >= requiredCompleted)
                            return true;
                        else
                            SetStatusReason("Not enough science completed yet");
                    }
                if (!b)
                {
                    if (statusReason == "")
                        SetStatusReason("Not FlyingHigh yet");
                    return false;
                }
            }
            if (InSpaceLow)
            {
                bool b = false;
                for (int c = 0; c < celst.Count(); c++)
                    if (celst[c].situation == ExperimentSituations.InSpaceLow)
                    {
                        if (celst[c].science / celst[c].scienceCap >= requiredCompleted)
                            return true;
                        else
                            SetStatusReason("Not enough science completed yet");
                    }

                if (!b)
                {
                    if (statusReason == "")
                        SetStatusReason("Not InSpaceLow yet");
                    return false;
                }
            }
            if (InSpaceHigh)
            {
                bool b = false;
                for (int c = 0; c < celst.Count(); c++)
                    if (celst[c].situation == ExperimentSituations.InSpaceHigh)
                    {
                        if (celst[c].science / celst[c].scienceCap >= requiredCompleted)
                            return true;
                        else
                            SetStatusReason("Not enough science completed yet");
                    }

                if (!b)
                {
                    if (statusReason == "")
                        SetStatusReason("Not InSpaceHigh yet");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check the listed modules and experiments to see what, if any modules, need to be disabled
        /// </summary>
        void CheckExperiments()
        {
            //if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            Log.Info("CheckExperiments");
            foreach (var modToDisable in initiallyDisabledModulesAr)
            {
                Log.Info("CheckExperiments: " + modToDisable);
                DisabledModule disabledModule = null;
                disabledModules.TryGetValue(modToDisable, out disabledModule);
                if (disabledModule == null)
                {
                    disabledModule = new DisabledModule();
                    disabledModule.modName = modToDisable;
                    disabledModules.Add(modToDisable, disabledModule);
                }
                foreach (var mod in part.Modules)
                {
                    if (mod.moduleName == modToDisable)
                    {
                        Log.Info("Disabling " + modToDisable);
                        foreach (BaseEvent e in mod.Events)
                        {
                            if (e.active)
                            {
                                Log.Info("Disabling " + modToDisable + ",   event: " + e.name);
                                disabledModule.disabledEvents.Add(e.name);
                                e.active = false;
                            }
                        }
                        foreach (BaseField f in mod.Fields)
                        {
                            if (f.guiActive)
                            {
                                Log.Info("Disabling " + modToDisable + ",   field: " + f.name);
                                disabledModule.disabledFields.Add(f.name);
                                f.guiActive = false;
                            }
                        }
                        foreach (BaseAction a in mod.Actions)
                        {
                            if (a.active)
                            {
                                Log.Info("Disabling " + modToDisable + ",   action: " + a.name);
                                disabledModule.disabledActions.Add(a.name);
                                a.active = false;
                            }
                        }
                    }
                }
                disabledModules[modToDisable] = disabledModule;
            }
        }

        string info = "";
        void AddToInfo(string s)
        {
            if (info != "")
            {
                if (s.Substring(0, 1) == " ")
                    info += ",";
                info += "\n";
            }
            info += s;
        }


        public override string GetInfo()
        {
            info = "";
            if (experiment == null)
            {
                experiment = ResearchAndDevelopment.GetExperiment(requiredExperimentID);
            }
            if (!HighLogic.LoadedSceneIsFlight)
            {
                info = experiment.experimentTitle;
                  info += " is required to be completed";
                if (inCurrentSituation)
                    info += " in the biome";
                
                if (uniqueBodies)
                    info += " at each body";
                info += " before use";

                return info;
            }

            AddToInfo(experiment.experimentTitle);
            if (SrfLanded)
                AddToInfo("     Landed on surface");
            if (SrfSplashed)
                AddToInfo("     In the water");
            if (FlyingLow)
                AddToInfo("     Flying low");
            if (FlyingHigh)
                AddToInfo("     Flying High");
            if (InSpaceLow)
                AddToInfo("     In low space");
            if (InSpaceHigh)
                AddToInfo("     In high space");

            if (biomes != "")
            {
                AddToInfo("in the following biomes:");
                foreach (var b in biomeAr)
                    AddToInfo("     " + b);
            }
            return info;
        }


        // Following needed for the IModule interface

        public string GetModuleTitle()
        {
            return MODULETITLE;
        }
        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }
        public string GetPrimaryField()
        {
            return "Missing Experiments";
        }

    }
}
