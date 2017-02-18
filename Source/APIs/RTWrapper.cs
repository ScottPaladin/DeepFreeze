﻿/**
 * DeepFreeze Continued...
 * (C) Copyright 2015, Jamie Leighton
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of JPLRepo's DeepFreeze (continued...) - a Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton based of code and concepts from the Kerbal Alarm Clock Mod. Which was licensed under the MIT license.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Continues to be licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace DF
{
    /// <summary>
    /// The Wrapper class to access Remote Tech
    /// </summary>
    public class RTWrapper
    {
        protected static Type RTAPIType;
        protected static Object actualRTAPI;

        /// <summary>
        /// This is the Remote Tech API object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static RTAPI RTactualAPI;

        /// <summary>
        /// Whether we found the Remote Tech API assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return RTAPIType != null; } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return actualRTAPI != null; } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _RTWrapped;

        /// <summary>
        /// Whether the object has been wrapped
        /// </summary>
        public static Boolean APIReady { get { return _RTWrapped; } }

        /// <summary>
        /// This method will set up the Remote Tech object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitTRWrapper()
        {
            //reset the internal objects
            _RTWrapped = false;
            actualRTAPI = null;
            LogFormatted_DebugOnly("Attempting to Grab Remote Tech Types...");

            //find the base type
            RTAPIType = getType("RemoteTech.API.API"); 

            if (RTAPIType == null)
            {
                return false;
            }

            LogFormatted("Remote Tech Version:{0}", RTAPIType.Assembly.GetName().Version.ToString());

            //now grab the running instance
            LogFormatted_DebugOnly("Got Assembly Types, grabbing Instances");
            try
            {
                actualRTAPI = RTAPIType.GetMember("HasLocalControl", BindingFlags.Public | BindingFlags.Static);
            }
            catch (Exception)
            {
                LogFormatted("No Remote Tech isInitialised found");
                //throw;
            }

            if (actualRTAPI == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted_DebugOnly("Got Instance, Creating Wrapper Objects");
            RTactualAPI = new RTAPI(actualRTAPI);

            _RTWrapped = true;
            return true;
        }

        internal static Type getType(string name)
        {
            Type type = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>

            {
                if (t.FullName == name)
                    type = t;
            }
            );

            if (type != null)
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// The Type that is an analogue of the real Remote Tech. This lets you access all the API-able properties and Methods of Remote Tech
        /// </summary>
        public class RTAPI
        {
            internal RTAPI(Object actualRT)
            {
                //store the actual object
                APIactualRT = actualRT;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE
                //Methods
                LogFormatted_DebugOnly("Getting HasLocalControl Method");
                HasLocalControlMethod = RTAPIType.GetMethod("HasLocalControl", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (HasLocalControlMethod != null));

                LogFormatted_DebugOnly("Getting HasAnyConnection Method");
                HasAnyConnectionMethod = RTAPIType.GetMethod("HasAnyConnection", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (HasAnyConnectionMethod != null));

                LogFormatted_DebugOnly("Getting GetShortestSignalDelay Method");
                GetShortestSignalDelayMethod = RTAPIType.GetMethod("GetShortestSignalDelay", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (GetShortestSignalDelayMethod != null));
            }

            private Object APIactualRT;

            #region Methods

            private MethodInfo HasLocalControlMethod;

            /// <summary>
            /// Whether the current vessel HasLocalControl
            /// </summary>
            /// <param name="id">The vessel id reference</param>
            /// <returns>Success of call</returns>
            internal bool HasLocalControl(Guid id)
            {
                try
                {
                    return (bool)HasLocalControlMethod.Invoke(APIactualRT, new Object[] { id });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Remote Tech HasLocalControl Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            private MethodInfo HasAnyConnectionMethod;

            /// <summary>
            /// Whether the current vessel HasAnyConnection
            /// </summary>
            /// <param name="id">The vessel id reference</param>
            /// <returns>Success of call</returns>
            internal bool HasAnyConnection(Guid id)
            {
                try
                {
                    return (bool)HasAnyConnectionMethod.Invoke(APIactualRT, new Object[] { id });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Remote Tech HasAnyConnection Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            private MethodInfo GetShortestSignalDelayMethod;

            /// <summary>
            /// Gets the signal delay
            /// </summary>
            /// <param name="id">The vessel id reference</param>
            /// <returns>A double indicating the signaldelay time</returns>
            internal double GetShortestSignalDelay(Guid id)
            {
                try
                {
                    return (double)GetShortestSignalDelayMethod.Invoke(APIactualRT, new Object[] { id });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Remote Tech GetShortestSignalDelay Method");
                    LogFormatted("Exception: {0}", ex);
                    return 0;
                    //throw;
                }
            }

            #endregion Methods
        }

        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            if (RSTUtils.Utilities.debuggingOn)
                LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params Object[] strParams)
        {
            Message = String.Format(Message, strParams);
            String strMessageLine = String.Format("{0},{2}-{3},{1}",
                DateTime.Now, Message, Assembly.GetExecutingAssembly().GetName().Name,
                MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.Log(strMessageLine);
        }

        #endregion Logging Stuff
    }
}