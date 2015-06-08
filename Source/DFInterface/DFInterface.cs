/**
 * DFInterface.cs
 *
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of JPLRepo's DeepFreeze (continued...) - a Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *  
 * This code has been copied and modified from Ship Manifest mod for KSP by Papa_Joe.
 *
 * Continues to be licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace DF
{
    public interface IDFInterface
    {
        Dictionary<string, KerbalInfo> FrozenKerbals { get; }
    }

    public static class DFInterface
    {
        private static bool _dfChecked = false;
        private static bool _dfInstalled = false;
        public static bool IsDFInstalled
        {
            get
            {
                if (!_dfChecked)
                {
                    string assemblyName = "DeepFreeze";
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var assembly = (from a in assemblies
                                    where a.FullName.Contains(assemblyName)
                                    select a).SingleOrDefault();
                    if (assembly != null)
                        _dfInstalled = true;
                    else
                        _dfInstalled = false;
                    _dfChecked = true;
                }
                return _dfInstalled;
            }
        }

        public static IDFInterface GetFrozenKerbals()
        {
            IDFInterface _IDFobj = null;
            Type SMAddonType = AssemblyLoader.loadedAssemblies.SelectMany(a => a.assembly.GetExportedTypes()).SingleOrDefault(t => t.FullName == "DF.DeepFreeze");
            if (SMAddonType != null)
            {
                object DeepFreezeFznKerbalsObj = SMAddonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                _IDFobj = (IDFInterface)DeepFreezeFznKerbalsObj;
            }
            return _IDFobj;
        }            
    }
}
