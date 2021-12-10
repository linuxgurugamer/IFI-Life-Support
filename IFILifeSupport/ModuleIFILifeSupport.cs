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
        //[KSPField]
        //public bool FixAnim = false;
        internal bool Active { get { return !animSwitch; } }

        public void Start()
        {
            if (HighLogic.CurrentGame == null)
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

    public class ModuleIFILifeSupport : PartModule, IModuleInfo
    {
        //double IFITimer = 0;
        public List<ResourceRatio> inputList;
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
        public float AnimationStartOffset = 0f;

        public bool isPlaying = false;

        Animation BioAnimate = null;

        void UpdateEventNames()
        {
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

            inputList = new List<ResourceRatio>();
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
                            ResourceRatio rr = new ResourceRatio();
                            rr.Load(dataNode);
                            inputList.Add(rr);
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
            if (Log != null)
                Log.Info("ModuleIFILifeSupport.Start");
            moduleIFIgreenhousePanel = part.GetComponent<ModuleIFIGreenhousePanel>();

            lastUpdateTime = Planetarium.GetUniversalTime();
            if (AnimationName != null)
                Log.Info("ModuleIFILifeSupport.Start, Part: " + this.part.partInfo.title + ", AnimationName: [" + AnimationName + "]");

            var ma = part.FindModelAnimators(AnimationName);
            if (ma != null && ma.Length > 0)
                BioAnimate = ma[0];

            UpdateEventNames();

            if (BioAnimate != null)
            {
                Log.Info("ModuleIFILifeSupport.Start, Part: " + this.part.partInfo.title + ", " + AnimationName + " found, stopping animation");
                BioAnimate.Stop();
                BioAnimate[AnimationName].speed = 0;
                lastTime = BioAnimate[AnimationName].time = AnimationStartOffset;
            }
        }

        double lastUpdateTime;

#if false
        void RunConverter()
        {
            if (!HighLogic.LoadedSceneIsEditor &&
                (Planetarium.GetUniversalTime() - lastUpdateTime >= HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().RefreshInterval || TimeWarp.CurrentRate > 800))
            {
                Life_Support_Converter_Update();
                lastUpdateTime = Planetarium.GetUniversalTime();
            }

        }
#endif
#if false
        private double IFI_Get_Elasped_Time()
        {
            double CurrTime = Planetarium.GetUniversalTime();
            if (Time.timeSinceLevelLoad < 1f || !FlightGlobals.ready)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
            }

            double IHOLD = CurrTime - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            return IHOLD;
        }
#endif

#if false
        void Life_Support_Converter_Update()
        {
            double Elapsed_Time = IFI_Get_Elasped_Time();


            double[] inputResource = new double[10];        // How much is needed for ElapsedTime
            double[] usedInputResource = new double[10];    // How much is available, never more than inputResource
            double[] outputResource = new double[10];

            double[] minPercent = new double[10];
            Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update, Elapsed_Time: " + Elapsed_Time + ",   lastUpdateTime: " + lastUpdateTime);
            {

                if (this.ModuleIsActive())
                {
                    Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update, Elapsed_Time: " + Elapsed_Time + ",   lastUpdateTime: " + lastUpdateTime);
                    Log.Info("ModuleIFILifeSupport.ModuleIsActive, vessel: " + this.vessel.name + ",  part: " + this.part.partInfo.title);
                    //Log.Info("ModuleIFILifeSupport, inputList.Count: " + inputList.Count);
                    double percentResAvail = 1;
                    for (int i = 0; i < this.inputList.Count; i++)
                    {
                        inputResource[i] = inputList[i].Ratio * Elapsed_Time;
                        usedInputResource[i] = this.part.RequestResource(inputList[i].ResourceName, inputResource[i]);
                        Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update 1, RequestedResource, part: " + part.partInfo.title + ", usedInputResource[" + i + "]: " + usedInputResource[i]);
                        minPercent[i] = Math.Min(1, usedInputResource[i] / inputResource[i]);
                        percentResAvail = Math.Min(percentResAvail, minPercent[i]);
                        Log.Info("ModuleIFILifeSupport.Input Resource: " + inputList[i].ResourceName + ",  amount requested: " + inputResource[i] + ", amount unavailable: " + (inputResource[i] - usedInputResource[i]));
                        Log.Info("ModuleIFILifeSupport.minPercent: " + minPercent[i]);
                    }
                    for (int i = 0; i < this.inputList.Count; i++)
                    {
                        if (percentResAvail < 1)
                        {
                            double percentUsed = (usedInputResource[i] / inputResource[i]);
                            double percentToreturn = percentResAvail - percentUsed;
                            this.part.RequestResource(inputList[i].ResourceName, percentToreturn * inputResource[i]);
                            Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update 2, RequestedResource, part: " + part.partInfo.title + ", percentToreturn * inputResource[" + i + "]: " + percentToreturn * inputResource[i]);
                            Log.Info("ModuleIFILifeSupport.Returning Resource: " + inputList[i].ResourceName + ", amount: " + percentToreturn * inputResource[i]);
                        }
                    }

                    Log.Info("ModuleIFILifeSupport, percentResAvail: " + percentResAvail + ", outputList.Count: " + outputList.Count);

                    for (int i = 0; i < this.outputList.Count; i++)
                    {
                        outputResource[i] = -1 * outputList[i].Ratio * Elapsed_Time * percentResAvail;
                        this.part.RequestResource(outputList[i].ResourceName, outputResource[i]);
                        Log.Info("ModuleIFILifeSupport.Life_Support_Converter_Update 3, RequestedResource, part: " + part.partInfo.title + ", outputResource[" + i + "]: " + outputResource[i]);
                        Log.Info("ModuleIFILifeSupport.Output Resource (should be negative): " + outputList[i].ResourceName + ", amount: " + outputResource[i]);
                    }


                }

            }
        }
#endif
        float curSpeed, endSpeed, lastTime = 0;
        public void FixedUpdate()
        {
            //Log.Info("ModuleIFILifeSupport.FixedUpdate, IsActivated: " + IsActivated + ",   isPlaying: " + isPlaying);
            if (BioAnimate != null)
            {
                if (IsActivated && !isPlaying)
                {
                    isPlaying = true;
                    try
                    {
                        Log.Info("ModuleIFILifeSupport, part: " + part.partInfo.title + " Artificial Lights Activated");

                        curSpeed = BioAnimate[AnimationName].speed;
                        endSpeed = 1;
                        if (Continuous)
                            BioAnimate[AnimationName].wrapMode = WrapMode.Loop;
                        else
                            BioAnimate[AnimationName].wrapMode = WrapMode.ClampForever;
                        BioAnimate[AnimationName].time = lastTime;
                        BioAnimate.Play();
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

                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        Log.Error("ModuleIFILifeSupport.FixedUpdate IndexOutOfRangeException: " + AnimationName);
                    }
                }
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
                    }
                }
            }
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
                    AddToInfo("     " + inputList[i].ResourceName + ": " + ResourceRatioToString(inputList[i].Ratio));
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
