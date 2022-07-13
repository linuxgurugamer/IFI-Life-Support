using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SpaceTuxUtility;
using static IFILifeSupport.RegisterToolbar;


namespace IFILifeSupport
{
    public class Settings
    {
        //public const float WINDOW_WIDTH = 500;
        //public const float WINDOW_HEIGHT = 100;

        public const float WINDOW_WIDTH_DEFAULT = 100;
        public const float WINDOW_HEIGHT = 200;
        public const float EDITOR_WIN_HEIGHT = 100;

        public static Settings Instance;

        internal string rootPath;
        internal GUIStyle resizeButton;
        internal GUIStyle myStyle;
        internal Texture2D styleOff;
        internal Texture2D styleOn;

        // Following are saved in a file


        public enum ActiveWin { FlightStatusWin = 0, FlightStatusWinWide = 1, SpaceCenter = 2, SpaceCenterWide = 3, InfoWin = 4, EditorInfoWin = 5, CalculatorWin = 6 };
        public static int activeWin;

        public int NumWins = Enum.GetValues(typeof(ActiveWin)).Length;
        public static Rect[] winPos = new Rect[Enum.GetValues(typeof(ActiveWin)).Length];

        internal static readonly string CFG_PATH = "/GameData/IFILS/PluginData/";
        static readonly string CFG_FILE = CFG_PATH + "IFILS.cfg";

        internal static readonly string DISPLAYINFO_NODENAME = "IFILS";
        internal static readonly string CONTRACT_NODENAME = "Data";

        public void SetActiveWin(ActiveWin win)
        {
            activeWin = (int)win;
        }

        public Settings(string rpath)
        {
            Instance = this;
            rootPath = rpath;
            for (int i = 0; i <NumWins; i++)
            {
                winPos[i] = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
            }
            winPos[(int)ActiveWin.InfoWin] = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, 400, 300);
            //InitWinPos();
            Settings.Instance.LoadData();
        }

        public void InitWinPos()
        {
            winPos[(int)ActiveWin.FlightStatusWin] = new Rect(winPos[(int)ActiveWin.FlightStatusWin].x, winPos[(int)ActiveWin.FlightStatusWin].y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
            winPos[(int)ActiveWin.FlightStatusWinWide] = new Rect(winPos[(int)ActiveWin.FlightStatusWinWide].x, winPos[(int)ActiveWin.FlightStatusWinWide].y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
            winPos[(int)ActiveWin.SpaceCenter] = new Rect(winPos[(int)ActiveWin.SpaceCenter].x, winPos[(int)ActiveWin.SpaceCenter].y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
            winPos[(int)ActiveWin.SpaceCenterWide] = new Rect(winPos[(int)ActiveWin.SpaceCenterWide].x, winPos[(int)ActiveWin.SpaceCenterWide].y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);

            Settings.winPos[(int)Settings.ActiveWin.EditorInfoWin] = new Rect(Settings.winPos[(int)Settings.ActiveWin.EditorInfoWin].x, Settings.winPos[(int)Settings.ActiveWin.EditorInfoWin].y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
            Settings.winPos[(int)Settings.ActiveWin.CalculatorWin] = new Rect(Settings.winPos[(int)Settings.ActiveWin.CalculatorWin].x, Settings.winPos[(int)Settings.ActiveWin.CalculatorWin].y, 800, 400);

        }
        public void SaveData()
        {
            string fullPath = rootPath + CFG_FILE;
            var configFile = new ConfigNode();
            var configFileNode = new ConfigNode(DISPLAYINFO_NODENAME);

            for (int i = 0; i < NumWins; i++)
            {
                configFileNode.AddValue("winPos_" + i + "_x", winPos[i].x);
                configFileNode.AddValue("winPos_" + i + "_y", winPos[i].y);
                configFileNode.AddValue("winPos_" + i + "_width", winPos[i].width);
                configFileNode.AddValue("winPos_" + i + "_height", winPos[i].height);
            }

#if false
            configFileNode.AddValue("infoWindowPos_x", infoWindowPos.x);
            configFileNode.AddValue("infoWindowPos_y", infoWindowPos.y);
            configFileNode.AddValue("infoWindowPos_width", infoWindowPos.width);
            configFileNode.AddValue("infoWindowPos_height", infoWindowPos.height);
            configFileNode.AddValue("editorInfoWindowPos_x", editorInfoWindowPos.x);
            configFileNode.AddValue("editorInfoWindowPos_y", editorInfoWindowPos.y);
            configFileNode.AddValue("editorInfoWindowPos_width", editorInfoWindowPos.width);
            configFileNode.AddValue("editorInfoWindowPos_height", editorInfoWindowPos.height);
#endif


            configFile.AddNode(configFileNode);


            configFile.Save(fullPath);
        }

        public void LoadData()
        {
            string fullPath = rootPath + CFG_FILE;
            Log.Info("LoadData, fullpath: " + fullPath);
            if (File.Exists(fullPath))
            {
                Log.Info("file exists");
                var configFile = ConfigNode.Load(fullPath);
                if (configFile != null)
                {
                    Log.Info("configFile loaded");
                    var configFileNode = configFile.GetNode(DISPLAYINFO_NODENAME);
                    if (configFileNode != null)
                    {
                        Log.Info("configFileNode loaded");

                        for (int i = 0; i < NumWins; i++)
                        {
                            winPos[i].x = configFileNode.SafeLoad("winPos_" + i + "_x", winPos[i].x);
                            winPos[i].y = configFileNode.SafeLoad("winPos_" + i + "_y", winPos[i].y);
                            winPos[i].width = configFileNode.SafeLoad("winPos_" + i + "_width", winPos[i].width);
                            winPos[i].height = configFileNode.SafeLoad("winPos_" + i + "_height", winPos[i].height);
                        }

#if false
                        infoWindowPos.x = configFileNode.SafeLoad("infoWindowPos_x", infoWindowPos.x);
                        infoWindowPos.y = configFileNode.SafeLoad("infoWindowPos_y", infoWindowPos.y);
                        infoWindowPos.width = configFileNode.SafeLoad("infoWindowPos_width", infoWindowPos.width);
                        infoWindowPos.height = configFileNode.SafeLoad("infoWindowPos_height", infoWindowPos.height);

                        editorInfoWindowPos.x = configFileNode.SafeLoad("editorInfoWindowPos_x", editorInfoWindowPos.x);
                        editorInfoWindowPos.y = configFileNode.SafeLoad("editorInfoWindowPos_y", editorInfoWindowPos.y);
                        editorInfoWindowPos.width = configFileNode.SafeLoad("editorInfoWindowPos_width", editorInfoWindowPos.width);
                        editorInfoWindowPos.height = configFileNode.SafeLoad("editorInfoWindowPos_height", editorInfoWindowPos.height);
#endif
                    }
                }
            }
        }
    }
}