using UnityEngine;
using ToolbarControl_NS;
using KSP_Log;

namespace IFILifeSupport
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        internal static Log Log = null;
        static bool _gamePaused = false;
        static double lastTime = 0;

        public static bool GamePaused
        {
            get
            {
                if (_gamePaused)
                {
                    if (Planetarium.GetUniversalTime() != lastTime)
                        _gamePaused = false;
                }
                lastTime = Planetarium.GetUniversalTime();
                return _gamePaused;
            }
        }

        void Awake()
        {
            if (Log == null)
#if DEBUG
                Log = new Log("IFILS", Log.LEVEL.INFO);
#else
                Log = new Log("IFILS", Log.LEVEL.ERROR);
#endif
            GameEvents.onGamePause.Add(onGamePause);
            GameEvents.onGameUnpause.Add(onGameUnpause);
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);
            Settings.Instance = new Settings(KSPUtil.ApplicationRootPath);

            DontDestroyOnLoad(this);
        }

        void onGamePause() { lastTime = Planetarium.GetUniversalTime(); _gamePaused = true; }
        void onGameUnpause() { if (HighLogic.CurrentGame != null) lastTime = Planetarium.GetUniversalTime(); _gamePaused = false; }

        void onGameSceneLoadRequested(GameScenes gs)
        {
            onGameUnpause();
        }
        void Start()
        {
            ToolbarControl.RegisterMod(IFI_LifeSupportTrackingDisplay.MODID, IFI_LifeSupportTrackingDisplay.MODNAME);
        }

        static public bool initted = false;
        static public GUIStyle tooltipStyle;
        static public GUIStyle kspToolTipStyle;
        static public GUIStyle bold;


        void OnGUI()
        {
            if (!initted)
            {
                initted = true;
                Settings.Instance.myStyle = new GUIStyle();
                Settings.Instance.styleOff = new Texture2D(2, 2);
                Settings.Instance.styleOn = new Texture2D(2, 2);

                Settings.Instance.resizeButton = GetToggleButtonStyle("resize", 20, 20, true);

                tooltipStyle = new GUIStyle(GUI.skin.GetStyle("label"));
                Texture2D texBack = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                texBack.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 1f));
                texBack.Apply();
                tooltipStyle.normal.background = texBack;

                kspToolTipStyle = new GUIStyle(HighLogic.Skin.GetStyle("label"));
                texBack = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                texBack.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 1f));
                texBack.Apply();
                kspToolTipStyle.normal.background = texBack;

                bold = new GUIStyle(GUI.skin.label);
                bold.fontStyle = FontStyle.Bold;

            }
        }

        public GUIStyle GetToggleButtonStyle(string styleName, int width, int height, bool hover)
        {
            Log.Info("GetToggleButtonStyle, styleName: " + styleName);

            ToolbarControl.LoadImageFromFile(ref Settings.Instance.styleOff, "GameData/IFILS/PluginData/textures/" + styleName + "_off");
            ToolbarControl.LoadImageFromFile(ref Settings.Instance.styleOn, "GameData/IFILS/PluginData/textures/" + styleName + "_on");

            Settings.Instance.myStyle.name = styleName + "Button";
            Settings.Instance.myStyle.padding = new RectOffset() { left = 0, right = 0, top = 0, bottom = 0 };
            Settings.Instance.myStyle.border = new RectOffset() { left = 0, right = 0, top = 0, bottom = 0 };
            Settings.Instance.myStyle.margin = new RectOffset() { left = 0, right = 0, top = 2, bottom = 2 };
            Settings.Instance.myStyle.normal.background = Settings.Instance.styleOff;
            Settings.Instance.myStyle.onNormal.background = Settings.Instance.styleOn;
            if (hover)
            {
                Settings.Instance.myStyle.hover.background = Settings.Instance.styleOn;
            }
            Settings.Instance.myStyle.active.background = Settings.Instance.styleOn;
            Settings.Instance.myStyle.fixedWidth = width;
            Settings.Instance.myStyle.fixedHeight = height;
            return Settings.Instance.myStyle;
        }
    }
}