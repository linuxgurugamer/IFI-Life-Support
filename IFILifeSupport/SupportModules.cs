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

    public class IFI_Resources : PartModule
    {

        [KSPField(isPersistant = true, guiName = Constants.LIFESUPPORT, guiActiveEditor = false, guiActive = false), UI_FloatRange(minValue = 0f, maxValue = 10, stepIncrement = 1)]
        public float ls_amount = 0;
        [KSPField(guiActive = false)]
        public float ls_maxAmount = 0;


        [KSPField(isPersistant = true, guiName = Constants.SLURRY, guiActiveEditor = false, guiActive = false), UI_FloatRange(minValue = 0f, maxValue = 10, stepIncrement = 1)]
        public float slurry_amount = 0;
        [KSPField(guiActive = false)]
        public float slurry_maxAmount = 0;


        [KSPField(isPersistant = true, guiName = Constants.SLUDGE, guiActiveEditor = false, guiActive = false), UI_FloatRange(minValue = 0f, maxValue = 10, stepIncrement = 1)]
        public float sludge_amount = 0;
        [KSPField(guiActive = false)]
        public float sludge_maxAmount = 0;


        public const int SECS_PER_DAY = 21600;


        internal string info = "";
#if false
        public override string GetInfo()
        {
            string info2 = info;
            foreach (PartResource partResource in part.Resources)
            {
                if (partResource.resourceName == Constants.LIFESUPPORT)
                    info2 += "\nLife Support: " + partResource.amount  + " / " + partResource.maxAmount;
                if (partResource.resourceName == Constants.SLURRY)
                    info2 += "\nSlurry: " + partResource.amount  + " / " + partResource.maxAmount ;
                if (partResource.resourceName == Constants.SLUDGE)
                    info2 += "\nSludge: " + partResource.amount  + " / " + partResource.maxAmount ;
            }
            return info2;
        }
#endif
        bool initted = false;
        void LateUpdate()
        {
            if (!initted)
                Start();
        }

        void OnDestroy()
        {
            if (initted && HighLogic.LoadedSceneIsEditor)
            {
                Log.Info("IFI_Resources.OnDestory");
                GameEvents.onEditorShipModified.Remove(ShipModified);
                initted = false;
            }
        }
        public void Start()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(ShipModified);
            }

            Log.Info("IFI_Resources.Start, amount: " + ls_amount);

            Log.Info("IFI_Resources.Start, amount: " + ls_amount + ", Constants.LIFESUPPORT: " + Constants.LIFESUPPORT);
            foreach (PartResource partResource in part.Resources)
            {
                Log.Info("partResource.resourceName: " + partResource.resourceName);
                switch (partResource.resourceName)
                {
                    case Constants.LIFESUPPORT:
                        if (ls_maxAmount > 0)
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                partResource.amount = this.ls_amount * SECS_PER_DAY;
                                Fields["ls_amount"].guiActive = true;
                            }
                            partResource.maxAmount = this.ls_maxAmount * SECS_PER_DAY;

                            ((UI_FloatRange)this.Fields["ls_amount"].uiControlEditor).minValue = 0;
                            ((UI_FloatRange)this.Fields["ls_amount"].uiControlEditor).maxValue = this.ls_maxAmount;
                            ((UI_FloatRange)this.Fields["ls_amount"].uiControlEditor).stepIncrement = 0.1f;
                            Fields["ls_amount"].guiActiveEditor = true;
     
                        }
                        break;

                    case Constants.SLURRY:
                        if (slurry_maxAmount > 0)
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                partResource.amount = this.slurry_amount * SECS_PER_DAY;
                                Fields["slurry_amount"].guiActive = true;
                            }
                            partResource.maxAmount = this.slurry_maxAmount * SECS_PER_DAY;

                            ((UI_FloatRange)this.Fields["slurry_amount"].uiControlEditor).minValue = 0;
                            ((UI_FloatRange)this.Fields["slurry_amount"].uiControlEditor).maxValue = this.slurry_maxAmount;
                            ((UI_FloatRange)this.Fields["slurry_amount"].uiControlEditor).stepIncrement = 0.1f;
                            Fields["slurry_amount"].guiActiveEditor = true;
                   
                        }
                        break;

                    case Constants.SLUDGE:
                        if (sludge_maxAmount > 0)
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                partResource.amount = this.sludge_amount * SECS_PER_DAY;
                                Fields["sludge_amount"].guiActive = true;
                            }
                            partResource.maxAmount = this.sludge_maxAmount * SECS_PER_DAY;

                            ((UI_FloatRange)this.Fields["sludge_amount"].uiControlEditor).minValue = 0;
                            ((UI_FloatRange)this.Fields["sludge_amount"].uiControlEditor).maxValue = this.sludge_maxAmount;
                            ((UI_FloatRange)this.Fields["sludge_amount"].uiControlEditor).stepIncrement = 0.1f;
                            Fields["sludge_amount"].guiActiveEditor = true;
                   
                        }
                        break;

                }
            }
            
            initted = true;
        }

        void ShipModified(ShipConstruct sc)
        {
            Log.Info("IFI_Resources.ShipModified, sc: " + sc);
            foreach (PartResource partResource in part.Resources)
            {
                Log.Info("partResource.resourceName: " + partResource.resourceName);

                switch (partResource.resourceName)
                {
                    case Constants.LIFESUPPORT:
                        if (ls_maxAmount > 0)
                        {
                            partResource.amount = this.ls_amount * SECS_PER_DAY;
                            partResource.maxAmount = this.ls_maxAmount * SECS_PER_DAY;
                        }
                        break;

                    case Constants.SLURRY:
                        if (slurry_maxAmount > 0)
                        {
                            partResource.amount = this.slurry_amount * SECS_PER_DAY;
                            partResource.maxAmount = this.slurry_maxAmount * SECS_PER_DAY;
                        }
                        break;

                    case Constants.SLUDGE:
                        if (sludge_maxAmount > 0)
                        {
                            partResource.amount = this.sludge_amount * SECS_PER_DAY;
                            partResource.maxAmount = this.sludge_maxAmount * SECS_PER_DAY;
                        }
                        break;

                }
            }
        }
    }

    public class IFI_Basic : IFI_Resources
    {
        new void Start()
        {
            Log.Info("IFI_Basic");
            info = "IFI_Basic";
        }
    }
    public class IFI_Improved : IFI_Resources
    {
        new void Start()
        {
            Log.Info("IFI_Improved");
            info = "IFI_Improved";
        }
    }
    public class IFI_Advanced : IFI_Resources
    {
        new void Start()
        {
            info = "IFI_Advanced";
            Log.Info("IFI_Advanced");
        }
    }
    public class IFI_Extreme : IFI_Resources
    {
        new void Start()
        {
            info = "IFI_Extreme";
            Log.Info("IFI_Extreme");
        }
    }

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

}
