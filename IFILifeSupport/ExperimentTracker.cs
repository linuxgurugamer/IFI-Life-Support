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
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ExperimentTracker : MonoBehaviour
    {
        string lastGameTitle = "";
        double lastUT = 0;
        //private IEnumerator coroutine;
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
            //coroutine = MonitorGameTime(WAITTIME);
            StartCoroutine(MonitorGameTime(WAITTIME));

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
            Log.Info("ExperimentTracker.OnDestroy");
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
                if (!HighLogic.LoadedSceneIsGame)
                {
                    lastGameTitle = "";
                    lastUT = 0;
                }
                else
                {
                    if (HighLogic.CurrentGame != null)
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
}