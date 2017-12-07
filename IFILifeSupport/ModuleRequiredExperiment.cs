using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using IFILifeSupport;


using System.Collections;

using KSP.UI.Screens;

namespace RequiredExperiments
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ExperimentTracker : MonoBehaviour
    {
        string lastGameTitle = "";
        double lastUT = 0;
        private IEnumerator coroutine;
        const float WAITTIME = 2f;

        public class CompletedExperiment
        {
            public string name = "";
            public string body = "";
            public ExperimentSituations situation = 0;
            public string location = "";
            public string biome = "";
            public float science = 0;
            public float scienceCap = 0;
        }
        static internal List<CompletedExperiment> completedExps = new List<CompletedExperiment>();

        public class BodyBiome
        {
            public CelestialBody body;
            public string[] biomes;
        }
        static Dictionary<string, BodyBiome> allBodyBiomes = null;

        internal static List<CompletedExperiment> GetCompletedExperiment(string expId)
        {
            var ceAr = completedExps.Where(ce => ce.name == expId).ToList();

            return ceAr;
        }

        void InitBodyBiomes()
        {
            if (allBodyBiomes != null)
                allBodyBiomes.Clear();
            else
                allBodyBiomes = new Dictionary<string, BodyBiome>();
            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                //Log.Info("CelestialBody: " + cb.name);
                BodyBiome bb = new BodyBiome();
                if (cb.BiomeMap != null)
                    bb.biomes = cb.BiomeMap.Attributes.Select(y => y.name).ToArray();
                else
                    bb.biomes = new string[0];
                bb.body = cb;
                allBodyBiomes.Add(cb.name, bb);
                //foreach (var s in bb.biomes)
                //    Log.Info("CelestialBody: " + cb.name + ",   Biome: " + s);
            }
        }

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            coroutine = MonitorGameTime(WAITTIME);
            StartCoroutine(coroutine);

            GameEvents.OnScienceChanged.Add(onScienceChanged);
            GameEvents.OnScienceRecieved.Add(onScienceReceived);
        }
        void onScienceChanged(float f, TransactionReasons tr)
        {
            //Log.Info("onScienceChanged, f: " + f + ",   reason: " + tr);
            // LoadCompletedExperiments();
        }

        void onScienceReceived(float f, ScienceSubject ss, ProtoVessel pv, bool b)
        {
            Log.Info("onScienceReceived");
            Log.Info("ScienceSubject:  id: " + ss.id + ",  title: " + ss.title + ",   subjectValue: " + ss.subjectValue +
                ",   scientificValue: " + ss.scientificValue + ",  scienceCap: " + ss.scienceCap);
            // LoadCompletedExperiments();
            AddCompletedScienceToList(ss.id, ss.title, ss.scientificValue, ss.scienceCap);
            DisplayTotalCompletedScience();

        }
        private void OnDestroy()
        {
            this.StopAllCoroutines();
            GameEvents.OnScienceChanged.Remove(onScienceChanged);
            GameEvents.OnScienceRecieved.Remove(onScienceReceived);

        }

        void AddCompletedScienceToList(string s, string title, float science, float scienceCap)
        {
            //Log.Info("ExperimentID: " + s);
            CompletedExperiment ce = new CompletedExperiment();

            string[] sp = s.Split(new char[] { '@' });
            ce.name = sp[0];
            int i = 0;
            for (int idx = 0; idx < FlightGlobals.Bodies.Count; idx++)
            //foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                CelestialBody cb = FlightGlobals.Bodies[idx];
                if (cb.name.Length <= sp[1].Length)
                    if (sp[1].Substring(0, cb.name.Length) == cb.name)
                    {
                        //Log.Info("body found: " + cb.name);
                        ce.body = cb.name;
                        i = cb.name.Length;

                        var esv = Enum.GetValues(typeof(ExperimentSituations));
                        
                        foreach (var es in esv)
                        {
                            //Log.Info("es: " + es + ",   es.Length: " + es.ToString().Length + ",   sp[1]: " + sp[1] + "   len: " + sp[1].Length + "    i: " + i);
                            if (i + es.ToString().Length <= sp[1].Length)
                            {
                                if (sp[1].Substring(i, es.ToString().Length) == es.ToString())
                                {
                                    ce.situation = (ExperimentSituations)es;
                                    i += es.ToString().Length;
                                    ce.location = sp[1].Substring(i);
                                    break;
                                }
                            }
                        }
                    }
            }
            Log.Info("Checking title for biome");
            if (title != null && title != "")
            {
                {
                    Log.Info("Title found: " + title + ",  body: " + ce.body);
                    BodyBiome bbv;
                    if (allBodyBiomes.TryGetValue(ce.body, out bbv))
                    {
                        //Log.Info("Body found, checking biomes");
                        ce.biome = ce.location; // If no biome found, then uses the location as the biome
                        for (int idx2 = 0; idx2 < bbv.biomes.Count(); idx2++)
                        //foreach (var b in bbv.biomes)
                        {
                            string b = bbv.biomes[idx2];
                            //Log.Info("Checking biome: " + b);
                            if (b.Length < title.Length)
                            {
                                if (title.Substring(title.Length - b.Length) == b)
                                {
                                    ce.biome = b;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            ce.science = science;
            ce.scienceCap = scienceCap;

            completedExps.Add(ce);
        }


        IEnumerator MonitorGameTime(float waitTime)
        {
            float w = 2 * waitTime;
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                if (HighLogic.LoadedScene == GameScenes.MAINMENU)
                {
                    lastGameTitle = "";
                    lastUT = 0;
                }
                else
                {
                    // for debugging
                    LoadCompletedExperiments();

                    //Log.Info("MonitorGameTime, lastGameTitle: " + lastGameTitle + ", currentGameTitle: " + HighLogic.CurrentGame.Title + ",  UT: " + Planetarium.GetUniversalTime().ToString() + ", " + lastUT.ToString());
                    if (lastGameTitle == HighLogic.CurrentGame.Title)
                    {
                        // Use abs here in case a game is loaded which is LATER than the current time
                        // Both cases are considered to be "reverted"
                        if (Math.Abs(Planetarium.GetUniversalTime() - lastUT) > w)
                            LoadCompletedExperiments();
                    }
                    else
                    {
                        lastGameTitle = HighLogic.CurrentGame.Title;
                        LoadCompletedExperiments();
                    }
                    lastUT = Planetarium.GetUniversalTime();
                }
            }
        }



        void LoadCompletedExperiments()
        {
            Log.Info("LoadCompletedExperiments");
            InitBodyBiomes();


            ConfigNode rnd = HighLogic.CurrentGame.config.GetNodes("SCENARIO").FirstOrDefault(n => n.GetValue("name") == "ResearchAndDevelopment");
            if (rnd != null)
            {
                ConfigNode[] nodes = rnd.GetNodes("Science");
                for (int idx = 0; idx < nodes.Count(); idx++)
               // foreach (var node in rnd.GetNodes("Science"))
                {
                    var node = nodes[idx];
                    var s = node.GetValue("id");
                    if (s != null)
                    {
                        var title = node.GetValue("title");
                        var science = float.Parse(node.GetValue("sci"));
                        var scienceCap = float.Parse(node.GetValue("cap"));
                        AddCompletedScienceToList(s, title, science, scienceCap);
                    }
                }

            }
            else
            {
                Log.Info("No science found");
            }
            DisplayTotalCompletedScience();
        }

        void DisplayTotalCompletedScience()
        {
            Log.Info("Total science items found: " + completedExps.Count());
            for (int idx = 0; idx < completedExps.Count; idx++)
            //foreach (var ce in completedExps)
            {
                CompletedExperiment ce = completedExps[idx];
                Log.Info("Science found: name: " + ce.name +
                    ", body: " + ce.body +
                    ", situation: " + ce.situation +
                    ", location: " + ce.location +
                    ", biome: " + ce.biome +
                    ", science: " + ce.science +
                    ", scienceCapacity: " + ce.scienceCap);

            }

        }
    }


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

        [KSPField]
        public float refreshInterval = 15;  // seconds

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

        [KSPField(guiActive = true, guiName = "Status:")]
        public string statusReason = "";

        [KSPField]
        public bool uniqueBodies = true;      // If true, then the experiment must have been completed in the planet's soi the vessel is in.

        [KSPField]
        public bool inCurrentSituation = true;

        [Persistent]
        ScienceExperiment experiment;

        [Persistent]
        double availableAtTime = -1; // used to know when modules should be enabled

        void SetStatusReason(string s)
        {
            if (statusReason != "")
                statusReason += ", ";
            statusReason += s;
            Fields["statusReason"].guiActive = (statusReason != "");
            if (s != "")
                Log.Info(s);
        }

        string[] biomeAr = null;

        const string MODTITLE = "Required Experiments";


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
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                return;
            ifiDebugWinRect = GUILayout.Window(123532, ifiDebugWinRect, DrawIFIDebugWin, "IFI Debug Window");
        }

        void DrawIFIDebugWin(int windowID)
        {
            return;
#if false
            GUILayout.BeginHorizontal(GUILayout.Width(400));
            GUILayout.Label("requiredExperimentID: " + requiredExperimentID);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("experiment.title: " + experiment.experimentTitle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
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
            if (requiredExperimentID == "" || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
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
            //foreach (var s in part.partInfo.moduleInfos)
            {
                var s = part.partInfo.moduleInfos[idx];
                Log.Info("ModuleInfo, name: " + s.moduleName);
                Log.Info("s: " + s.ToString());
                if (s.moduleName == MODTITLE)
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
                    moduleName = MODTITLE,
                    moduleDisplayName = MODTITLE
                };
                Log.Info("Adding info: " + MODTITLE + ",   " + info);
                part.partInfo.moduleInfos.Add(s);
            }
            //CheckExperiments();
            InvokeRepeating("Refresh", 0, refreshInterval);

            //GameEvents.OnScienceChanged.Add(onScienceChanged);
            //GameEvents.OnScienceRecieved.Add(onScienceReceived);            
        }

        void Refresh()
        {

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                return;
            CheckExperiments();
            CheckExperimentCompletions();
            if (Planetarium.GetUniversalTime() < availableAtTime)
            {
                EnableModules();
                availableAtTime = -1;
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
            yield return new WaitForSeconds(waitTime);
#else
        void CheckExperimentCompletions()
        {
#endif
            if (AllConditionsMet())
            {
                if (availableAtTime == -1)
                {
                    availableAtTime = Planetarium.GetUniversalTime() + delayBeforeAvailable;
                    //                EnableModules();
                }
            }
            else
                availableAtTime = -1;
        }

        /// <summary>
        /// Enable all the disabled modules
        /// </summary>
        void EnableModules()
        {
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
            if (vessel.landedAt != string.Empty)
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
            statusReason = "";

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
                        if (celst[c].biome == currentBiome)
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
                if (!inCurrentBiome)
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
            } else
            {
                return CheckBiomesAndSituations(celst);
            }

#if false
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
#endif

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

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                return;
            foreach (var modToDisable in initiallyDisabledModulesAr)
            {
                //Log.Info("InitiallyDisabled: " + modToDisable);
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
                return "";
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

        public string GetModuleTitle()
        {
            return MODTITLE;
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
