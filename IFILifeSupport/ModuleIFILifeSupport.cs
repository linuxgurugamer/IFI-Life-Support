using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.Localization;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{
    public class ModuleIFILifeSupport :  PartModule , IModuleInfo
    {
        //double IFITimer = 0;
        public List<ResourceRatio> inputList;
        public List<ResourceRatio> outputList;

        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public string ConverterName = "converter";        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public bool requiresAllInputs = true;
        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public bool isAlwaysActive = false;
        [KSPField(isPersistant = false, guiActive = false, guiActiveUnfocused = false)]
        public bool AutoShutdown = true;

        public bool IsActivated = false;

        const string INPUT = "INPUT_RESOURCE";
        const string OUTPUT = "OUTPUT_RESOURCE";

        public bool ModuleIsActive()
        {
            IsActivated = (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER);
            return IsActivated;
        }
        public override void OnLoad(ConfigNode node)
        {
            
            inputList = new List<ResourceRatio>();
            outputList = new List<ResourceRatio>();

            if (node.HasNode(INPUT))
            {
                foreach (ConfigNode dataNode in node.GetNodes(INPUT))
                {
                    ResourceRatio rr = new ResourceRatio();
                    rr.Load(dataNode);
                    inputList.Add(rr);
                }
            }

            if (node.HasNode(OUTPUT))
            {
                foreach (ConfigNode dataNode in node.GetNodes(OUTPUT))
                {
                    ResourceRatio rr = new ResourceRatio();
                    rr.Load(dataNode);
                    outputList.Add(rr);
                }
            }
        }


        void Start()
        {
            Log.Info("ModuleIFILifeSupport.Start");
            //IFITimer = Convert.ToInt32(Planetarium.GetUniversalTime());
            // CancelInvoke();
            lastUpdateTime = Planetarium.GetUniversalTime();
            InvokeRepeating("calcLS", 0, HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().refreshInterval);
        }

        double lastUpdateTime;

#if true
        void calcLS()
        {
            if (!HighLogic.LoadedSceneIsEditor && 
                (Planetarium.GetUniversalTime()-lastUpdateTime >= HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().RefreshInterval || TimeWarp.CurrentRate > 800))
            {
                Life_Support_Update();
                lastUpdateTime = Planetarium.GetUniversalTime();
            }

        }
#endif
#if true
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

#if true
        void Life_Support_Update()
        {
#if false
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                Log.Info("Life_Support_Update, Went_To_Main");
                Went_to_Main = true;
                return; // Don't Run at main menu and reset IFITimer
            }
            if (Went_to_Main)
            {
                IFITimer = Convert.ToInt32(Planetarium.GetUniversalTime());
                Went_to_Main = false;
            }
#endif
            double Elapsed_Time = IFI_Get_Elasped_Time();

            
            double[] inputResource = new double[10];        // How much is needed for ElapsedTime
            double[] usedInputResource = new double[10];    // How much is available, never more than inputResource
            double[] outputResource = new double[10];       

            double[] minPercent = new double[10];
            Log.Info("Life_Support_Update, Elapsed_Time: " + Elapsed_Time + ",   lastUpdateTime: " + lastUpdateTime);
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {

                if (this.ModuleIsActive())
                {
                    Log.Error("Life_Support_Update, Elapsed_Time: " + Elapsed_Time + ",   lastUpdateTime: " + lastUpdateTime);
                    Log.Error("ModuleIFILifeSupport, ModuleIsActive, vessel: " + this.vessel.name + ",  part: " + this.part.partInfo.title);
                    for (int i = 0; i < this.inputList.Count; i++)
                    {
                        inputResource[i] = inputList[i].Ratio * Elapsed_Time;
                        usedInputResource[i] = this.part.RequestResource(inputList[i].ResourceName, inputResource[i]);
                        minPercent[i] = Math.Min(1, usedInputResource[i] / inputResource[i]);
                        Log.Error("Input Resource: " + inputList[i].ResourceName + ",  amount requested: " + inputResource[i] + ", amount unavailable: " + (inputResource[i] - usedInputResource[i]));
                        Log.Error("minPercent: " + minPercent[i]);
                    }
                   
                    for (int i = 0; i < this.outputList.Count; i++)
                    {
                        outputResource[i] = -1 * outputList[i].Ratio * Elapsed_Time * minPercent[i];
                        this.part.RequestResource(outputList[i].ResourceName, outputResource[i]);
                        Log.Error("Output Resource (should be negative): " + outputList[i].ResourceName + ", amount: " + outputResource[i]);
                    }


                    for (int i = 0; i < this.inputList.Count; i++)
                    {
                        if (minPercent[i] < 1)
                        {
                            double percentUsed = (usedInputResource[i] / inputResource[i]);
                            double percentToreturn = minPercent[i] - percentUsed;
                            this.part.RequestResource(inputList[i].ResourceName, percentToreturn * inputResource[i]);
                            Log.Error("Returning Resource: " + inputList[i].ResourceName + ", amount: " + percentToreturn * inputResource[i]);
                        }
                    }

                }

            }
        }
#endif

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

        public string GetModuleTitle()
        {
            return MODULETITLE;
        }
        public  override string GetModuleDisplayName()
        {
            return Localizer.Format(MODULETITLE);
        }


        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }
        public string GetPrimaryField()
        {
            return "Missing LS";
        }
#endregion
    }
}
