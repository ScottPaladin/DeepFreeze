/**
 * Utilities.cs
 *
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
using System.Reflection;
using UnityEngine;

namespace DF
{    
    public static class Utilities
    {
              
        // Dump an object by reflection
        public static void DumpObjectFields(object o, string title = "---------")
        {
            // Dump the raw PQS of Dres (by reflection)
            Debug.Log("---------" + title + "------------");
            foreach (FieldInfo field in o.GetType().GetFields())
            {
                if (!field.IsStatic)
                {
                    Debug.Log(field.Name + " = " + field.GetValue(o));
                }
            }
            Debug.Log("--------------------------------------");
        }

        /**
          * Recursively searches for a named transform in the Transform heirarchy.  The requirement of
          * such a function is sad.  This should really be in the Unity3D API.  Transform.Find() only
          * searches in the immediate children.
          *
          * @param transform Transform in which is search for named child
          * @param name Name of child to find
          * 
          * @return Desired transform or null if it could not be found
          */
        public static Transform FindInChildren(Transform transform, string name)
        {
            // Is this null?
            if (transform == null)
            {
                return null;
            }

            // Are the names equivalent
            if (transform.name == name)
            {
                return transform;
            }

            // If we did not find a transform, search through the children
            foreach (Transform child in transform)
            {
                // Recurse into the child
                Transform t = FindInChildren(child, name);
                if (t != null)
                {
                    return t;
                }
            }

            // Return the transform (will be null if it was not found)
            return null;
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        public static bool ActiveKerbalIsLocal(this Part thisPart)
        {
            return FindCurrentKerbal(thisPart) != null;
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        public static int CurrentActiveSeat(this Part thisPart)
        {
            Kerbal activeKerbal = thisPart.FindCurrentKerbal();
            return activeKerbal != null ? activeKerbal.protoCrewMember.seatIdx : -1;
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        public static Kerbal FindCurrentKerbal(this Part thisPart)
        {
            if (thisPart.internalModel == null || !VesselIsInIVA(thisPart.vessel))
                return null;
            // InternalCamera instance does not contain a reference to the kerbal it's looking from.
            // So we have to search through all of them...
            Kerbal thatKerbal = null;
            foreach (InternalSeat thatSeat in thisPart.internalModel.seats)
            {
                if (thatSeat.kerbalRef != null)
                {
                    if (thatSeat.kerbalRef.eyeTransform == InternalCamera.Instance.transform.parent)
                    {
                        thatKerbal = thatSeat.kerbalRef;
                        break;
                    }
                }
            }
            return thatKerbal;
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        public static bool VesselIsInIVA(Vessel thatVessel)
        {
            // Inactive IVAs are renderer.enabled = false, this can and should be used...
            // ... but now it can't because we're doing transparent pods, so we need a more complicated way to find which pod the player is in.
            return HighLogic.LoadedSceneIsFlight && IsActiveVessel(thatVessel) && IsInIVA();
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        public static bool IsActiveVessel(Vessel thatVessel)
        {
            return (HighLogic.LoadedSceneIsFlight && thatVessel != null && thatVessel.isActiveVessel);
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        public static bool IsInIVA()
        {
            return CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA;
        }

        
        public static bool IsInInternal()
        {
            return CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal;
        }

        //Temperature
        public static float KelvintoCelsius(float kelvin)
        {
            return (kelvin - 273.15f);

        }

        public static float CelsiustoKelvin(float celsius)
        {
            return (celsius + 273.15f);
        }
        
        // GUI & Window Methods

        public static bool WindowVisibile(Rect winpos)
        {
            float minmargin = 20.0f; // 20 bytes margin for the window
            float xMin = minmargin - winpos.width;
            float xMax = Screen.width - minmargin;
            float yMin = minmargin - winpos.height;
            float yMax = Screen.height - minmargin;
            bool xRnge = (winpos.x > xMin) && (winpos.x < xMax);
            bool yRnge = (winpos.y > yMin) && (winpos.y < yMax);
            return xRnge && yRnge;
        }

        public static Rect MakeWindowVisible(Rect winpos)
        {
            float minmargin = 20.0f; // 20 bytes margin for the window
            float xMin = minmargin - winpos.width;
            float xMax = Screen.width - minmargin;
            float yMin = minmargin - winpos.height;
            float yMax = Screen.height - minmargin;

            winpos.x = Mathf.Clamp(winpos.x, xMin, xMax);
            winpos.y = Mathf.Clamp(winpos.y, yMin, yMax);

            return winpos;
        }

        // Get Config Node Values out of a config node Methods

        public static bool GetNodeValue(ConfigNode confignode, string fieldname, bool defaultValue)
        {
            bool newValue;
            if (confignode.HasValue(fieldname) && bool.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static int GetNodeValue(ConfigNode confignode, string fieldname, int defaultValue)
        {
            int newValue;
            if (confignode.HasValue(fieldname) && int.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static float GetNodeValue(ConfigNode confignode, string fieldname, float defaultValue)
        {
            float newValue;
            if (confignode.HasValue(fieldname) && float.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static double GetNodeValue(ConfigNode confignode, string fieldname, double defaultValue)
        {
            double newValue;
            if (confignode.HasValue(fieldname) && double.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static string GetNodeValue(ConfigNode confignode, string fieldname, string defaultValue)
        {
            if (confignode.HasValue(fieldname))
            {
                return confignode.GetValue(fieldname);
            }
            else
            {
                return defaultValue;
            }
        }

        public static Guid GetNodeValue(ConfigNode confignode, string fieldname)
        {
            if (confignode.HasValue(fieldname))
            {
                confignode.GetValue(fieldname);
                Log_Debug("getnodguid ", fieldname);
                return new Guid(fieldname);
            }
            else
            {

                return Guid.Empty;
            }
        }

        public static T GetNodeValue<T>(ConfigNode confignode, string fieldname, T defaultValue) where T : IComparable, IFormattable, IConvertible
        {
            if (confignode.HasValue(fieldname))
            {
                string stringValue = confignode.GetValue(fieldname);
                if (Enum.IsDefined(typeof(T), stringValue))
                {
                    return (T)Enum.Parse(typeof(T), stringValue);
                }
            }
            return defaultValue;
        }

        // Logging Functions
        // Name of the Assembly that is running this MonoBehaviour
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        public static void Log(this UnityEngine.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log(this System.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetHashCode().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log(string context, string message)
        {
            Debug.Log(context + "[][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(this UnityEngine.Object obj, string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log(obj.GetType().FullName + "[" + obj.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(this System.Object obj, string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log(obj.GetType().FullName + "[" + obj.GetHashCode().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(string context, string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log(context + "[][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log("[DeepFreeze][" + Time.time.ToString("0.00") + "]: " + message);
        }
    }
}