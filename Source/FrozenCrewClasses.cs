/**
 * FrozenCrewClasses.cs
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
using System.Collections.Generic;

namespace DF
{
    public class FrznCrewMbr
    {
        public string CrewName { get; set; }
        public int SeatIdx { get; set; }
        public Guid VesselID { get; set; }
        public string VesselName { get; set; }

        public FrznCrewMbr(string crewName, int seat, Guid vessel, string VesselName)
        {
            CrewName = crewName;
            SeatIdx = seat;
            VesselID = vessel;
            this.VesselName = VesselName;
        }
    }

    public class FrznCrewList : List<FrznCrewMbr>
    {
    }

    public enum FrzrTmpStatus
    {
        OK = 0,
        WARN = 1,
        RED = 2
    }
}