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
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DF
{
    internal class DFInstalledMods
    {
        // Class used to check what Other mods we are interested in are installed.
        private static bool? _SMInstalled = null;

        private static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        internal static bool SMInstalled
        {
            get
            {
                _SMInstalled = ShipManifest.SMInterface.IsSMInstalled;
                return (bool)_SMInstalled;
            }
        }

        internal static bool IsRTInstalled
        {
            get
            {
                return IsModInstalled("RemoteTech");
            }
        }

        internal static bool IsUSILSInstalled
        {
            get
            {
                return IsModInstalled("USILifeSupport");
            }
        }

        internal static bool IsSnacksInstalled
        {
            get
            {
                return IsModInstalled("Snacks");
            }
        }

        internal static bool IsTACLSInstalled
        {
            get
            {
                return IsModInstalled("TacLifeSupport");
            }
        }

        internal static bool IsRPMInstalled
        {
            get
            {
                return IsModInstalled("RasterPropMonitor");
            }
        }

        internal static bool IsBGPInstalled
        {
            get
            {
                return IsModInstalled("BackgroundProcessing");
            }
        }

        internal static bool IsModInstalled(string assemblyName)
        {
            try
            {
                Assembly assembly = (from a in assemblies
                                     where a.FullName.Split(',')[0] == assemblyName
                                     select a).First();
                return assembly != null;
            }
            catch
            {
                return false;
            }
        }

        internal static bool RTVesselConnected(Guid id)
        {
            bool RTVslConnected = false;
            if (IsRTInstalled)
            {
                RTVslConnected = (RemoteTech.API.API.HasLocalControl(id) || RemoteTech.API.API.HasAnyConnection(id));
                //Utilities.Log_Debug("vessel " + id + "haslocal " + RemoteTech.API.API.HasLocalControl(id) + " has any " + RemoteTech.API.API.HasAnyConnection(id));
            }
            return RTVslConnected;
        }

        internal static double RTVesselDelay
        {
            get
            {
                double RTVslDelay = 0f;
                RTVslDelay = RemoteTech.API.API.GetShortestSignalDelay(FlightGlobals.ActiveVessel.id);
                return RTVslDelay;
            }
        }
    }
}