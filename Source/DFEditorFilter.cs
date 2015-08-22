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

using System;
using System.Collections.Generic;
using System.Linq;
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
        internal string iconName = "R&D_node_icon_evatech";
        internal bool filter = true;

        private void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(SubCategories);

            avPartItems.Clear();
            foreach (AvailablePart avPart in PartLoader.LoadedPartsList)
            {
                if (!avPart.partPrefab) continue;
                DeepFreezer moduleItem = avPart.partPrefab.GetComponent<DeepFreezer>();
                if (avPart.name == "GlykerolTankRadial" || moduleItem)
                {
                    avPartItems.Add(avPart);
                }
            }
        }

        private bool EditorItemsFilter(AvailablePart avPart)
        {
            if (avPartItems.Contains(avPart))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SubCategories()
        {
            RUI.Icons.Selectable.Icon icon = PartCategorizer.Instance.iconLoader.GetIcon(iconName);
            PartCategorizer.Category Filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == category);
            PartCategorizer.AddCustomSubcategoryFilter(Filter, subCategoryTitle, icon, p => EditorItemsFilter(p));

            RUIToggleButtonTyped button = Filter.button.activeButton;
            button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}