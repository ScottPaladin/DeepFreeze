// The following Class is derived from Kerbal Alarm Clock mod. Which is licensed under:
// The MIT License(MIT) Copyright(c) 2014, David Tregoning
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using UnityEngine;
using RSTUtils;

namespace DF
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class LoadGlobals : MonoBehaviour
    {
        public static LoadGlobals Instance;
        //Awake Event - when the DLL is loaded
        public void Awake()
        {
            if (Instance != null)
                return;
            Instance = this;
            Textures.LoadIconAssets();
            DontDestroyOnLoad(this);
            Utilities.Log("DeepFreeze LoadGlobals Awake Complete");
        }

        public void Start()
        {
            //GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
        }

        public void OnDestroy()
        {
            //GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
        }
    }

    internal static class Textures
    {
        
        internal static Texture2D TooltipBox = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        internal static Texture2D BtnRedCross = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D BtnResize = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        internal static String PathIconsPath = System.IO.Path.Combine(DeepFreeze._AssemblyFolder.Substring(0, DeepFreeze._AssemblyFolder.IndexOf("\\DeepFreeze\\") + 11), "Icons").Replace("\\", "/");
        internal static String PathToolbarIconsPath = PathIconsPath.Substring(PathIconsPath.ToLower().IndexOf("/gamedata/") + 10);


        internal static void LoadIconAssets()
        {
            try
            {
                LoadImageFromFile(ref TooltipBox, "DFToolTipBox.png", PathIconsPath);
                LoadImageFromFile(ref BtnRedCross, "DFbtnRedCross.png", PathIconsPath);
                LoadImageFromFile(ref BtnResize, "DFbtnResize.png", PathIconsPath);
            }
            catch (Exception)
            {
                RSTUtils.Utilities.Log("DeepFreeze Failed to Load Textures - are you missing a file?");
            }
        }

        public static Boolean LoadImageFromFile(ref Texture2D tex, String fileName, String folderPath = "")
        {
            Boolean blnReturn = false;
            try
            {
                if (folderPath == "") folderPath = PathIconsPath;

                //File Exists check
                if (System.IO.File.Exists(String.Format("{0}/{1}", folderPath, fileName)))
                {
                    try
                    {
                        tex.LoadImage(System.IO.File.ReadAllBytes(String.Format("{0}/{1}", folderPath, fileName)));
                        blnReturn = true;
                    }
                    catch (Exception ex)
                    {
                        RSTUtils.Utilities.Log("TST Failed to load the texture:" + folderPath + "(" + fileName + ")");
                        RSTUtils.Utilities.Log(ex.Message);
                    }
                }
                else
                {
                    RSTUtils.Utilities.Log("TST Cannot find texture to load:" + folderPath + "(" + fileName + ")");
                }


            }
            catch (Exception ex)
            {
                RSTUtils.Utilities.Log("TST Failed to load (are you missing a file):" + folderPath + "(" + fileName + ")");
                RSTUtils.Utilities.Log(ex.Message);
            }
            return blnReturn;
        }

        internal static GUIStyle ResizeStyle, ClosebtnStyle;
        internal static GUIStyle statusStyle, frozenStyle, comaStyle, sectionTitleLeftStyle, sectionTitleCenterStyle, resizeStyle, StatusOKStyle, 
            StatusWarnStyle, StatusRedStyle, StatusGrayStyle, ButtonStyle;


        internal static bool StylesSet = false;

        internal static void SetupStyles()
        {
            GUI.skin = HighLogic.Skin;

            //Init styles

            Utilities._TooltipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                stretchHeight = true,
                wordWrap = true,
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(4, 4, 6, 4),
                alignment = TextAnchor.MiddleCenter
            };
            Utilities._TooltipStyle.normal.background = Textures.TooltipBox;
            Utilities._TooltipStyle.normal.textColor = new Color32(207, 207, 207, 255);
            Utilities._TooltipStyle.hover.textColor = Color.blue;
            
            ClosebtnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 20,
                fixedHeight = 20,
                fontSize = 14,
                fontStyle = FontStyle.Normal
            };
            ClosebtnStyle.active.background = GUI.skin.toggle.onNormal.background;
            ClosebtnStyle.onActive.background = ClosebtnStyle.active.background;
            ClosebtnStyle.padding = Utilities.SetRectOffset(ClosebtnStyle.padding, 3);

            ResizeStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 20,
                fixedHeight = 20,
                fontSize = 14,
                fontStyle = FontStyle.Normal
            };
            ResizeStyle.onActive.background = ClosebtnStyle.active.background;
            ResizeStyle.padding = Utilities.SetRectOffset(ClosebtnStyle.padding, 3);

            //Init styles
            sectionTitleLeftStyle = new GUIStyle(GUI.skin.label);
            sectionTitleLeftStyle.alignment = TextAnchor.MiddleLeft;
            sectionTitleLeftStyle.stretchWidth = true;
            sectionTitleLeftStyle.normal.textColor = Color.blue;
            sectionTitleLeftStyle.fontStyle = FontStyle.Bold;

            sectionTitleCenterStyle = new GUIStyle(GUI.skin.label);
            sectionTitleCenterStyle.alignment = TextAnchor.MiddleCenter;
            sectionTitleCenterStyle.stretchWidth = true;
            sectionTitleCenterStyle.fontStyle = FontStyle.Bold;
            
            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.stretchWidth = true;
            statusStyle.normal.textColor = Color.white;
            
            frozenStyle = new GUIStyle(GUI.skin.label);
            frozenStyle.alignment = TextAnchor.MiddleLeft;
            frozenStyle.stretchWidth = true;
            frozenStyle.normal.textColor = Color.cyan;
            
            comaStyle = new GUIStyle(GUI.skin.label);
            comaStyle.alignment = TextAnchor.MiddleLeft;
            comaStyle.stretchWidth = true;
            comaStyle.normal.textColor = Color.gray;

            StatusOKStyle = new GUIStyle(GUI.skin.label);
            StatusOKStyle.alignment = TextAnchor.MiddleLeft;
            StatusOKStyle.stretchWidth = true;
            StatusOKStyle.normal.textColor = Color.green;

            StatusWarnStyle = new GUIStyle(GUI.skin.label);
            StatusWarnStyle.alignment = TextAnchor.MiddleLeft;
            StatusWarnStyle.stretchWidth = true;
            StatusWarnStyle.normal.textColor = Color.yellow;

            StatusRedStyle = new GUIStyle(GUI.skin.label);
            StatusRedStyle.alignment = TextAnchor.MiddleLeft;
            StatusRedStyle.stretchWidth = true;
            StatusRedStyle.normal.textColor = Color.red;

            StatusGrayStyle = new GUIStyle(GUI.skin.label);
            StatusGrayStyle.alignment = TextAnchor.MiddleLeft;
            StatusGrayStyle.stretchWidth = true;
            StatusGrayStyle.normal.textColor = Color.gray;

            resizeStyle = new GUIStyle(GUI.skin.button);
            resizeStyle.alignment = TextAnchor.MiddleCenter;
            resizeStyle.padding = new RectOffset(1, 1, 1, 1);

            ButtonStyle = new GUIStyle(GUI.skin.toggle);
            ButtonStyle.margin.top = 0;
            ButtonStyle.margin.bottom = 0;
            ButtonStyle.padding.top = 0;
            ButtonStyle.padding.bottom = 0;
            
            StylesSet = true;
        }
    }
}