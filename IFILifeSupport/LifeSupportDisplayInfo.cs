using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{
    class LifeSupportDisplayInfo
    {

        //Singleton
        private static LifeSupportDisplayInfo instance = null;

        public static LifeSupportDisplayInfo Instance
        {
            get
            {
                if (instance == null)
                    instance = new LifeSupportDisplayInfo();
                return instance;
            }
        }

        //Properties

        public const float WINDOW_WIDTH_DEFAULT = 100;
        public const float WINDOW_HEIGHT = 200;
        public static bool WarpCancel = true;
        public static bool ShowRecyclers = true;
        public static bool LSDisplayActive = false;
        public static bool LSInfoDisplay = false;
        public static bool Summarize = false;

        public static Rect statusWindowPos = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
        public static Rect infoWindowPos = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, 400, 300);
        public static Rect editorInfoWindowPos = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT / 2);

        public static Vector2 infoScrollPos = Vector2.zero;

        public static void ReinitInfoWindowPos()
        {
            Log.Info("ReinitInfowindowPos");
            statusWindowPos = new Rect(statusWindowPos.x, statusWindowPos.y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
        }
        public static void ReinitEditorInfoWindowPos()
        {

            editorInfoWindowPos = new Rect(editorInfoWindowPos.x, editorInfoWindowPos.y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
        }
    }
}
