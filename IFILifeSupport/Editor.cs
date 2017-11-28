using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP;
using KSP.UI.Screens;
using KSP.UI;

using UnityEngine.SceneManagement;


namespace IFILifeSupport
{
#if DEBUG
    public class IFI_Improved : PartModule
    {
        void Start()
        {
            Log.Info("IFI_Improved");
        }
    }
    public class IFI_Advanced : PartModule
    {
        void Start()
        {
            Log.Info("IFI_Advanced");
        }
    }
#endif

    /// <summary>
    /// Need to keep it around always since going into settings and out doesn't seem to be a new scene
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class PartUpdater : MonoBehaviour
    {
        void Awake()
        {
            OnGameSettingsApplied();
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
            DontDestroyOnLoad(this);
        }

        void Destroy()
        {
            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);
        }

        /// <summary>
        /// Examine the loaded parts, changing the TechRequired as necessary to hide or show the parts in the tech tree
        /// Similar code is used in the editor, to hide or show the parts there
        /// </summary>
        void OnGameSettingsApplied()
        {
            if (PartLoader.LoadedPartsList == null)
                return;
            Log.Info("PartUpdater.OnGameSettingsApplied, LoadedPartsList.Count: " + PartLoader.LoadedPartsList.Count);
            for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
            {
                AvailablePart p = PartLoader.LoadedPartsList[i];

                ConfigNode node;
                if (!Editor.PartShouldBeHidden(p, out node))
                {                    
                    Log.Info("Hiding name: " + p.name + ",   " + p.TechRequired);
                    p.TechRequired = "none";
                }
                else
                {
                    if (node != null)
                    {
                        string techRequired = node.GetValue("TechRequired");

                        p.TechRequired = techRequired;
                        Log.Info("Restoring name: " + p.name + ",   " + p.TechRequired);
                    }
                }
            }
        }
    }


#if false
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class FuelSwitchMonitor : MonoBehaviour
    {
        IFILS1.LifeSupportLevel oldLevel = IFILS1.LifeSupportLevel.none;

        List<AvailablePart> GetPartsList()
        {
            List<AvailablePart> loadedParts = new List<AvailablePart>();
            loadedParts.AddRange(PartLoader.LoadedPartsList); // make a copy we can manipulate

            // these two parts are internal and just serve to mess up our lists and stuff
            AvailablePart kerbalEVA = null;
            AvailablePart flag = null;
            foreach (var part in loadedParts)
            {
                if (part.name == "kerbalEVA")
                    kerbalEVA = part;
                else if (part.name == "flag")
                    flag = part;
            }

            // still need to prevent errors with null refs when looking up these parts though
            if (kerbalEVA != null)
            {
                loadedParts.Remove(kerbalEVA);

            }
            if (flag != null)
            {
                loadedParts.Remove(flag);

            }
            return loadedParts;
        }

        void Start()
        {
            DontDestroyOnLoad(this);
        }
        void FixedUpdate()
        {
            if (oldLevel != HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level)
            {
                foreach (var p in GetPartsList()
                {
                    Log.Info("checking part: " + p.title);
                    ConfigNode partNode = p.partConfig;
                    if (partNode != null)
                    {
                        var nodes = partNode.GetNodes("IFIFuelSwitch");
                        if (nodes != null && nodes.Count() >0)
                        {

                        }

                    }              
                }
            }
        }
    }


    [KSPModule("IFIFuelSwitch")]
    public class IFIFuelSwitch : InterstellarFuelSwitch.InterstellarFuelSwitch
    {


        [KSPField]
        public string IFIResourceGui;

        [KSPField]
        public string IFIResourceNames;

        [KSPField]
        public string IFIInitialResourceAmounts;

        [KSPField]
        public string IFIResourceAmounts;

        string[] splitResourceGui;
        string[] splitResourceNames;
        string[] splitInitialResourceAmounts;
        string[] splitResourceAmounts;


        public override void OnStart(PartModule.StartState state)
        {
            Log.Info("IFIFuelSwitch.OnStart");
            InitializeData();
            base.OnStart(state);
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
            GameEvents.OnGameDatabaseLoaded.Add(onGameSettingsApplied);
        }

        void Destroy()
        {
            Log.Info("IFIFuelSwitch.Destroy");
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
            GameEvents.OnGameDatabaseLoaded.Remove(onGameSettingsApplied);
            // SceneManager.sceneLoaded -= CallbackOnLevelWasLoaded;
        }


        void onGameSettingsApplied()
        {
            Log.Info("IFIFuelSwitch.onGameSettingsApplied");

            InitializeData();
            base.ReInitializeData();
            base.OnAwake();
        }

        public override void OnAwake()
        {
            Log.Info("IFIFuelSwitch.OnAwake");
            try
            {
                if (configLoaded)
                {
                    InitializeData();
                    base.ReInitializeData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - OnAwake Error: " + e.Message);
                throw;
            }
            base.OnAwake();
        }

        public override void OnLoad(ConfigNode partNode)
        {
            Log.Info("IFIFuelSwitch.OnLoad");
            //if (!configLoaded)
            //    InitializeData();

            base.OnLoad(partNode);
            base.ReInitializeData();
        }


        void AddToString(ref string s, string r)
        {
            if (s == null)
                s = "";
            Log.Info("AddToString, s: " + s + "     r: " + r);
            if (s != "") s += ";";
            s += r;
        }


        void AddResource(string s)
        {
            if (part != null && part.partInfo != null)
                Log.Info(part.partInfo.name + ": AddResource: " + s);

            for (int i = 0; i < splitResourceNames.Count(); i++)
            {
                if (splitResourceNames[i] == s)
                {
                    AddToString(ref resourceGui, splitResourceGui[i]);
                    AddToString(ref resourceNames, splitResourceNames[i]);
                    AddToString(ref initialResourceAmounts, splitInitialResourceAmounts[i]);
                    AddToString(ref resourceAmounts, splitResourceAmounts[i]);
                    break;
                }
            }
        }

        void InitializeData()
        {
            Log.Info("IFIFuelSwitch.InitializeData");
            Log.Info("IFIResourceGui: " + IFIResourceGui);
            Log.Info("IFIResourceNames: " + IFIResourceNames);
            Log.Info("IFIInitialResourceAmounts: " + IFIInitialResourceAmounts);
            Log.Info("IFIResourceAmounts: " + IFIResourceAmounts);

            char[] splitString = { ';' };

            splitResourceGui = IFIResourceGui.Split(splitString);
            splitResourceNames = IFIResourceNames.Split(splitString);
            splitInitialResourceAmounts = IFIInitialResourceAmounts.Split(splitString);
            splitResourceAmounts = IFIResourceAmounts.Split(splitString);

            resourceGui = "";
            resourceNames = "";
            initialResourceAmounts = "";
            resourceAmounts = "";

            AddResource("LifeSupport");
            try
            {
                switch (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level)
                {
                    case IFILS1.LifeSupportLevel.basic:
                        break;
                    case IFILS1.LifeSupportLevel.improved:
                        AddResource("OrganicSlurry");
                        break;
                    case IFILS1.LifeSupportLevel.advanced:
                        AddResource("OrganicSlurry");
                        AddResource("Sludge");
                        AddResource("LifeSupport,OrganicSlurry");
                        break;
                }
            }
            catch
            {
                Log.Info("IFIFuelSwitch: Exception checking Level");
            }
            Log.Info("resourceGui: " + resourceGui);
            Log.Info("resourceNames: " + resourceNames);
            Log.Info("initialResourceAmounts: " + initialResourceAmounts);
            Log.Info("resourceAmounts: " + resourceAmounts);

        }

    }
#endif


    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class Editor : MonoBehaviour
    {
        private Dictionary<AvailablePart, PartInfo> partInfos = new Dictionary<AvailablePart, PartInfo>();
        private static UrlDir.UrlConfig[] configs = null;

        List<AvailablePart> GetPartsList()
        {
            List<AvailablePart> loadedParts = new List<AvailablePart>();
            loadedParts.AddRange(PartLoader.LoadedPartsList); // make a copy we can manipulate

            // these two parts are internal and just serve to mess up our lists and stuff
            AvailablePart kerbalEVA = null;
            AvailablePart flag = null;
            foreach (var part in loadedParts)
            {
                if (part.name == "kerbalEVA")
                    kerbalEVA = part;
                else if (part.name == "flag")
                    flag = part;
            }

            // still need to prevent errors with null refs when looking up these parts though
            if (kerbalEVA != null)
            {
                loadedParts.Remove(kerbalEVA);

            }
            if (flag != null)
            {
                loadedParts.Remove(flag);

            }
            return loadedParts;
        }

        /// <summary>
        /// Used both in the Editor and in the PartUpdater.
        /// The confignode is only used in the PartUpdater code
        /// </summary>
        /// <param name="part"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        static internal bool PartShouldBeHidden(AvailablePart part, out ConfigNode n)
        {
            n = null;
            if (part.partConfig == null)
                return true;
            ConfigNode partNode = part.partConfig;
            if (partNode != null)
                foreach (ConfigNode node in partNode.GetNodes())
                {
                    string s = node.GetValue("name");
                    if (s != null)
                    {
                        //    Log.Info("Part: " + part.name + ",   node name: " + s);
                        n = node;
                        if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level <= IFILS1.LifeSupportLevel.basic && s == "IFI_Improved") // improved
                            return false;
                        if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level < IFILS1.LifeSupportLevel.advanced && s == "IFI_Advanced") // advanced
                            return false;
                        if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level < IFILS1.LifeSupportLevel.extreme && s == "IFI_Extreme") // Extreme
                            return false;
                    }
                }
            n = null;
            return true;
#if false
            foreach (var r in part.resourceInfos)
            {
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level == IFILS1.LifeSupportLevel.improved && r.resourceName == "Sludge")
                    return false;
            }
            return true;
#endif
        }

        void DefineFilters()
        {
            Log.Info("DefineFilters");
            if (configs == null)
                configs = GameDatabase.Instance.GetConfigs("PART");
            ConfigNode n;
            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Unpurchased Filter", (part => PartShouldBeHidden(part, out n))));

            EditorPartList.Instance.Refresh();
        }
        void InitData()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level == IFILS1.LifeSupportLevel.extreme)
                return;
            if (configs == null)
                configs = GameDatabase.Instance.GetConfigs("PART");
            List<AvailablePart> loadedParts = GetPartsList();
        }
        public void Start()
        {
            InitData();
            DefineFilters();
        }


    }
}
