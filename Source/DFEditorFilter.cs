/**
 * DeepFreeze Continued...
 * (C) Copyright 2015, Jamie Leighton
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of JPLRepo's DeepFreeze (continued...) - a Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Continues to be licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */

using System.Collections.Generic;
using System.IO;
using KSP.UI.Screens;
using RUI.Icons.Selectable;
using UnityEngine;

namespace DF
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class DFEditorFilter : MonoBehaviour
    {
        // This class ass a Filter Icon to the Editor to show DeepFreeze Parts
        // which currently consist of any partprefab that has component DeepFreezer (All the freezer parts)
        // and the GlykerolTankRadial.
        private static List<AvailablePart> avPartItems = new List<AvailablePart>();

        internal string category = "Filter by Function";
        internal string subCategoryTitle = "DeepFreeze Items";
        internal string defaultTitle = "DF";

        //internal string iconName = "R&D_node_icon_evatech";
        //create and the icons
        private Texture2D icon_DeepFreeze_Editor = new Texture2D(32, 32);

        internal string iconName = "DeepFreezeEditor";
        internal bool filter = true;

        private void Awake()
        {
            Debug.Log("DFEditorFilter Awake");
            GameEvents.onGUIEditorToolbarReady.Add(SubCategories);
            //ModuleManager.MMPatchLoader.addPostPatchCallback(DFMMCallBack);
            DFMMCallBack();
            //load the icons
            icon_DeepFreeze_Editor.LoadImage(File.ReadAllBytes("GameData/REPOSoftTech/DeepFreeze/Icons/DeepFreezeEditor.png"));

            Debug.Log("DFEditorFilter Awake Complete");
        }

        public void DFMMCallBack()
        {
            Debug.Log("DFEDitorFilter DFMMCallBack");
            avPartItems.Clear();
            foreach (AvailablePart avPart in PartLoader.LoadedPartsList)
            {
                if (!avPart.partPrefab) continue;
                DeepFreezer moduleItem = avPart.partPrefab.GetComponent<DeepFreezer>();
                if (avPart.name == "GlykerolTankRadial" || avPart.name == "DF.Glykerol.Tank" || moduleItem)
                {
                    avPartItems.Add(avPart);
                }
            }
            Debug.Log("DFEDitorFilter DFMMCallBack end");
        }

        private bool EditorItemsFilter(AvailablePart avPart)
        {
            if (avPartItems.Contains(avPart))
            {
                return true;
            }
            return false;
        }

        private void SubCategories()
        {
            Icon filterDeepFreeze = new Icon("DeepFreezeEditor", icon_DeepFreeze_Editor, icon_DeepFreeze_Editor, true);
            PartCategorizer.Category Filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == category);
            PartCategorizer.AddCustomSubcategoryFilter(Filter, subCategoryTitle, filterDeepFreeze, p => EditorItemsFilter(p));
            //RUIToggleButtonTyped button = Filter.button.activeButton;
            //button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            //button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}