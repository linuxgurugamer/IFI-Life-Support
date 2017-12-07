using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IFILifeSupport
{
	class LifeSupportDisplay
	{

		//Singleton

		private static LifeSupportDisplay instance = null;

		public static LifeSupportDisplay Instance
		{
			get
			{
				if (instance == null)
					instance = new LifeSupportDisplay();
				return instance;
			}
		}

		//Properties

		public const float WINDOW_WIDTH_DEFAULT = 100;
		public const float WINDOW_HEIGHT = 440;
        public static bool WarpCancel = true;
        public static bool ShowRecyclers = true;
		public static bool LSDisplayActive = false;
        public static bool Summarize = false;

        public static Rect infoWindowPos = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
        public static Rect editorInfoWindowPos = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
#pragma warning disable CS0649
        //public static GUILayoutOption[] layoutOptions;
		public static Vector2 infoScrollPos = Vector2.zero;

        public static void ReinitInfoWindowPos()
        {
            infoWindowPos = new Rect(infoWindowPos.x, infoWindowPos.y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
        }
        public static void ReinitEditorInfoWindowPos()
        {

            editorInfoWindowPos = new Rect(editorInfoWindowPos.x, editorInfoWindowPos.y, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
        }
    }
}
