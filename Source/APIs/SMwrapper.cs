﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace DF
{
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // BELOW HERE SHOULD NOT BE EDITED - this links to the loaded ShipManifest module without requiring a Hard Dependancy
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The Wrapper class to access ShipManifest from another plugin
    /// </summary>
    public class SMWrapper
    {
        protected static Type SMType;
        protected static Type TransferCrewType;
        protected static Object actualSM;

        /// <summary>
        /// This is the ShipManifest object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static SMAPI ShipManifestAPI;
        
        /// <summary>
        /// Whether we found the ShipManifest assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return SMType != null && TransferCrewType != null; } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return ShipManifestAPI != null; } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _SMWrapped;
        
        /// <summary>
        /// Whether the object has been wrapped and the APIReady flag is set in the real ShipManifest
        /// </summary>
        public static Boolean SMAPIReady { get { return _SMWrapped; } }
        
        /// <summary>
        /// This method will set up the ShipManifest object and wrap all the methods/functions
        /// </summary>
        /// <returns>
        /// Bool indicating success of call
        /// </returns>
        public static Boolean InitSMWrapper()
        {
            try
            {
                //reset the internal objects
                _SMWrapped = false;
                actualSM = null;
                ShipManifestAPI = null;
                LogFormatted_DebugOnly("Attempting to Grab ShipManifest Types...");

                //find the base type
                SMType = getType("ShipManifest.SMAddon"); 

                if (SMType == null)
                {
                    return false;
                }

                LogFormatted("ShipManifest Version:{0}", SMType.Assembly.GetName().Version.ToString());

                //now the KerbalInfo Type
                TransferCrewType = getType("ShipManifest.Process.TransferCrew");
                
                if (TransferCrewType == null)
                {
                    return false;
                }

                //now grab the running instance
                LogFormatted_DebugOnly("Got Assembly Types, grabbing Instance");
                try
                {
                    actualSM = SMType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                }
                catch (Exception)
                {
                    LogFormatted("No Instance found - most likely you have an old ShipManifest installed");
                    return false;
                }
                if (actualSM == null)
                {
                    LogFormatted("Failed grabbing SMAddon Instance");
                    return false;
                }

                //If we get this far we can set up the local object and its methods/functions
                LogFormatted_DebugOnly("Got Instance, Creating Wrapper Objects");
                ShipManifestAPI = new SMAPI(actualSM);
                _SMWrapped = true;
                return true;
            }
            catch (Exception ex)
            {
                LogFormatted("Unable to setup InitSMrapper Reflection");
                LogFormatted("Exception: {0}", ex);
                _SMWrapped = false;
                return false;
            }
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
        /// The API Class that is an analogue of the real ShipManifest. This lets you access all the API-able properties and Methods of the ShipManifest
        /// </summary>
        public class SMAPI
        {
            internal SMAPI(Object a)
            {
                try
                {
                    //store the actual object
                    actualSMAPI = a;

                    //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                    //for events we also add a handler

                    LogFormatted_DebugOnly("Getting TransferCrew Instance");
                    TransferCrewMethod = SMType.GetMethod("get_CrewTransferProcess", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    actualCrewTransfer = getCrewTransferProcess;
                    LogFormatted_DebugOnly("Success: " + (TransferCrewMethod != null));
                    LogFormatted_DebugOnly("Getting CrewProcessOn Instance");
                    CrewProcessOnMethod = SMType.GetMethod("get_CrewProcessOn", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (CrewProcessOnMethod != null));
                    LogFormatted_DebugOnly("Getting getCrewXferActiveMethod");
                    getCrewXferActiveMethod = TransferCrewType.GetMethod("get_CrewXferActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getCrewXferActiveMethod != null));
                    LogFormatted_DebugOnly("Getting setCrewXferActiveMethod");
                    setCrewXferActiveMethod = TransferCrewType.GetMethod("set_CrewXferActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (setCrewXferActiveMethod != null));
                    LogFormatted_DebugOnly("Getting getIsStockXferMethod");
                    getIsStockXferMethod = TransferCrewType.GetMethod("get_IsStockXfer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getIsStockXferMethod != null));
                    LogFormatted_DebugOnly("Getting getOverrideStockCrewXferMethod");
                    getOverrideStockCrewXferMethod = TransferCrewType.GetMethod("get_OverrideStockCrewXfer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getOverrideStockCrewXferMethod != null));
                    LogFormatted_DebugOnly("Getting getCrewXferDelaySecMethod");
                    getCrewXferDelaySecMethod = TransferCrewType.GetMethod("get_CrewXferDelaySec", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getCrewXferDelaySecMethod != null));
                    LogFormatted_DebugOnly("Getting getIsSeat2SeatXferMethod");
                    getIsSeat2SeatXferMethod = TransferCrewType.GetMethod("get_IsSeat2SeatXfer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getIsSeat2SeatXferMethod != null));
                    LogFormatted_DebugOnly("Getting getSeat2SeatXferDelaySecMethod");
                    getSeat2SeatXferDelaySecMethod = TransferCrewType.GetMethod("get_Seat2SeatXferDelaySec", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getSeat2SeatXferDelaySecMethod != null));
                    LogFormatted_DebugOnly("Getting getFromSeatMethod");
                    getFromSeatMethod = TransferCrewType.GetMethod("get_FromSeat", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getFromSeatMethod != null));
                    LogFormatted_DebugOnly("Getting getToSeatMethod");
                    getToSeatMethod = TransferCrewType.GetMethod("get_ToSeat", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getToSeatMethod != null));
                    LogFormatted_DebugOnly("Getting getXferVesselIdMethod");
                    getXferVesselIdMethod = TransferCrewType.GetMethod("get_XferVesselId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getXferVesselIdMethod != null));
                    LogFormatted_DebugOnly("Getting getIvaDelayActiveMethod");
                    getIvaDelayActiveMethod = TransferCrewType.GetMethod("get_IvaDelayActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getIvaDelayActiveMethod != null));
                    LogFormatted_DebugOnly("Getting getIvaPortraitDelayMethod");
                    getIvaPortraitDelayMethod = TransferCrewType.GetMethod("get_IvaPortraitDelay", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getIvaPortraitDelayMethod != null));
                    LogFormatted_DebugOnly("Getting getFromPartMethod");
                    getFromPartMethod = TransferCrewType.GetMethod("get_FromPart", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getFromPartMethod != null));
                    LogFormatted_DebugOnly("Getting getToPartMethod");
                    getToPartMethod = TransferCrewType.GetMethod("get_ToPart", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getToPartMethod != null));
                    LogFormatted_DebugOnly("Getting getFromCrewMemberMethod");
                    getFromCrewMemberMethod = TransferCrewType.GetMethod("get_FromCrewMember", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getFromCrewMemberMethod != null));
                    LogFormatted_DebugOnly("Getting getToCrewMemberMethod");
                    getToCrewMemberMethod = TransferCrewType.GetMethod("get_ToCrewMember", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    LogFormatted_DebugOnly("Success: " + (getToCrewMemberMethod != null));
                }

                catch (Exception ex)
                {
                    LogFormatted("Unable to Instantiate SMAPI object using Reflection");
                    LogFormatted("Exception: {0}", ex);
                }
            }

            private Object actualSMAPI;
            private Object actualCrewTransfer;

            /// <summary>
            /// True if a crewXfer on the Active Vessel is currently active
            /// </summary>
            public bool CrewProcessOn;

            private MethodInfo CrewProcessOnMethod;
            private bool getCrewProcessOn
            {
                get { return (bool)CrewProcessOnMethod.Invoke(actualSMAPI, null); }
            }
            
            private MethodInfo TransferCrewMethod;
            /// <summary>
            /// Get the actual CrewTransfer object from the SMAddon Instance
            /// </summary>
            /// <returns>
            /// Object reference to the CrewTransfer Instance
            /// </returns>
            public object getCrewTransferProcess
            {
                get
                {
                    if (TransferCrewMethod == null)
                    {
                        return null;
                    }  

                    return TransferCrewMethod.Invoke(actualSMAPI, null);
                }
            }
            
            private MethodInfo getCrewXferActiveMethod;
            private MethodInfo setCrewXferActiveMethod;
            /// <summary>
            /// True if a crewXfter on the Active Vessel is currently active
            /// If you set it to False whilst a Xfer is active it will cancel the current transfer
            /// </summary> 
            /// <returns>
            /// Bool
            /// </returns>
            public bool CrewXferActive
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;   
                    return (bool)getCrewXferActiveMethod.Invoke(actualCrewTransfer, null);
                }
                set
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    setCrewXferActiveMethod.Invoke(actualCrewTransfer, new Object[] { value });
                }
            }

            private MethodInfo getIsStockXferMethod;
            /// <summary>
            /// This flag indicates if the transfer was initiated by the Stock Crew Transfer interface. 
            /// It stays enabled during the Crew Transfer Process (just like CrewXferActive)
            /// </summary> 
            /// <returns>
            /// Bool
            /// </returns>
            public bool IsStockXfer
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (bool)getIsStockXferMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getOverrideStockCrewXferMethod;
            /// <summary>
            /// This flag indicates if SM is overriding the Stock Crew Transfer process.
            /// </summary> 
            /// <returns>
            /// Bool
            /// </returns>
            public bool OverrideStockCrewXfer
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (bool)getOverrideStockCrewXferMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getCrewXferDelaySecMethod;
            /// <summary>
            /// This is the number of seconds delay used for the transfer in progress
            /// </summary> 
            /// <returns>
            /// Double
            /// </returns>
            public double CrewXferDelaySec
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (double)getCrewXferDelaySecMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getIsSeat2SeatXferMethod;
            /// <summary>
            /// This flag indicates if the transfer is Seat 2 seat, i.e. within the same part.
            /// </summary> 
            /// <returns>
            /// Bool
            /// </returns>
            public bool IsSeat2SeatXfer
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (bool)getIsSeat2SeatXferMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getSeat2SeatXferDelaySecMethod;
            /// <summary>
            /// This is the number of seconds used for Seat2seat transfer delays
            /// </summary> 
            /// <returns>
            /// Double
            /// </returns>
            public double Seat2SeatXferDelaySec
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (double)getSeat2SeatXferDelaySecMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getFromSeatMethod;
            /// <summary>
            /// This is the part seat where the kerbal is being moved from. In the case of parts with no internal view, it can be null.
            /// </summary> 
            /// <returns>
            /// InternalSeat
            /// </returns>
            public InternalSeat FromSeat
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (InternalSeat)getFromSeatMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getToSeatMethod;
            /// <summary>
            /// This is the part seat where the kerbal is being moved to. In the case of parts with no internal view, it can be null.
            /// </summary> 
            /// <returns>
            /// internalSeat
            /// </returns>
            public InternalSeat ToSeat
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (InternalSeat)getToSeatMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getXferVesselIdMethod;
            /// <summary>
            /// This is the vessel id for the transfer in question
            /// </summary> 
            /// <returns>
            /// Guid
            /// </returns>
            public Guid XferVesselId
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (Guid)getXferVesselIdMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getIvaDelayActiveMethod;
            /// <summary>
            /// This flag indicates the IVA delay is active. This means that the transfer has occurred (kerbals have actually moved) and we are cleaning up portraits.
            /// </summary> 
            /// <returns>
            /// Bool
            /// </returns>
            public bool IvaDelayActive
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (bool)getIvaDelayActiveMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getIvaPortraitDelayMethod;
            /// <summary>
            /// This stores the number of frames that have passed since transfer has completed. 
            /// In order for the portraits to update properly, a wait period is needed to allow for unity/ksp async callbacks to complete after crew are moved. 
            /// This is currently hard coded at 20 Frames (update cycles). I then fire an OnVesselChanged event to refresh the portraits.
            /// </summary> 
            /// <returns>
            /// int
            /// </returns>
            public int IvaPortraitDelay
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (int)getIvaPortraitDelayMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getFromPartMethod;
            /// <summary>
            /// This is the part from which the kerbal is being transferred.
            /// </summary> 
            /// <returns>
            /// Part
            /// </returns>
            public Part FromPart
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (Part)getFromPartMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getToPartMethod;
            /// <summary>
            /// This is the part to which the kerbal is being transferred. It can be the same as the source (Seat2Seat transfers).
            /// </summary> 
            /// <returns>
            /// Part
            /// </returns>
            public Part ToPart
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (Part)getToPartMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getFromCrewMemberMethod;
            /// <summary>
            /// This is the crew member being transferred.
            /// </summary> 
            /// <returns>
            /// ProtoCrewMember
            /// </returns>
            public ProtoCrewMember FromCrewMember
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (ProtoCrewMember)getFromCrewMemberMethod.Invoke(actualCrewTransfer, null);
                }
            }

            private MethodInfo getToCrewMemberMethod;
            /// <summary>
            /// This is the crew member that would be swapped if the target seat is occupied. can be null.
            /// </summary>
            /// <returns>
            /// protoCrewMember
            /// </returns> 
            public ProtoCrewMember ToCrewMember
            {
                get
                {
                    if (actualCrewTransfer == null)
                        actualCrewTransfer = getCrewTransferProcess;
                    return (ProtoCrewMember)getToCrewMemberMethod.Invoke(actualCrewTransfer, null);
                }
            }
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