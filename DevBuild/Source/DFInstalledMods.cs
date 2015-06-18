using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace DF
{
    class DFInstalledMods
    {

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

        internal static bool IsModInstalled(string assemblyName)
        {
            Assembly assembly = (from a in assemblies
                                 where a.FullName.Contains(assemblyName)
                                 select a).SingleOrDefault();
            return assembly != null;
        }

        internal static bool RTVesselConnected
        {
            get
            {
                bool RTVslConnected = false;
                RTVslConnected = (RemoteTech.API.API.HasLocalControl(FlightGlobals.ActiveVessel.id) || RemoteTech.API.API.HasAnyConnection(FlightGlobals.ActiveVessel.id));            
                Utilities.Log_Debug("RTVesselConnected = " + RTVslConnected);            
                return RTVslConnected;
            }            
        }

        internal static double RTVesselDelay
        {
            get
            {
                double RTVslDelay = 0f;
                RTVslDelay = RemoteTech.API.API.GetShortestSignalDelay(FlightGlobals.ActiveVessel.id);
                Utilities.Log_Debug("RTVesselDelay = " + RTVslDelay);
                return RTVslDelay;
            }
            
        }
    }
}
