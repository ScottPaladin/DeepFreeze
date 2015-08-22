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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DF
{
    public enum DoorState
    {
        OPEN,
        CLOSED,
        OPENING,
        CLOSING,
        UNKNOWN
    }

    internal static class Utilities
    {
        // Dump an object by reflection
        internal static void DumpObjectFields(object o, string title = "---------")
        {
            // Dump (by reflection)
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

        // Dump all Unity Cameras
        internal static void DumpCameras()
        {
            // Dump (by reflection)
            Debug.Log("--------- Dump Unity Cameras ------------");
            foreach (Camera c in Camera.allCameras)
            {
                Debug.Log("Camera " + c.name + " cullingmask " + c.cullingMask + " depth " + c.depth + " farClipPlane " + c.farClipPlane + " nearClipPlane " + c.nearClipPlane);
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

        internal static Transform FindInChildren(Transform transform, string name)
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

        internal static Camera FindCamera(string name)
        {
            foreach (Camera c in Camera.allCameras)
            {
                if (c.name == name)
                {
                    return c;
                }
            }
            return null;
        }

        // The following method is modified from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        internal static void setTransparentTransforms(this Part thisPart, string transparentTransforms)
        {
            string transparentShaderName = "Transparent/Specular";
            Shader transparentShader;
            transparentShader = Shader.Find(transparentShaderName);
            foreach (string transformName in transparentTransforms.Split('|'))
            {
                Log_Debug("setTransparentTransforms " + transformName);
                try
                {
                    Transform tr = thisPart.FindModelTransform(transformName.Trim());
                    if (tr != null)
                    {
                        // We both change the shader and backup the original shader so we can undo it later.
                        Shader backupShader = tr.renderer.material.shader;
                        tr.renderer.material.shader = transparentShader;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Unable to set transparent shader transform " + transformName);
                    Debug.LogException(e);
                }
            }
        }

        // The following method is derived from TextureReplacer mod. Which is licensed as:
        //Copyright © 2013-2015 Davorin Učakar, Ryan Bray
        //Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
        //The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
        private static double atmSuitPressure = 50.0;

        internal static bool isAtmBreathable()
        {
            bool value = !HighLogic.LoadedSceneIsFlight
                         || (FlightGlobals.getStaticPressure() >= atmSuitPressure);
            Log_Debug("isATMBreathable Inflight? " + value + " InFlight " + HighLogic.LoadedSceneIsFlight + " StaticPressure " + FlightGlobals.getStaticPressure());
            return value;
        }

        // The following method is derived from TextureReplacer mod. Which is licensed as:
        //Copyright © 2013-2015 Davorin Učakar, Ryan Bray
        //Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
        //The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
        private static Mesh[] helmetMesh = { null, null };

        private static Mesh[] visorMesh = { null, null };
        private static bool helmetMeshstored = false;

        internal static void storeHelmetMesh()
        {
            Log_Debug("StoreHelmetMesh");
            foreach (Kerbal kerbal in Resources.FindObjectsOfTypeAll<Kerbal>())
            {
                int gender = kerbal.transform.name == "kerbalFemale" ? 1 : 0;
                // Save pointer to helmet & visor meshes so helmet removal can restore them.
                foreach (SkinnedMeshRenderer smr in kerbal.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.name.EndsWith("helmet", StringComparison.Ordinal))
                        helmetMesh[gender] = smr.sharedMesh;
                    else if (smr.name.EndsWith("visor", StringComparison.Ordinal))
                        visorMesh[gender] = smr.sharedMesh;
                }
            }
            helmetMeshstored = true;
        }

        // The following method is derived from TextureReplacer mod.Which is licensed as:
        //Copyright © 2013-2015 Davorin Učakar, Ryan Bray
        //Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
        //The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
        internal static void setHelmetshaders(Kerbal thatKerbal, bool helmetOn)
        {
            if (!helmetMeshstored)
                storeHelmetMesh();

            //This will check if Atmospher is breathable then we always remove our hetmets regardless.
            if (helmetOn && isAtmBreathable())
            {
                helmetOn = false;
                Log_Debug("setHelmetShaders to put on helmet but in breathable atmosphere");
            }

            try
            {
                foreach (SkinnedMeshRenderer smr in thatKerbal.helmetTransform.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (smr.name.EndsWith("helmet", StringComparison.Ordinal))
                        smr.sharedMesh = helmetOn ? helmetMesh[(int)thatKerbal.protoCrewMember.gender] : null;
                    else if (smr.name.EndsWith("visor", StringComparison.Ordinal))
                        smr.sharedMesh = helmetOn ? visorMesh[(int)thatKerbal.protoCrewMember.gender] : null;
                }
            }
            catch (Exception ex)
            {
                Log("DeepFreezer", "Error attempting to setHelmetshaders for " + thatKerbal.name + " to " + helmetOn);
                Log("DeepFreezer ", ex.Message);
            }
        }

        // The following method is derived from TextureReplacer mod. Which is licensed as:
        //Copyright © 2013-2015 Davorin Učakar, Ryan Bray
        //Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
        //The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
        internal static void setHelmets(this Part thisPart, bool helmetOn)
        {
            if (thisPart.internalModel == null)
            {
                Log_Debug("setHelmets but no internalModel");
                return;
            }

            if (!helmetMeshstored)
                storeHelmetMesh();

            Log_Debug("setHelmets helmetOn=" + helmetOn);
            //Kerbal thatKerbal = null;
            foreach (InternalSeat thatSeat in thisPart.internalModel.seats)
            {
                if (thatSeat.crew != null)
                {
                    Kerbal thatKerbal = thatSeat.kerbalRef;
                    if (thatKerbal != null)
                    {
                        thatSeat.allowCrewHelmet = helmetOn;
                        Log_Debug("Setting helmet=" + helmetOn + " for kerbal " + thatSeat.crew.name);
                        // `Kerbal.ShowHelmet(false)` irreversibly removes a helmet while
                        // `Kerbal.ShowHelmet(true)` has no effect at all. We need the following workaround.
                        // I think this can be done using a coroutine to despawn and spawn the internalseat crewmember kerbalref.
                        // But I found this workaround in TextureReplacer so easier to use that.
                        //if (thatKerbal.showHelmet)
                        //{
                        setHelmetshaders(thatKerbal, helmetOn);
                        //}
                        //else
                        //    Log_Debug("Showhelmet is OFF so the helmettransform does not exist");
                    }
                    else
                        Log_Debug("kerbalref = null?");
                }
            }
        }

        // Sets the kerbal layers to make them visible (Thawed) or not (Frozen), setVisible = true sets layers to visible, false turns them off.
        // If bodyOnly is true only the "body01" mesh is changed (to be replaced by placeholder mesh lying down as kerbals in IVA are always in sitting position).
        internal static void setFrznKerbalLayer(ProtoCrewMember kerbal, bool setVisible, bool bodyOnly)
        {
            int layer = 16;
            if (!setVisible)
            {
                layer = 21;
            }

            foreach (Renderer renderer in kerbal.KerbalRef.GetComponentsInChildren<Renderer>(true))
            {
                if ((bodyOnly && renderer.name == "body01") || !bodyOnly)
                {
                    if (renderer.gameObject.layer == layer)
                    {
                        Log_Debug("Layers already set");
                        break;
                    }
                    Log_Debug("Renderer: " + renderer.name + " set to layer " + layer);
                    renderer.gameObject.layer = layer;
                    if (setVisible) renderer.enabled = true;
                    else renderer.enabled = false;
                }
            }
        }

        internal static void CheckPortraitCams(Vessel vessel)
        {
            // Only the pods in the active vessel should be doing it since the list refers to them.
            Log_Debug("CheckPortraitCams");
            if (vessel.isActiveVessel)
            {
                // First, every pod should check through the list of portaits and remove everyone who is from some other vessel, or NO vessel.
                var stowaways = new List<Kerbal>();
                foreach (Kerbal thatKerbal in KerbalGUIManager.ActiveCrew)
                {
                    if (thatKerbal.InPart == null)
                    {
                        stowaways.Add(thatKerbal);
                    }
                    else
                    {
                        if (thatKerbal.InVessel != vessel)
                        {
                            stowaways.Add(thatKerbal);
                        }
                    }
                }
                foreach (Kerbal thatKerbal in stowaways)
                {
                    KerbalGUIManager.RemoveActiveCrew(thatKerbal);
                }
                // Then, every pod should check the list of seats in itself and see if anyone is missing who should be present.
                List<Part> crewparts = (from p in vessel.parts where (p.CrewCapacity > 0 && p.internalModel != null) select p).ToList();
                foreach (Part part in crewparts)
                {
                    Log_Debug("Check Portraits for part " + part.name);
                    foreach (InternalSeat seat in part.internalModel.seats)
                    {
                        Log_Debug("checking Seat " + seat.seatTransformName);
                        if (seat.kerbalRef != null) Log_Debug("kerbalref=" + seat.kerbalRef.crewMemberName);
                        else Log_Debug("Seat kerbalref is null");
                        if (seat.kerbalRef != null && !KerbalGUIManager.ActiveCrew.Contains(seat.kerbalRef))
                        {
                            Log_Debug("Checking crewstatus " + seat.kerbalRef.protoCrewMember.rosterStatus + " " + seat.kerbalRef.protoCrewMember.type);
                            if (seat.kerbalRef.protoCrewMember.rosterStatus != ProtoCrewMember.RosterStatus.Dead || seat.kerbalRef.protoCrewMember.type != ProtoCrewMember.KerbalType.Unowned)
                            {
                                Log_Debug("Adding missing Portrait for " + seat.kerbalRef.crewMemberName);
                                KerbalGUIManager.AddActiveCrew(seat.kerbalRef);
                            }
                        }
                    }
                }
            }
            else Log_Debug("Vessel is not active vessel");
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        internal static Kerbal FindCurrentKerbal(this Part thisPart)
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
        internal static bool VesselIsInIVA(Vessel thatVessel)
        {
            // Inactive IVAs are renderer.enabled = false, this can and should be used...
            // ... but now it can't because we're doing transparent pods, so we need a more complicated way to find which pod the player is in.
            return HighLogic.LoadedSceneIsFlight && IsActiveVessel(thatVessel) && IsInIVA();
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        internal static bool IsActiveVessel(Vessel thatVessel)
        {
            return (HighLogic.LoadedSceneIsFlight && thatVessel != null && thatVessel.isActiveVessel);
        }

        // The following method is taken from RasterPropMonitor as-is. Which is covered by GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
        internal static bool IsInIVA()
        {
            return CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA;
        }

        internal static bool IsInInternal()
        {
            return CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal;
        }

        internal static IEnumerator WaitForAnimation(Animation animation, string name)
        {
            do
            {
                yield return null;
            } while (animation.IsPlaying(name));
        }

        //Temperature
        internal static float KelvintoCelsius(float kelvin)
        {
            return (kelvin - 273.15f);
        }

        internal static float CelsiustoKelvin(float celsius)
        {
            return (celsius + 273.15f);
        }

        // GUI & Window Methods

        internal static bool WindowVisibile(Rect winpos)
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

        internal static Rect MakeWindowVisible(Rect winpos)
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

        internal static bool GetNodeValue(ConfigNode confignode, string fieldname, bool defaultValue)
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

        internal static int GetNodeValue(ConfigNode confignode, string fieldname, int defaultValue)
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

        internal static float GetNodeValue(ConfigNode confignode, string fieldname, float defaultValue)
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

        internal static double GetNodeValue(ConfigNode confignode, string fieldname, double defaultValue)
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

        internal static string GetNodeValue(ConfigNode confignode, string fieldname, string defaultValue)
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

        internal static Guid GetNodeValue(ConfigNode confignode, string fieldname)
        {
            if (confignode.HasValue(fieldname))
            {
                try
                {
                    Guid id = new Guid(confignode.GetValue(fieldname));
                    return id;
                }
                catch (Exception ex)
                {
                    Debug.Log("Unable to getNodeValue " + fieldname + " from " + confignode);
                    Debug.Log("Err: " + ex);
                    return Guid.Empty;
                }
            }
            else
            {
                return Guid.Empty;
            }
        }

        internal static T GetNodeValue<T>(ConfigNode confignode, string fieldname, T defaultValue) where T : IComparable, IFormattable, IConvertible
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

        //Format a Time double variable into format "YxxxxDxxxhh:mm:ss"
        //Future expansion required to format to different formats.
        internal static string FormatDateString(double time)
        {
            string outputstring = string.Empty;
            int[] datestructure = new int[5];
            if (GameSettings.KERBIN_TIME)
            {
                datestructure[0] = (int)time / 60 / 60 / 6 / 426; /// Years
                datestructure[1] = (int)time / 60 / 60 / 6 % 426; // Days
                datestructure[2] = (int)time / 60 / 60 % 6;    // Hours
                datestructure[3] = (int)time / 60 % 60;    // Minutes
                datestructure[4] = (int)time % 60; //seconds
            }
            else
            {
                datestructure[0] = (int)time / 60 / 60 / 24 / 365; /// Years
                datestructure[1] = (int)time / 60 / 60 / 24 % 365; // Days
                datestructure[2] = (int)time / 60 / 60 % 24;    // Hours
                datestructure[3] = (int)time / 60 % 60;    // Minutes
                datestructure[4] = (int)time % 60; //seconds
            }
            if (datestructure[0] > 0)
                outputstring += "Y" + datestructure[0].ToString("####") + ":";
            if (datestructure[1] > 0)
                outputstring += "D" + datestructure[1].ToString("###") + ":";
            outputstring += datestructure[2].ToString("00:");
            outputstring += datestructure[3].ToString("00:");
            outputstring += datestructure[4].ToString("00");
            return outputstring;
        }

        //Returns True if the PauseMenu is open. Because the GameEvent callbacks don't work on the mainmenu.
        internal static bool isPauseMenuOpen
        {
            get
            {
                try
                {
                    return PauseMenu.isOpen;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Electricity and temperature functions are only valid if timewarp factor is < 5.
        internal static bool timewarpIsValid(int max)
        {
            return TimeWarp.CurrentRateIndex < max;
        }

        internal static void stopWarp()
        {
            TimeWarp.SetRate(0, false);
        }

        // Logging Functions
        // Name of the Assembly that is running this MonoBehaviour
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        internal static void Log(this UnityEngine.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        internal static void Log(this System.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetHashCode().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        internal static void Log(string context, string message)
        {
            Debug.Log(context + "[][" + Time.time.ToString("0.00") + "]: " + message);
        }

        internal static void Log_Debug(this UnityEngine.Object obj, string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log(obj.GetType().FullName + "[" + obj.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        internal static void Log_Debug(this System.Object obj, string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log(obj.GetType().FullName + "[" + obj.GetHashCode().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        internal static void Log_Debug(string context, string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log(context + "[][" + Time.time.ToString("0.00") + "]: " + message);
        }

        internal static void Log_Debug(string message)
        {
            DFSettings DFsettings = DeepFreeze.Instance.DFsettings;
            if (DFsettings.debugging)
                Debug.Log("[DeepFreeze][" + Time.time.ToString("0.00") + "]: " + message);
        }
    }
}