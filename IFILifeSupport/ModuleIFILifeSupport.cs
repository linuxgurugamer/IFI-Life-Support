using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.Localization;
using UnityEngine;
using KSP;


namespace IFILifeSupport
{
    public class ModuleIFILifeSupport : BaseConverter, IModuleInfo
    {
        double IFITimer = 0;
        int IFITIM = 0;
        private bool Went_to_Main = false;

        // LS values are way too small for the normal resource converter to work with properly
        // so no recipe, it's handled inside this module
        // This is really here to avoid annoying warning messages from the BaseConverter
        //
        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            return null;
        }


        void Start()
        {
            Log.Info("ModuleIFILifeSupport.Start");
            IFITimer = Convert.ToInt32(Planetarium.GetUniversalTime());
            CancelInvoke();
            InvokeRepeating("calcLS", 1, 1);
        }

        void calcLS()
        {
            IFITIM++;
            if (!HighLogic.LoadedSceneIsEditor && (IFITIM > 3 || TimeWarp.CurrentRate > 800))
            {
                Life_Support_Update();
                IFITIM = 0;
            }

        }

        private double IFI_Get_Elasped_Time()
        {
            double CurrTime = Planetarium.GetUniversalTime();
            if (Time.timeSinceLevelLoad < 1f || !FlightGlobals.ready)
            {
                IFITimer = CurrTime;
            }

            double IHOLD = CurrTime - IFITimer;

            IFITimer = CurrTime;
            return IHOLD;
        }



        void Life_Support_Update()
        {
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

            double Elapsed_Time = IFI_Get_Elasped_Time();

            double[] inputResource = new double[10];
            double[] usedInputResource = new double[10];
            double[] outputResource = new double[10];

            double minPercent = 1;
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
                        minPercent = Math.Min(minPercent, usedInputResource[i] / inputResource[i]);
                        Log.Error("Input Resource: " + inputList[i].ResourceName + ",  amount requested: " + inputResource[i] + ", amount unavailable: " + (inputResource[i] - usedInputResource[i]));
                    }
                    Log.Error("minPercent: " + minPercent);
                    for (int i = 0; i < this.outputList.Count; i++)
                    {
                        outputResource[i] = -1 * outputList[i].Ratio * Elapsed_Time * minPercent;
                        this.part.RequestResource(outputList[i].ResourceName, outputResource[i]);
                        Log.Error("Output Resource (should be negative): " + outputList[i].ResourceName + ", amount: " + outputResource[i]);
                    }
                    if (minPercent < 1)
                    {
                        for (int i = 0; i < this.inputList.Count; i++)
                        {
                            double percentUsed = (usedInputResource[i] / inputResource[i]);
                            double percentToreturn = minPercent - percentUsed;
                            this.part.RequestResource(inputList[i].ResourceName, percentToreturn * inputResource[i]);
                            Log.Error("Returning Resource: " + inputList[i].ResourceName + ", amount: " + percentToreturn * inputResource[i]);
                        }
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
            for (int i = 0; i < this.inputList.Count; i++)
            {
                AddToInfo("     " + inputList[i].ResourceName + ": " + ResourceRatioToString(inputList[i].Ratio));
            }
            AddToInfo("Outputs:");
            for (int i = 0; i < this.outputList.Count; i++)
            {
                AddToInfo("     " + outputList[i].ResourceName + ": " + ResourceRatioToString(outputList[i].Ratio));
            }

            return info;
        }

        public string GetModuleTitle()
        {
            return MODULETITLE;
        }
        public override string GetModuleDisplayName()
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
