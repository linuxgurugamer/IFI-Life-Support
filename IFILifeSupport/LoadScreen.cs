#if false
using System;
using System.Collections;
using UnityEngine;
using KSP;

namespace BioMass
{

    /// <summary>
    /// Bio mass load screen.
    /// Fades the biomass logo in/out on the main loading screen.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class IFILifeSupport : MonoBehaviour
    {
        public Texture IFILogo;
        public GameObject logoObj;
        public Color visibleColor;
        public Color invisibleColor;

        public bool showLogo;

        void Start()
        {
            visibleColor = Color.white;
            invisibleColor = Color.clear;
            IFILogo = new Texture();
            string PathPlugin = string.Format("{0}", "IFILS");
            string PathTextures = string.Format("{0}/Flags", PathPlugin);

            string imageFilename = String.Format("{0}/{1}", PathTextures, "IFI_Flag_Large");
            if (GameDatabase.Instance.ExistsTexture(imageFilename))
            {
                IFILogo = GameDatabase.Instance.GetTexture(imageFilename, false);

            }
            else
            {
                print("image not found" + imageFilename);
                return;
            }
            showLogo = false;
            logoObj = new GameObject();
            logoObj.AddComponent<GUITexture>().texture = IFILogo;
            //logoObj.GetComponent<GUITexture>().guiTexture.color = new Color(logoObj.GetComponent<GUITexture>().guiTexture.color.r,logoObj.GetComponent<GUITexture>().guiTexture.color.g, logoObj.GetComponent<GUITexture>().guiTexture.color.b, 0.0f);
            logoObj.transform.localPosition = new Vector3(0.495f, 0.6f, 1.0f);
            logoObj.transform.localScale = new Vector3(0.12f, 0.1f, 0.1f);
            //logoObj.transform.localRotation = new Quaternion(-30,0,0,0);
            StartCoroutine(logoTimerIn());
        }
        void FixedUpdate()
        {

            if (Input.GetMouseButtonDown(0))
            {
                StopCoroutine(logoTimerIn());
                StartCoroutine(logoTimerOut());
            }
        }

        public IEnumerator logoTimerIn()
        {

            Color newColor = logoObj.GetComponent<GUITexture>().color;

            for (int i = 0; newColor.a < 0.4; i++)
            {

                newColor.a = Mathf.MoveTowards(newColor.a, 0.4f, 0.02f);
                yield return new WaitForSeconds(0.003f);
                logoObj.GetComponent<GUITexture>().color = newColor;
                if (newColor.a >= 0.4)
                {
                    StopCoroutine(logoTimerIn());
                }
            }


        }

        public IEnumerator logoTimerOut()
        {

            Color newColor = logoObj.GetComponent<GUITexture>().color;
            for (int i = 0; newColor.a > 0; i++)
            {

                newColor.a = Mathf.MoveTowards(newColor.a, 0.0f, 0.05f);
                yield return new WaitForSeconds(0.003f);
                logoObj.GetComponent<GUITexture>().color = newColor;
                if (newColor.a <= 0)
                {

                    StopCoroutine(logoTimerOut());

                    //Destroy(logoObj);
                }
            }

        }

    }
}

#endif