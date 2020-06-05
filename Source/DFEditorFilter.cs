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
using KSP.UI.Screens;
using RUI.Icons.Selectable;
using UnityEngine;
using KSP.Localization;

namespace DF
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class DFEditorFilter : MonoBehaviour
    {
        // This class ass a Filter Icon to the Editor to show DeepFreeze Parts
        // which currently consist of any partprefab that has component DeepFreezer (All the freezer parts)
        // and the GlykerolTankRadial.
        private static List<AvailablePart> avPartItems = new List<AvailablePart>();
        public static DFEditorFilter Instance;
        internal string category = "Filter by Function";
        internal string subCategoryTitle = "DeepFreeze Items";        

        //internal string iconName = "R&D_node_icon_evatech";
        //create and the icons
        //private Texture2D icon_DeepFreeze_Editor;

        internal string iconName = "DeepFreezeEditor";
        internal bool filter = true;

        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        public void Setup()
        {
            Debug.Log("DFEditorFilter Start");
            RemoveSubFilter();
            AddPartUtilitiesCat();
            GameEvents.onGUIEditorToolbarReady.Remove(SubCategories);
            

            if (!HighLogic.CurrentGame.Parameters.CustomParams<DeepFreeze_SettingsParms_Sec3>().EditorFilter)
            {
                Debug.Log("EditorFilter Option is Off");
                return;
            }


            Debug.Log("EditorFilter Option is On");
            DFMMCallBack();
            RemovePartUtilitiesCat();
            GameEvents.onGUIEditorToolbarReady.Add(SubCategories);
            /*
            //Attempt to add Module Manager callback  - find the base type
            System.Type MMType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "ModuleManager.MMPatchLoader");
            if (MMType != null)
            {
                MethodInfo MMPatchLoaderInstanceMethod = MMType.GetMethod("get_Instance", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                if (MMPatchLoaderInstanceMethod != null)
                {
                    object actualMM = MMPatchLoaderInstanceMethod.Invoke(null,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, null, null);
                    MethodInfo MMaddPostPatchCallbackMethod = MMType.GetMethod("addPostPatchCallback", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                    if (actualMM != null && MMaddPostPatchCallbackMethod != null)
                        MMaddPostPatchCallbackMethod.Invoke(actualMM, new object[] { this.DFMMCallBack() });
                }
                
            }*/
            //DFMMCallBack();
            Debug.Log("DFEditorFilter Awake Complete");
        }

        public bool DFMMCallBack()
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
            return true;
        }

        private void RemoveSubFilter()
        {
            if (PartCategorizer.Instance != null)
            {
                PartCategorizer.Category Filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == category);
                if (Filter != null)
                {
                    PartCategorizer.Category subFilter = Filter.subcategories.Find(f => f.button.categoryName == subCategoryTitle);
                    if (subFilter != null)
                    {
                        subFilter.DeleteSubcategory();
                    }
                }
            }
        }

        private void RemovePartUtilitiesCat()
        {
            foreach (AvailablePart avPart in avPartItems)
            {
                avPart.category = PartCategories.none;
            }
        }

        private void AddPartUtilitiesCat()
        {
            foreach (AvailablePart avPart in avPartItems)
            {
                avPart.category = PartCategories.Utility;
            }
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
            RemoveSubFilter();
            Icon filterDeepFreeze = new Icon("DeepFreezeEditor", Textures.DeepFreeze_Editor, Textures.DeepFreeze_Editor, true);
            PartCategorizer.Category Filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == category);
            PartCategorizer.AddCustomSubcategoryFilter(Filter, subCategoryTitle, Localizer.Format("#autoLOC_DF_00107"), filterDeepFreeze, p => EditorItemsFilter(p));
        }
    }
}