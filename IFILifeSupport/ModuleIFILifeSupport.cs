using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.Localization;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{

    public class ModuleIFIGreenhousePanel : ModuleAnimateGeneric
    {
        internal bool Active { get { return !animSwitch; } }

        public void Start()
        {
            if (HighLogic.CurrentGame == null || !HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            anim[animationName].speed = 0f;
        }

        public void FixedUpdate()
        {
            if (RegisterToolbar.GamePaused /*("ModuleIFILifeSupport.FixedUpdate") */ )
                return;
            //
            // Due to a bug in one of the models, this code is needed to stop the animation when it is complete
            //
            //Log.Info("ModuleIFIGreenhousePanel  Active: " + Active + ", anim[animationName].speed: " + anim[animationName].speed + ", anim[animationName].normalizedTime: " + anim[animationName].normalizedTime);
            if (anim[animationName].speed != 0)
            {
                if (anim[animationName].normalizedTime > 1)
                {
                    anim[animationName].speed = 0f;
                    anim[animationName].normalizedTime = 1f;
                }
                if (anim[animationName].normalizedTime < 0)
                {
                    anim[animationName].speed = 0f;
                    anim[animationName].normalizedTime = 0f;
                }
            }
            else
            {
                anim[animationName].normalizedTime = Active ? 1 : 0;
            }
        }

    }

    public class LocalResourceRatio
    {
        public ResourceRatio rr;
        public int resourceId;
        public float uvMultipler;
    }
    public class ModuleIFILifeSupport : PartModule, IModuleInfo
    {
        public List<LocalResourceRatio> inputList;
        public List<ResourceRatio> outputList;

        ModuleIFIGreenhousePanel moduleIFIgreenhousePanel;

        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public string ConverterName = "converter";        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public bool requiresAllInputs = true;
        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public bool isAlwaysActive = false;

        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public bool AutoShutdown = true;

        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public string StartActionName = "Turn UV Lights ON";

        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public string StopActionName = "Turn UV Lights OFF";

        [KSPField(isPersistant = true)]
        public bool UVLightsActivated = false;

        [KSPField]
        public bool checkForOxygen = false;

        //
        // animation stuff here
        //

        [KSPField]
        public bool Reversable = true;

        [KSPField]
        public float RampSpeed = 1;

        [KSPField]
        public bool Continuous = false;

        [KSPField]
        public string AnimationName = null;

        [KSPField]
        public string AnimationLights = null;

        [KSPField]
        public float AnimationStartOffset = 0f;

        public bool isPlaying = false;

        Animation BioAnimate = null;
        Animation Lights = null;

        ModuleDeployableSolarPanel solarPanel;
        ModuleDeployablePart.DeployState lastDeployState;

        void UpdateEventNames()
        {
            if (solarPanel != null)
            {
                Events["ToggleUVLightsAction"].guiActive = (solarPanel.deployState == ModuleDeployablePart.DeployState.EXTENDED);
                lastDeployState = solarPanel.deployState;
            }
            switch (UVLightsActivated)
            {
                case true:
                    Events["ToggleUVLightsAction"].guiName = StopActionName;
                    break;
                case false:
                    Events["ToggleUVLightsAction"].guiName = StartActionName;
                    break;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Toggle UV Lights")]
        public void ToggleUVLightsAction()
        {
            UVLightsActivated = !UVLightsActivated;
            UpdateEventNames();
        }

        [KSPAction("Toggle UV Lights")]
        public void ToggleUVLightsAction(KSPActionParam param)
        {
            ToggleUVLightsAction();
        }

        [KSPField(isPersistant = true)]
        public bool IsActivated = false;


        public const string INPUT = "INPUT_RESOURCE";
        public const string OUTPUT = "OUTPUT_RESOURCE";

        public bool ModuleIsActive()
        {
            IsActivated = UVLightsActivated ||
                (moduleIFIgreenhousePanel != null && moduleIFIgreenhousePanel.Active);

            return IsActivated;
        }

        public override void OnLoad(ConfigNode node)
        {
            if (Log == null)
                return;

            inputList = new List<LocalResourceRatio>();
            outputList = new List<ResourceRatio>();

            var p = part.partInfo.partConfig.GetNodes("MODULE");
            foreach (var p1 in p)
            {
                var name = p1.GetValue("name");
                if (name == "ModuleIFILifeSupport")
                {
                    if (p1.HasNode(INPUT))
                    {
                        foreach (ConfigNode dataNode in p1.GetNodes(INPUT))
                        {
                            LocalResourceRatio lrr = new LocalResourceRatio();

                            lrr.rr.Load(dataNode);
                            lrr.resourceId = PartResourceLibrary.Instance.GetDefinition(lrr.rr.ResourceName).id;
                            inputList.Add(lrr);
                        }
                    }

                    if (p1.HasNode(OUTPUT))
                    {
                        foreach (ConfigNode dataNode in p1.GetNodes(OUTPUT))
                        {
                            ResourceRatio rr = new ResourceRatio();
                            rr.Load(dataNode);
                            outputList.Add(rr);
                        }
                    }
                    break;
                }
            }
        }


        void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            if (Log != null)
                Log.Info("ModuleIFILifeSupport.Start");

            solarPanel = part.FindModuleImplementing<ModuleDeployableSolarPanel>();

            moduleIFIgreenhousePanel = part.GetComponent<ModuleIFIGreenhousePanel>();

            lastUpdateTime = Planetarium.GetUniversalTime();
            if (AnimationName != null)
            {
                Log.Info("ModuleIFILifeSupport.Start, Part: " + this.part.partInfo.title + ", AnimationName: [" + AnimationName + "]");

                var ma = part.FindModelAnimators(AnimationName);
                if (ma != null && ma.Length > 0)
                    BioAnimate = ma[0];

                UpdateEventNames();

                if (BioAnimate != null)
                {
                    Log.Info("ModuleIFILifeSupport.Start, Part: " + this.part.partInfo.title + ", " + AnimationName + " found, stopping animation");
                    BioAnimate[AnimationName].speed = 0;
                    lastTime = BioAnimate[AnimationName].time = 0;
                }
            }
            if (AnimationLights != null)
            {
                Log.Info("ModuleIFILifeSupport.Start, Part: " + this.part.partInfo.title + ", AnimationLights: [" + AnimationLights + "]");

                var ma = part.FindModelAnimators(AnimationLights);
                if (ma != null && ma.Length > 0)
                    Lights = ma[0];

                UpdateEventNames();

                if (Lights != null)
                {
                    Lights[AnimationLights].speed = 0;
                }
            }
            
        }

        double lastUpdateTime;

        float curSpeed, endSpeed, lastTime = 0;
        public void FixedUpdate()
        {
            if (BioAnimate != null)
            {
                if (IsActivated && !isPlaying)
                {
                    isPlaying = true;
                    try
                    {
                        curSpeed = BioAnimate[AnimationName].speed;
                        endSpeed = 1;
                        if (Continuous)
                            BioAnimate[AnimationName].wrapMode = WrapMode.Loop;
                        else
                            BioAnimate[AnimationName].wrapMode = WrapMode.ClampForever;
                        BioAnimate[AnimationName].time = lastTime;
                        BioAnimate.Play();

                        if (Lights != null)
                        {
                            Lights[AnimationLights].wrapMode = WrapMode.ClampForever;
                            Lights[AnimationLights].speed = 1;
                            Lights.Play();
                        }

                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        Log.Error("ModuleIFILifeSupport.FixedUpdate IndexOutOfRangeException: " + AnimationName);
                    }
                }
                if (!IsActivated && isPlaying)
                {
                    Log.Info("ModuleIFILifeSupport, part: " + part.partInfo.title + " Artificial Lights Deactivated");
                    isPlaying = !isPlaying;
                    try
                    {
                        curSpeed = BioAnimate[AnimationName].speed;

                        if (Reversable)
                            endSpeed = -1;
                        else
                            endSpeed = 0;

                        if (Lights != null)
                        {
                            Lights[AnimationLights].speed = -1;
                            Lights[AnimationLights].time = 1;

                        }

                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        Log.Error("ModuleIFILifeSupport.FixedUpdate IndexOutOfRangeException: " + AnimationName);
                    }
                }

                Log.Info("ModuleIFILifeSupport.FixedUpdate, part: " + part.partInfo.title + " BioAnimate[AnimationName].time: " + BioAnimate[AnimationName].time + ", BioAnimate[AnimationName].speed: " + BioAnimate[AnimationName].speed);

                if (endSpeed == 1 && curSpeed < 1)
                {
                    curSpeed += RampSpeed;
                    BioAnimate[AnimationName].speed = curSpeed;
                    
                }
                if (endSpeed <= 0 && curSpeed > endSpeed)
                {
                    curSpeed -= RampSpeed;
                    BioAnimate[AnimationName].speed = curSpeed;
                    lastTime = BioAnimate[AnimationName].time;
                    if (curSpeed <= endSpeed)
                    {
                        BioAnimate[AnimationName].wrapMode = WrapMode.ClampForever;
                        BioAnimate.Stop();
                        if (Lights != null)
                            Lights.Stop();
                    }
                }
            }
            if (solarPanel != null && lastDeployState != solarPanel.deployState)
                UpdateEventNames();
        }


#region info
        const string MODULETITLE = "IFI Life Support";
        string info = "";
        void AddToInfo(string s)
        {
            if (info != "")
            {
                //if (s.Substring(0, 1) == " ")
                //    info += ",";
                info += "\n";
            }
            info += s;
        }


        string ResourceRatioToString(double Ratio)
        {
            StringBuilder stringBuilder = StringBuilderCache.Acquire(256);
            if (Ratio < 0.0001)
            {
                stringBuilder.AppendFormat("{0:0.00}/day", Ratio * (double)KSPUtil.dateTimeFormatter.Day);
            }
            else if (Ratio < 0.01)
            {
                stringBuilder.AppendFormat("{0:0.00}/hr", Ratio * (double)KSPUtil.dateTimeFormatter.Hour);
            }
            else
            {
                stringBuilder.AppendFormat("{0:0.00}/sec", Ratio);
            }
            return stringBuilder.ToString();
        }

        public override string GetInfo()
        {
            info = "";

            AddToInfo("Inputs:");
            if (inputList != null)
                for (int i = 0; i < this.inputList.Count; i++)
                {
                    AddToInfo("     " + inputList[i].rr.ResourceName + ": " + ResourceRatioToString(inputList[i].rr.Ratio));
                }
            AddToInfo("Outputs:");
            if (outputList != null)
                for (int i = 0; i < this.outputList.Count; i++)
                {
                    AddToInfo("     " + outputList[i].ResourceName + ": " + ResourceRatioToString(outputList[i].Ratio) + ", DumpExcess : " + outputList[i].DumpExcess);
                }

            return info;
        }

        public string GetModuleTitle() { return MODULETITLE; }
        public override string GetModuleDisplayName() { return Localizer.Format(MODULETITLE); }

        public Callback<Rect> GetDrawModulePanelCallback() { return null; }

        public string GetPrimaryField() { return "Missing LS"; }
#endregion
    }
}
