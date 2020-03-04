/*
 * DeepFreeze Continued...
 * (C) Copyright 2015, Jamie Leighton
 *
 * Kerbal Space Program is Copyright(C) 2013 Squad.See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of JPLRepo's DeepFreeze (continued...) - a Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Continues to be licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license.See<https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RSTUtils;

namespace DF
{
    public class DFExtDoorMgr : InternalModule
    {
        private DeepFreezer Freezer;

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
            {
                if (Freezer == null)
                {
                    Freezer = part.FindModuleImplementing<DeepFreezer>();
                    Utilities.Log_Debug("DFExtDoorMgr OnUpdate Set part " + part.name);
                }
            }
        }

        public void ButtonExtDoor(bool state)
        {
            if (Freezer == null)
            {
                Freezer = part.FindModuleImplementing<DeepFreezer>();
                Utilities.Log_Debug("DFExtDoorMgr buttonExtDoorState set part " + part.name);
            }
            if (Freezer == null) return; // If freezer is still null just return
            if (!Freezer.ExternalDoorActive) return;  // if freezer doesn't have an external door just return.

            if (Freezer._externaldoorstate == DoorState.OPEN)
            {
                //Door is open so we trigger a closedoor.
                Freezer.eventCloseDoors();
                Utilities.Log_Debug("DFExtDoorMgr ButtonExtDoor fired triggered eventCloseDoors");
            }
            else
            {
                if (Freezer._externaldoorstate == DoorState.CLOSED)
                {
                    //Door is closed so we trigger a opendoor.
                    Freezer.eventOpenDoors();
                    Utilities.Log_Debug("DFExtDoorMgr ButtonExtDoor fired triggered eventOpenDoors");
                }
                else
                {
                    // door already opening or closing...
                    Utilities.Log_Debug("DFExtDoorMgr ButtonExtDoor fired but door state is opening, closing or unknown");
                }
            }
        }

        public bool ButtonExtDoorState()
        {
            // Utilities.Log_Debug("DFExtDoorMgr ButtonExtDoorState fired");
            if (Freezer == null)
            {
                Freezer = part.FindModuleImplementing<DeepFreezer>();
                Utilities.Log_Debug("DFExtDoorMgr buttonExtDoorState set part " + part.name);
            }
            if (Freezer == null) return false; // if freezer still null return false
            if (!Freezer.ExternalDoorActive) return false; // if freezer doesn't have an external door just return.
            if (Freezer._externaldoorstate == DoorState.CLOSED || Freezer._externaldoorstate == DoorState.CLOSING || Freezer._externaldoorstate == DoorState.UNKNOWN)
            {
                Utilities.Log_Debug("DFExtDoorMgr Door is closed or closing or unknown return state false");
                return false;
            }
            Utilities.Log_Debug("DFExtDoorMgr Door is open or opening return state true");
            return true;
        }
    }
}
