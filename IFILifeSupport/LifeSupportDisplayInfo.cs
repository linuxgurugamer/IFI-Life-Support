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
        public static bool HideUnmanned = true;
        public static bool ShowRecyclers = true;
        public static bool LSDisplayActive = false;
        //public static bool LSInfoDisplay = false;
        public static bool Summarize = false;
        public static bool displayCalculator = false;

        public static Vector2 infoScrollPos = Vector2.zero;

    }
}
