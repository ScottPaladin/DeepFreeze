/**
 * DeepFreezerPart.cs
 *
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of Jamie Leighton's Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  The original DeepFreeze was licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 *  This File was part of the original Deepfreeze but has been heavily modified and re-written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Which is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DF
{
    public class DeepFreezer : PartModule
    {
        private float lastUpdate = 0.0f;
        private float lastRemove = 0.0f;
        private float updatetnterval = .5f;
        public bool crewXferTOActive = false;     // true if a crewXfer to this part is active
        public bool crewXferFROMActive = false;   // true if a crewXfer from this part is active        
        public ProtoCrewMember xfercrew;          // set to the crew kerbal during a crewXfer
        private Part xferfromPart;                // set to the from part during a crewXfer
        private Part xfertoPart;                  // set to the to part during a crewXfer                       
        private System.Random rnd = new System.Random(); // Random seed for Killing Kerbals when we run out of EC to keep the Freezer running.

        [KSPField(isPersistant = true, guiActive = false, guiName = "FC")] //This string value is the names of frozen crew, it is turned into a list called StoredCrew during loaded. We keep this string current and it get's saved to the persistant.sfs on save.
        public string FrozenCrew;

        [KSPField(isPersistant = true, guiActive = false, guiName = "Freezer Size")] //Total Size of Freezer, get's read from part.cfg.
        public int FreezerSize;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Total Frozen Kerbals")] //WISOTT Total number of frozen kerbals, just a count of the list object.
        public int TotalFrozen;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Freezer Space")] //Total space available for storage. Set by Part.cfg file.
        public int FreezerSpace;

        [KSPField(isPersistant = true)]
        public bool IsCrewableWhenFull; //Set by part.cfg. Intended to set whether a kerbal can enter the part when the frozen storage is filled up. Especially important for the single kerbal part.

        [KSPField(isPersistant = true)]
        public float timeSinceLastECtaken; //This is the game time since EC as taken, for the ongoing EC usage while kerbal's are frozen

        [KSPField(isPersistant = true)]
        public Int32 FrznChargeRequired; //Set by part.cfg. Total EC value required to maintain a frozen kerbal per minute.

        [KSPField()]                     // set to active while freezing a kerbal
        public bool IsFreezeActive;

        [KSPField()]                     // set to active while thawing a kerbal
        public bool IsThawActive;

        [KSPField()]
        public double StoredCharge;      // Stores up EC as we are freezing or thawing over time until we reach what we need.

        [KSPField(isPersistant = true)]
        public Int32 ChargeRequired; //Set by part.cfg. Total EC value required for a complete freeze or thaw.

        [KSPField(isPersistant = true)]
        public Int32 ChargeRate; //Set by part.cfg. EC draw per tick.

        private ProtoCrewMember ActiveKerbal;
        private string ToThawKerbal;
         
        public class FrznCrewMbr
        {
            public string CrewName { get; set; }
            public int Seat { get; set; }

            public FrznCrewMbr(string crewName, int seat)
            {
                
                this.CrewName = crewName;
                this.Seat = seat;                
            }

        }
        public class FrznCrewList : List<FrznCrewMbr>
        {
            
            public string Serialize()
            {
                string tmpstring = "";
                Utilities.Log_Debug("Freezer", "Serialize # of crew =" + this.Count());
                foreach (FrznCrewMbr crew in this)
                {
                    tmpstring += crew.CrewName + "," + crew.Seat.ToString() + "~";
                    Utilities.Log_Debug("Freezer", "Serialized string = " + tmpstring);
                }
                return tmpstring;
            }

            public void Deserialize(string str)
            {
                Utilities.Log_Debug("Freezer", "DeSerialize on load");
                this.Clear();
                List<string> spltcrew = new List<string>();                   
                spltcrew = str.Split('~').ToList();
                foreach (string strcrew in spltcrew)
                {
                    if (strcrew.Length > 0)
                    {
                        string[] arr = strcrew.Split(',');
                        string crewName = arr[0];
                        string strseat = arr[1];
                        int seat = 0;
                        bool prse = int.TryParse(strseat, out seat);

                        FrznCrewMbr newcrew = new FrznCrewMbr(crewName, seat);                        
                        this.Add(newcrew);
                        Utilities.Log_Debug("Freezer", "Added crew =" + crewName + " seat=" + seat);
                    }                    
                }
                this.DmpStCrwLst();
                Utilities.Log_Debug("Freezer", "DeSerialize completed");
            }

            public void DmpStCrwLst()
            {
                Utilities.Log_Debug("Freezer", "DmpStCrwLst");
                if (this.Count() == 0)
                    Utilities.Log_Debug("Freezer", "List empty");
                foreach (FrznCrewMbr lst in this)
                {
                    Utilities.Log_Debug("Freezer", "Name = " + lst.CrewName + " Seat= " + lst.Seat);
                }
            }

        }
        public FrznCrewList StoredCrewList = new FrznCrewList();

        protected AudioSource hatch_lock;
        protected AudioSource ice_freeze;
        protected AudioSource machine_hum;
        protected AudioSource ding_ding;

        public override void OnUpdate()
        {
            if ((Time.time - lastUpdate) > updatetnterval && (Time.time - lastRemove) > updatetnterval) // We only update every updattnterval time interval.
            {
                lastUpdate = Time.time;
                UpdateEvents(); // Update the Freeze/Thaw Events that are attached to this Part.

                //This whole next section is pointless, and just debugging messages helping me develop the Mod.
                if (this.part.internalModel == null)
                {
                    Utilities.Log_Debug("Freezer", "Part has no internal model");
                }
                else
                {                    
                    Utilities.Log_Debug("Freezer", "Part has " + this.part.internalModel.seats.Count + " seats");
                    foreach (InternalSeat seat in this.part.internalModel.seats)
                    {
                        string logmsg = "SeatXformName=" + seat.seatTransformName + " Taken?= " + seat.taken;
                        if (seat.kerbalRef != null)
                        {
                            logmsg += " KerbalRefName=" + seat.kerbalRef.name;
                        }
                        else
                            logmsg += " KerbalRefName=null";
                        if (seat.crew != null)
                        {
                            logmsg += " CrewName=" + seat.crew.name + " type=" + seat.crew.type + " roststs=" + seat.crew.rosterStatus;
                        }
                        else
                            logmsg += " Crew=null";                            
                        Utilities.Log_Debug("Freezer", logmsg);
                    }
                    Utilities.Log_Debug("Freezer", "Available Seat count =" + this.part.internalModel.GetAvailableSeatCount());
                    //Utilities.Log_Debug("Freezer", "Next Available Seat=" + this.part.internalModel.GetNextAvailableSeat().seatTransformName + " indx =" + this.part.internalModel.GetNextAvailableSeatIndex());

                    
                }
                StoredCrewList.DmpStCrwLst();
                // End of the pointless debugging section is here.
                                
            }
            
            // The follow section of code consumes EC when we have ECreqdForFreezer set to true in the part config.
            // This consumes electric charge when we have frozen kerbals on board.
            // But due to bugs in KSP ith EC and SolarPanels at high timewarp if timewarp is > 4x we turn it off.
            // If we run out of EC we roll the dice. There is a 1 in 3 chance a Kerbal will DIE!!!!
            if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && timewarpIsValid)
            {
                if ((Time.time - timeSinceLastECtaken) > updatetnterval && TotalFrozen > 0) //We have frozen Kerbals, consume EC
                {
                    float crnttime = Time.time;
                    float timeperiod = crnttime - timeSinceLastECtaken;
                    double ECreqd = ((FrznChargeRequired / 60.0f) * timeperiod * TotalFrozen);
                    Debug.Log("Run the freezer parms crnttime =" + crnttime + " timeperiod =" + timeperiod + " ecreqd =" + ECreqd);
                    if (requireResource(vessel, "ElectricCharge", ECreqd, true))
                    {
                        //Have resource
                        Debug.Log("Consumed Freezer EC");
                    }
                    else
                    {
                        //don't have resource
                        Debug.Log("Ran out of EC to run the freezer, roll the dice... ");
                        ScreenMessages.PostScreenMessage("Insufficient electric charge to monitor frozen kerbals. They are going to die!!", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                        int dice = rnd.Next(1, 3);  // Change this dice to increase or decrease the odds of a Kerbal Dying
                        if (dice == 2) // Change this test to increase or decrease the odds of a Kerbal Dying
                        {
                            //a kerbal dies
                            int dice2 = rnd.Next(1, StoredCrewList.Count); // Randomly select a Kerbal to kill.
                            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == StoredCrewList[dice2].CrewName);
                            if (kerbal == null)
                            {
                                Debug.Log("DeepFreeze Tried to Kill a frozen kerbal but couldn't find them. This should never happen. Please report this to mod owner");
                            }
                            else
                            {
                                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                                ScreenMessages.PostScreenMessage(kerbal.name + " died due to lack of Electrical Charge to run cryogenics", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                                StoredCrewList.RemoveAt(dice2);
                            }
                        }
                    }
                    timeSinceLastECtaken = crnttime;
                }
            }
        }

        public void FixedUpdate()
        {
            if (IsFreezeActive == true)
            {
                if (!requireResource(vessel, "ElectricCharge", ChargeRate, false) == true)
                {
                    ScreenMessages.PostScreenMessage("Insufficient electric charge to freeze kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    FreezeKerbalAbort(ActiveKerbal);
                    return;
                }
                else
                {
                    requireResource(vessel, "ElectricCharge", ChargeRate, true);
                    StoredCharge = StoredCharge + ChargeRate;
                    //Debug.Log("Drawing Charge");
                    if (StoredCharge > ChargeRequired)
                    {
                        if (requireResource(vessel, "Glykerol", 5, true))
                        {
                            FreezeKerbalConfirm(ActiveKerbal);
                        }
                        else
                        {
                            FreezeKerbalAbort(ActiveKerbal);
                        }
                    }
                }
            }
            if (IsThawActive == true)
            {
                if (!requireResource(vessel, "ElectricCharge", ChargeRate, false))
                {
                    ScreenMessages.PostScreenMessage("Insufficient electric charge to thaw kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    ThawKerbalAbort(ToThawKerbal);
                }
                else
                {
                    requireResource(vessel, "ElectricCharge", ChargeRate, true);
                    StoredCharge = StoredCharge + ChargeRate;
                    if (StoredCharge > ChargeRequired)
                    {
                        ThawKerbalConfirm(ToThawKerbal);
                    }
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("DeepFreezer onLoad");
            //ChargeRequired = 3000;
            //ChargeRate = 20;
            Int32.TryParse(node.GetValue("ChargeRequired"), out ChargeRequired);
            Int32.TryParse(node.GetValue("ChargeRate"), out ChargeRate);
            IsCrewableWhenFull = Convert.ToBoolean(node.GetValue("IsCrewableWhenFull"));
            FrozenCrew = node.GetValue("FrozenCrew");
            Debug.Log("Frozen Crew =" + FrozenCrew);
            //LoadFrozenCrew(); 
            StoredCrewList.Clear();
            if (FrozenCrew != "" && FrozenCrew != null)
                StoredCrewList.Deserialize(FrozenCrew);            
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state != StartState.None || state != StartState.Editor)
            {
                GameEvents.onCrewTransferred.Add(this.OnCrewTransferred);
                GameEvents.onCrewBoardVessel.Add(this.onCrewBoardVessel);
            }

            hatch_lock = gameObject.AddComponent<AudioSource>();
            hatch_lock.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/hatch_lock");
            hatch_lock.volume = .5F;
            hatch_lock.panLevel = 0;
            hatch_lock.rolloffMode = AudioRolloffMode.Linear;
            hatch_lock.Stop();
            ice_freeze = gameObject.AddComponent<AudioSource>();
            ice_freeze.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/ice_freeze");
            ice_freeze.volume = 1;
            ice_freeze.panLevel = 0;
            ice_freeze.rolloffMode = AudioRolloffMode.Linear;
            ice_freeze.Stop();
            machine_hum = gameObject.AddComponent<AudioSource>();
            machine_hum.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/machine_hum");
            machine_hum.volume = .1F;
            machine_hum.panLevel = 0;
            machine_hum.rolloffMode = AudioRolloffMode.Linear;
            machine_hum.Stop();
            ding_ding = gameObject.AddComponent<AudioSource>();
            ding_ding.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/ding_ding");
            ding_ding.volume = .25F;
            ding_ding.panLevel = 0;
            ding_ding.rolloffMode = AudioRolloffMode.Linear;
            ding_ding.Stop();
        }

        public void OnDestroy()
        {
            GameEvents.onCrewTransferred.Remove(this.OnCrewTransferred);
            GameEvents.onCrewBoardVessel.Remove(this.onCrewBoardVessel);
        }

        public override void OnSave(ConfigNode node)
        {
            FrozenCrew = StoredCrewList.Serialize();
            node.SetValue("FrozenCrew", FrozenCrew);      
            Debug.Log("OnSave: " + node);
        }

        /*
        public override void OnInactive()
        {
            Debug.Log("OnInactive " + FrozenCrew);
            if (IsCrewableWhenFull)
            {
                part.CrewCapacity = StoredCrew.Count +1;
            }
            else
            {
                part.CrewCapacity = StoredCrew.Count;
            }
            
            //part.CrewCapacity = StoredCrew.Count;
            foreach (var crewmember in StoredCrew)
            {
                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (kerbal.name == crewmember)
                        part.AddCrewmember(kerbal);
                }
                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned)
                {
                    if (kerbal.name == crewmember)
                        part.AddCrewmember(kerbal);
                }
            }
        }
         * */
        
        #region Events

        private void UpdateEvents()
        {
            //Debug.Log("UpdateEvents");
            UpdateCounts();

            // If a CrewXferFROM this part is Active we need to check when it is finished and then remove the FreezeEvent fo the crew that just left.
            if (crewXferFROMActive)
            {
                //Debug.Log("Crew Xfer Active, chcking if complete");
                ProtoCrewMember crewfered = xfertoPart.protoModuleCrew.FirstOrDefault(a => a.name == xfercrew.name);
                if (crewfered != null)
                {
                    Utilities.Log_Debug("Freezer", "CrewXferFROM Completed");                    
                    removeFreezeEvent(xfercrew);
                    crewXferFROMActive = false;
                }
            }

            // If a CrewXferTO this part is Active we need to check when it is finished             
            // These variables are set in OnCrewTransferred.
            if (crewXferTOActive)
            {
                ProtoCrewMember crewfered = xfertoPart.protoModuleCrew.FirstOrDefault(a => a.name == xfercrew.name);
                if (crewfered != null)
                {
                    Utilities.Log_Debug("Freezer", "CrewXferTO Completed");                                       
                    crewXferTOActive = false;
                }
            }

            // If we aren't Thawing or Freezing a kerbal right now, and no crewXfer i active we check all the events.
            if (!IsThawActive && !IsFreezeActive && !crewXferFROMActive && !crewXferTOActive)
            {
                foreach (BaseEvent itemX in Events) // Iterate through all Events
                {                    
                    string[] subStrings = itemX.name.Split(' ');
                    if (subStrings[0] == "Freeze") // If it's a Freeze Event
                    {
                        string crewname = "";
                        crewname = subStrings[1] + " " + subStrings[2];   
                        if (part.protoModuleCrew.FirstOrDefault(a => a.name == crewname) == null) // Search the part for the crewmember.
                        // We didn't find the crewmember so remove the Freeze Event.
                        {                            
                            ProtoCrewMember fndcrew = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == crewname);
                            removeFreezeEvent(fndcrew);
                        }                                                  
                    }
                }                                
                if (StoredCrewList.Count < FreezerSize) // If the Freezer isn't full
                {
                    foreach (var CrewMember in part.protoModuleCrew) // We Add Freeze Events for all active crew in the part
                    {
                        addFreezeEvent(CrewMember);
                    }
                }
                if ((part.protoModuleCrew.Count < part.CrewCapacity) || part.CrewCapacity <= 0)  // If part is not full or zero (should always be true, think this is redundant line)              
                { 
                    foreach (var frozenkerbal in StoredCrewList) // We add a Thaw Event for every frozenkerbal.
                    {
                        addThawEvent(frozenkerbal.CrewName);
                    }
                }
            }            
        }
              
        private void addFreezeEvent(ProtoCrewMember CrewMember)
        {            
            BaseEvent item = Events.Find(v => v.name == "Freeze " + CrewMember.name);  // Search to see if there isn't already a Freeze Event for this CrewMember                                
            if (item == null && CrewMember.type == ProtoCrewMember.KerbalType.Crew) // Did we find one? and CrewMember is type=Crew? if so, add new Event.
            //***** Could change this to Tourists as well but needs more changes.
            {                
                Events.Add(new BaseEvent(Events, "Freeze " + CrewMember.name, () =>
                {
                    beginFreezeKerbal(CrewMember);
                }, new KSPEvent { guiName = "Freeze " + CrewMember.name, guiActive = true }));
            }
        }

        private void removeFreezeEvent(ProtoCrewMember CrewMember)
        {
            try
            {                
                BaseEvent item = Events.Find(v => v.name == "Freeze " + CrewMember.name); // Find the Freeze event for the CrewMember                                
                if (item != null)
                {
                    Events.Remove(item); // Remove it
                    lastRemove = Time.time; // we check this time when we do updateevents because if it is done too quickly the GUI goes crazy
                    // There is probably a quicker way to do this, but it finds the GUI for the Part ActionWindow and sets it to dirty which forces Unity to re-draw it.
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part)
                        {
                            window.displayDirty = true;
                        }
                    }                    
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception removing Freeze Event for " + CrewMember.name);
                Debug.Log(ex.Message);
            }
        }

        private void addThawEvent(string frozenkerbal)
        {            
            BaseEvent item = Events.Find(v => v.name == "Thaw " + frozenkerbal); // Check a Thaw even doesn't already exist for this kerbal                     
            if (item == null) // No Item exists so add a new Thaw Event.
            {                
                Events.Add(new BaseEvent(Events, "Thaw " + frozenkerbal, () =>
                {
                    FrznCrewMbr tmpKerbal = StoredCrewList.Find(a => a.CrewName == frozenkerbal);
                    
                    if (tmpKerbal != null)
                    {
                        beginThawKerbal(frozenkerbal);
                    }
                }, new KSPEvent { guiName = "Thaw " + frozenkerbal, guiActive = true }));
            }
        }
        
        private void removeThawEvent(string frozenkerbal)
        {
            try
            {                
                BaseEvent item = Events.Find(v => v.name == "Thaw " + frozenkerbal);
                if (item != null)
                {
                    Events.Remove(item);  // Remove it
                    lastRemove = Time.time; // we check this time when we do updateevents because if it is done too quickly the GUI goes crazy
                    // There is probably a quicker way to do this, but it finds the GUI for the Part ActionWindow and sets it to dirty which forces Unity to re-draw it.
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part)
                        {
                            window.displayDirty = true;
                        }
                    }                    
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception removing Thaw Event for " + frozenkerbal);
                Debug.Log(ex.Message);
            }
        }

        #endregion Events

        #region FrzKerbals

        public void beginFreezeKerbal(ProtoCrewMember CrewMember)
        {
            if (FreezerSpace > 0 && part.protoModuleCrew.Contains(CrewMember)) // Freezer has space? and Part contains the CrewMember?
            {
                if (!requireResource(vessel, "Glykerol", 5, false)) // check we have Glykerol on board. 5 units per freeze event. This should be a part config item not hard coded.
                {
                    ScreenMessages.PostScreenMessage("Insufficient Glykerol to freeze kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                else // We have enough Glykerol
                {
                    if (DeepFreeze.Instance.SMInstalled) // Check if Ship Manifest (SM) is installed? 
                    {
                        if (IsSMXferRunning())  // SM is installed and is a Xfer running? If so we can't run a Freeze while a SMXfer is running.
                        {
                            ScreenMessages.PostScreenMessage("Cannot Freeze while Crew Xfer in progress.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            return;
                        }
                    }
                    if (crewXferFROMActive || crewXferTOActive)  // We can't run a freeze process if a crewXfer is active, this is catching Stock Xfers.
                    {
                        ScreenMessages.PostScreenMessage("Cannot Freeze while Crew Xfer in progress.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }    
                    FreezeKerbal(CrewMember); // Begin the Freezing Process                    
                }
            }
            else
            {
                if (FreezerSpace == 0)
                    ScreenMessages.PostScreenMessage("Cannot freeze kerbal. Freezer is full.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
                             
        public void FreezeKerbal(ProtoCrewMember CrewMember)
        {
            Utilities.Log_Debug("Freezer", "Freeze kerbal called");            
            int seat = CrewMember.seatIdx;
            part.RemoveCrewmember(CrewMember);  // remove the CrewMember from the part, because they are frozen, and this is the only way to trick the game.
            part.internalModel.seats[seat].taken = true; // Set their seat to Taken, because they are really still there. :)
            ActiveKerbal = CrewMember;  // set the Active Freeze Kerbal
            ActiveKerbal.seatIdx = seat; // Set their seat
            IsFreezeActive = true; // Set the Freezer actively freezing mode on
            ScreenMessages.PostScreenMessage("Starting Freeze", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            Utilities.Log_Debug("Freezer", "FrozenCrew =" + FrozenCrew);
            Utilities.Log_Debug("Freezer", "ActiveKerbal = " + ActiveKerbal.name + " Seat =" + ActiveKerbal.seatIdx);
            UpdateCounts();  // Update the Crew counts
            // Start sound effects
            hatch_lock.Play();
            machine_hum.Play();
            machine_hum.loop = true;
        }

        public void FreezeKerbalAbort(ProtoCrewMember CrewMember)
        {
            ScreenMessages.PostScreenMessage("Freezing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            part.internalModel.seats[CrewMember.seatIdx].taken = false; // Set their seat to NotTaken before we assign them back to their seat, not sure we really need this.            
            part.AddCrewmemberAt(CrewMember, CrewMember.seatIdx); // Add the CrewMember back into the part in their assigned seat.
            IsFreezeActive = false; // Turn the Freezer actively freezing mode off
            ActiveKerbal = null; // Set the Active Freeze Kerbal to null
            machine_hum.Stop(); // Stop the sound effects
        }

        public void FreezeKerbalConfirm(ProtoCrewMember CrewMember)
        {
            machine_hum.Stop(); // stop the sound effects
            StoredCharge = 0;  // Discharge all EC stored    
            // Add frozen kerbal details to the frozen kerbal list.
            FrznCrewMbr tmpcrew = new FrznCrewMbr(CrewMember.name, CrewMember.seatIdx);             
            StoredCrewList.Add(tmpcrew);            
            // Set our newly frozen Popsicle, er Kerbal, to Unowned type (usually a Crew) and Missing status.
            CrewMember.type = ProtoCrewMember.KerbalType.Unowned;
            CrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Missing;
            UpdateCounts();  // Update the Crew counts
            IsFreezeActive = false; // Turn the Freezer actively freezing mode off
            ActiveKerbal = null; // Set the Active Freeze Kerbal to null
            removeFreezeEvent(CrewMember);  // Remove the Freeze Event for this kerbal.
            ScreenMessages.PostScreenMessage(CrewMember.name + " frozen", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            ice_freeze.Play();
        }

        #endregion FrzKerbals

        #region ThwKerbals

        public void beginThawKerbal(string frozenkerbal)
        {
            //if (part.protoModuleCrew.Count >= part.CrewCapacity)
            //{
            //    ScreenMessages.PostScreenMessage("Cannot Thaw " + frozenkerbal + " Part is full", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            //}
            //else
            //{
                if (DeepFreeze.Instance.SMInstalled) // Check if Ship Manifest (SM) is installed? 
                {
                    if (IsSMXferRunning()) // SM is installed and is a Xfer running? If so we can't run a Freeze while a SMXfer is running.
                    {
                        ScreenMessages.PostScreenMessage("Cannot Thaw while Crew Xfer in progress.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                }
                if (crewXferFROMActive || crewXferTOActive)  // We can't run a thaw process if a crewXfer is active, this is catching Stock Xfers.
                {
                    ScreenMessages.PostScreenMessage("Cannot Thaw while Crew Xfer in progress.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }                
                ToThawKerbal = frozenkerbal;  // Set the Active Thaw Kerbal to null
                IsThawActive = true;  // Turn the Freezer actively thawing mode on
                hatch_lock.Play();  // Play the sound effects.
                machine_hum.Play();
                machine_hum.loop = true;
            //}
        }

        public void ThawKerbalAbort(String ThawKerbal)
        {
            ScreenMessages.PostScreenMessage("Thawing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            IsThawActive = false; // Turn the Freezer actively thawing mode off            
            StoredCharge = 0; // Discharge all EC stored 
            machine_hum.Stop(); //stop the sound effects
        }

        public void ThawKerbalConfirm(String frozenkerbal)
        {
            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned) //There's probably a more efficient way to find Protocrewmember from the CrewRoster
            {
                if (kerbal.name == frozenkerbal)  // found them
                {
                    machine_hum.Stop(); //stop sound effects
                    StoredCharge = 0;   // Discharge all EC stored                  
                    // Set our newly thawed Popsicle, er Kerbal, to Crew type again (from Unowned) and Assigned status (from Missing status).
                    kerbal.type = ProtoCrewMember.KerbalType.Crew;
                    kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;                    
                    FrznCrewMbr tmpcrew = StoredCrewList.Find(a => a.CrewName == frozenkerbal);  // Find the thawed kerbal in the frozen kerbal list.
                    if (tmpcrew != null) 
                    {
                        part.internalModel.seats[tmpcrew.Seat].taken = false; // Set their seat to NotTaken before we assign them back to their seat, not sure we really need this.
                        part.AddCrewmemberAt(kerbal, tmpcrew.Seat);           // Add the CrewMember back into the part in their assigned seat.
                        StoredCrewList.Remove(tmpcrew);                       // Remove them from the frozen kerbal list.
                    }
                    else // This should NEVER occur.
                    {
                        Debug.Log("Could not find frozen kerbal to Thaw, Very Very Bad. Report this to Mod thread"); 
                        part.AddCrewmember(kerbal);  // Add them to the part anyway.
                    } 
                    ScreenMessages.PostScreenMessage(kerbal.name + " thawed out.", 5.0f, ScreenMessageStyle.UPPER_CENTER);                    
                    Debug.Log("Thawed out: " + kerbal.name);
                    ToThawKerbal = null; // Set the Active Thaw Kerbal to null
                    IsThawActive = false; // Turn the Freezer actively thawing mode off
                    UpdateCounts(); // Update the Crew counts
                    removeThawEvent(frozenkerbal); // Remove the Thaw Event for this kerbal.
                    ding_ding.Play();
                }
            }
        }

        #endregion ThwKerbals
    
        #region CrewXfers

        public bool IsSMXferRunning()  // Checks if Ship Manifest is running a CrewXfer or Not.
        {
            ShipManifest.ICrewTransfer SMObject = null;                       
            SMObject = ShipManifest.SMInterface.GetCrewTransfer();            
            if (SMObject.CrewXferActive == true && (SMObject.SourcePart == this.part || SMObject.TargetPart == this.part))
            {
                Debug.Log("SMXfer running and it is from or to this part");
                return true;
            }
            else
            {
                if (SMObject.CrewXferActive == true)
                {
                    Debug.Log("SMXfer running but is it not from or to this part");
                }
                return false; 
            }                             
        }

        // this is called when a crew transfer is STARTED. For catching stock Xfers. Because Ship Manifest Xfers will avoid these scenarios.
        private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> fromToAction)  
        {
            Utilities.Log_Debug("Freezer","OnCrewTransferred Fired From: " + fromToAction.from.name + " To: " + fromToAction.to.name + " Host: " + fromToAction.host.name);
            if (fromToAction.from == this.part)  // if the Xfer is FROM this part
            {
                Utilities.Log_Debug("Freezer", "crewXferFROMActive");
                removeFreezeEvent(fromToAction.host);  // Remove the Freeze Event for the crewMember leaving the part
                crewXferFROMActive = true;  // Set a flag to know a Xfer has started and we check when it is finished in 
                xferfromPart = fromToAction.from;
                xfertoPart = fromToAction.to;
                xfercrew = fromToAction.host;                
            }
            if (fromToAction.to == this.part)  // if the Xfer is TO this part
            {
                Utilities.Log_Debug("Freezer", "crewXferTOActive");
                crewXferTOActive = true; // Set a flag to know a Xfer has started and we check when it is finished in 
                xferfromPart = fromToAction.from;
                xfertoPart = fromToAction.to;
                xfercrew = fromToAction.host;                
            }
        }

        // this is called when a crew enters the Part from EVA.  It checks the freezer part isn't full, if it is it kicks them out. But this should NEVER happen.
        // Better to be safe than sorry.
        private void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> action)
        {
            Utilities.Log_Debug("Freezer", " onCrewBoardVessel " + action.to.vessel.name + " (" + action.to.vessel.id + ") Old " + action.from.vessel.name + " (" + action.from.vessel.id + ")");
            Utilities.Log_Debug("Freezer", "AYController active vessel " + FlightGlobals.ActiveVessel.id);
            Utilities.Log_Debug("Freezer", "ToPart = " + action.to.partName);
            Utilities.Log_Debug("Freezer", "FromPart = " + action.from.partName);
            if (action.to == this.part)
            {
                if (this.part.internalModel.GetAvailableSeatCount() == 0)  // If there is no free seats for this Kerbal we kick them back out. this Should NEVER HAPPEN.
                {
                    Utilities.Log_Debug("Freezer", "NoAvailableSeat Must Revert");
                    FlightEVA.fetch.spawnEVA(action.from.vessel.GetVesselCrew()[0], action.to, action.to.airlock);
                }
            }            
        }

        #endregion CrewXfers

        private bool timewarpIsValid
        {
            get
            {
                return TimeWarp.CurrentRateIndex < 5;
            }
        }

        public void UpdateCounts()
        {
            // Update the part counts.            
            FreezerSpace = (FreezerSize - StoredCrewList.Count);
            TotalFrozen = StoredCrewList.Count;

            // Reset the seat status for frozen crew to taken - true, because it seems to reset by something?? So better safe than sorry.
            foreach (FrznCrewMbr lst in StoredCrewList)
            {
                this.part.internalModel.seats[lst.Seat].taken = true;
            }

            // Set the Part CrewCapacity to Zero if the freezer is full.
            if (FreezerSpace == 0 && !IsCrewableWhenFull)
            {
                part.CrewCapacity = 0;
            }
            else  // Else, set the Part CrewCapacity to the amount of Freezer space.
            {
                part.CrewCapacity = FreezerSpace;
                if (IsCrewableWhenFull)  // I really don't think we what this IsCrewableWhenFull field any more, as I have changed how the mod used.
                {
                    part.CrewCapacity += 1;
                }
            }
        }
        
        // Simple bool for resource checking and usage.  Returns true and optionally uses resource if resAmount of res is available. - Credit TMarkos https://github.com/TMarkos/ as this is lifted verbatim from his Beacon's pack. Mad modify as needed.
        public bool requireResource(Vessel craft, string res, double resAmount, bool consumeResource)
        {
            if (!craft.loaded) return false; // Unloaded resource checking is unreliable.
            Dictionary<PartResource, double> toDraw = new Dictionary<PartResource, double>();
            double resRemaining = resAmount;
            foreach (Part cPart in craft.Parts)
            {
                foreach (PartResource cRes in cPart.Resources)
                {
                    if (cRes.resourceName != res) continue;
                    if (cRes.amount == 0) continue;
                    if (cRes.amount >= resRemaining)
                    {
                        toDraw.Add(cRes, resRemaining);
                        resRemaining = 0;
                    }
                    else
                    {
                        toDraw.Add(cRes, cRes.amount);
                        resRemaining -= cRes.amount;
                    }
                }
                if (resRemaining <= 0) break;
            }
            if (resRemaining > 0) return false;
            if (consumeResource)
            {
                foreach (KeyValuePair<PartResource, double> drawSource in toDraw)
                {
                    drawSource.Key.amount -= drawSource.Value;
                }
            }
            return true;
        }
          
    }
}