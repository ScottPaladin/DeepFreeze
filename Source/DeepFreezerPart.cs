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

        public bool crewXferActive = false;
        private Part xferfromPart;
        private Part xfertoPart;
        public ProtoCrewMember xfercrew;
        private System.Random rnd = new System.Random();

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

        [KSPField()]
        public bool IsFreezeActive;

        [KSPField()]
        public bool IsThawActive;

        [KSPField()]
        public double StoredCharge;

        [KSPField(isPersistant = true)]
        public Int32 ChargeRequired; //Set by part.cfg. Total EC value required for a complete freeze or thaw.

        [KSPField(isPersistant = true)]
        public Int32 ChargeRate; //Set by part.cfg. EC draw per tick.

        public ProtoCrewMember ActiveKerbal;
        public string ToThawKerbal;

        public List<string> StoredCrew;

        protected AudioSource hatch_lock;
        protected AudioSource ice_freeze;
        protected AudioSource machine_hum;
        protected AudioSource ding_ding;

        public override void OnUpdate()
        {
            if ((Time.time - lastUpdate) > updatetnterval && (Time.time - lastRemove) > updatetnterval)
            {
                lastUpdate = Time.time;
                UpdateEvents();
                //StoredCrew = StoredCrew.Distinct().ToList();
                if (StoredCrew.Count > 0)
                {
                    FrozenCrew = String.Join(",", StoredCrew.ToArray());
                }
            }
            if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer)
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
                        int dice = rnd.Next(1, 3);
                        if (dice == 2)
                        {
                            //a kerbal dies
                            int dice2 = rnd.Next(1, StoredCrew.Count);
                            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == StoredCrew[dice2]);
                            if (kerbal == null)
                            {
                                Debug.Log("DeepFreeze Tried to Kill a frozen kerbal but couldn't find them. This should never happen. Please report this to mod owner");
                            }
                            else
                            {
                                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                                ScreenMessages.PostScreenMessage(kerbal.name + " died due to lack of Electrical Charge to run cryogenics", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                                StoredCrew.RemoveAt(dice2);
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
            Debug.Log("onLoad");
            //ChargeRequired = 3000;
            //ChargeRate = 20;
            Int32.TryParse(node.GetValue("ChargeRequired"), out ChargeRequired);
            Int32.TryParse(node.GetValue("ChargeRate"), out ChargeRate);
            IsCrewableWhenFull = Convert.ToBoolean(node.GetValue("IsCrewableWhenFull"));
            FrozenCrew = node.GetValue("FrozenCrew");
            Debug.Log("Frozen Crew =" + FrozenCrew);
            LoadFrozenCrew();
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state != StartState.None || state != StartState.Editor)
            {
                GameEvents.onCrewTransferred.Add(this.OnCrewTransferred);
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
        }

        public override void OnSave(ConfigNode node)
        {
            node.SetValue("FrozenCrew", FrozenCrew);
            Debug.Log("OnSave: " + node);
        }

        public override void OnInactive()
        {
            Debug.Log("OnInactive" + FrozenCrew);
            /*
             * if (IsCrewableWhenFull)
            {
                part.CrewCapacity = StoredCrew.Count +1;
            }
            else
            {
                part.CrewCapacity = StoredCrew.Count;
            }
             * */
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

        private void LoadFrozenCrew() //The FrozenCrew variable is a string, we need it split into a list.
        {
            if (!string.IsNullOrEmpty(FrozenCrew))
            {
                StoredCrew = FrozenCrew.Split(',').ToList();
            }
        }

        private void UpdateEvents()
        {
            //Debug.Log("UpdateEvents");
            UpdateCounts();
            if (crewXferActive)
            {
                Debug.Log("Crew Xfer Active, chcking if complete");
                ProtoCrewMember crewfered = xfertoPart.protoModuleCrew.FirstOrDefault(a => a.name == xfercrew.name);
                if (crewfered != null)
                {
                    Debug.Log("Crew Xfer completed");
                    removeFreezeEvent(xfercrew);
                    crewXferActive = false;
                }
            }
            if (!IsThawActive && !IsFreezeActive && !crewXferActive)
            {
                foreach (BaseEvent itemX in Events)
                {
                    Debug.Log("Event = " + itemX.name);
                    string[] subStrings = itemX.name.Split(' ');
                    if (subStrings[0] == "Freeze")
                    {
                        string crewname = "";
                        crewname = subStrings[1] + " " + subStrings[2];
                        Debug.Log("Looking for " + crewname);
                        ProtoCrewMember fndcrew = part.protoModuleCrew.Find(a => a.name == crewname);
                        if (fndcrew == null)
                        {
                            Debug.Log("Not found in part, remove event");
                            fndcrew = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == crewname);
                            removeFreezeEvent(fndcrew);
                        }
                        else
                            Debug.Log("Found in part,  leave event");
                    }
                }
                if (StoredCrew.Count < FreezerSize)
                {
                    //part.CrewCapacity = 1;
                    foreach (var CrewMember in part.protoModuleCrew)
                    {
                        addFreezeEvent(CrewMember);
                    }
                }
                //if ((part.protoModuleCrew.Count < part.CrewCapacity) || part.CrewCapacity <= 0)
                if (part.protoModuleCrew.Count < part.CrewCapacity)
                {
                    foreach (var frozenkerbal in StoredCrew)
                    {
                        addThawEvent(frozenkerbal);
                    }
                }
            }
            //if (StoredCrew.Count >= FreezerSize && IsCrewableWhenFull == true)
            //{
            //    part.CrewCapacity = 0;
            //}
        }

        private void addFreezeEvent(ProtoCrewMember CrewMember)
        {
            Debug.Log("Try Add freeze event " + CrewMember.name);
            BaseEvent item = Events.Find(v => v.name == "Freeze " + CrewMember.name);
            Debug.Log("Current Events List");
            foreach (BaseEvent itemX in Events)
            {
                Debug.Log("Event = " + itemX.name);
            }
            if (item != null)
                Debug.Log("Item Name = " + item.name);
            if (item == null && CrewMember.type == ProtoCrewMember.KerbalType.Crew)
            {
                Debug.Log("Adding freeze event " + CrewMember.name);
                Events.Add(new BaseEvent(Events, "Freeze " + CrewMember.name, () =>
                {
                    beginFreezeKerbal(CrewMember);
                }, new KSPEvent { guiName = "Freeze " + CrewMember.name, guiActive = true }));
            }
        }

        public void beginFreezeKerbal(ProtoCrewMember CrewMember)
        {
            if (FreezerSpace > 0 && part.protoModuleCrew.Contains(CrewMember))
            {
                if (!requireResource(vessel, "Glykerol", 5, false))
                {
                    ScreenMessages.PostScreenMessage("Insufficient Glykerol to freeze kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                else
                {
                    FreezeKerbal(CrewMember);
                    //part.Events["Freeze " + CrewMember.name].guiActive = false;
                }
            }
            else
            {
                if (FreezerSpace == 0)
                    ScreenMessages.PostScreenMessage("Cannot freeze kerbal. Freezer is full.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        private void removeFreezeEvent(ProtoCrewMember CrewMember)
        {
            try
            {
                Debug.Log("Removing Freeze Event for " + CrewMember.name);
                BaseEvent item = Events.Find(v => v.name == "Freeze " + CrewMember.name);
                if (item == null)
                    Debug.Log("Freeze Event Item not found to delete " + CrewMember.name);
                else
                {
                    Events.Remove(item);
                    lastRemove = Time.time;
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part)
                        {
                            window.displayDirty = true;
                        }
                    }
                    Debug.Log("Item deleted");
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
            Debug.Log("Try Add thaw event " + frozenkerbal);
            BaseEvent item = Events.Find(v => v.name == "Thaw " + frozenkerbal);
            Debug.Log("Current Events List");
            foreach (BaseEvent itemX in Events)
            {
                Debug.Log("Event = " + itemX.name);
            }
            if (item != null)
                Debug.Log("Item Name = " + item.name);
            if (item == null)
            {
                Debug.Log("Adding thaw event " + frozenkerbal);
                Events.Add(new BaseEvent(Events, "Thaw " + frozenkerbal, () =>
                {
                    if (StoredCrew.Contains(frozenkerbal))
                    {
                        beginThawKerbal(frozenkerbal);
                    }
                }, new KSPEvent { guiName = "Thaw " + frozenkerbal, guiActive = true }));
            }
        }

        public void beginThawKerbal(string frozenkerbal)
        {
            if (part.protoModuleCrew.Count >= part.CrewCapacity)
            {
                ScreenMessages.PostScreenMessage("Cannot Thaw " + frozenkerbal + " Part is full", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                StoredCrew.Remove(frozenkerbal);
                ToThawKerbal = frozenkerbal;
                IsThawActive = true;
                hatch_lock.Play();
                machine_hum.Play();
                machine_hum.loop = true;
            }
        }

        private void removeThawEvent(string frozenkerbal)
        {
            try
            {
                Debug.Log("Removing Thaw Event for " + frozenkerbal);
                BaseEvent item = Events.Find(v => v.name == "Thaw " + frozenkerbal);
                if (item == null)
                    Debug.Log("Thaw Event Item not found to delete " + frozenkerbal);
                else
                {
                    Events.Remove(item);
                    lastRemove = Time.time;
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part)
                        {
                            window.displayDirty = true;
                        }
                    }
                    Debug.Log("Item deleted");
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception removing Thaw Event for " + frozenkerbal);
                Debug.Log(ex.Message);
            }
        }

        private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> fromToAction)
        {
            Debug.Log("OnCrewTransferred Fired From: " + fromToAction.from.name + " To: " + fromToAction.to.name + " Host: " + fromToAction.host.name);
            if (fromToAction.from == this.part)
            {
                removeFreezeEvent(fromToAction.host);
                crewXferActive = true;
                xferfromPart = fromToAction.from;
                xfertoPart = fromToAction.to;
                xfercrew = fromToAction.host;
            }
        }

        public void FreezeKerbal(ProtoCrewMember CrewMember)
        {
            Debug.Log("Freeze kerbal called");
            //part.CrewCapacity = 0;

            part.RemoveCrewmember(CrewMember);
            ActiveKerbal = CrewMember;
            IsFreezeActive = true;
            ScreenMessages.PostScreenMessage("Starting Freeze", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            Debug.Log("FrozenCrew =" + FrozenCrew);
            UpdateCounts();
            hatch_lock.Play();
            machine_hum.Play();
            machine_hum.loop = true;
        }

        public void FreezeKerbalAbort(ProtoCrewMember kerbal)
        {
            ScreenMessages.PostScreenMessage("Freezing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            //part.CrewCapacity = 1;
            part.AddCrewmember(kerbal);
            IsFreezeActive = false;
            ActiveKerbal = null;
            machine_hum.Stop();
        }

        public void ThawKerbalAbort(String ThawKerbal)
        {
            ScreenMessages.PostScreenMessage("Thawing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            IsThawActive = false;
            StoredCrew.Add(ThawKerbal);
            StoredCharge = 0;
            machine_hum.Stop();
        }

        public void FreezeKerbalConfirm(ProtoCrewMember kerbal)
        {
            machine_hum.Stop();
            StoredCharge = 0;
            StoredCrew.Add(kerbal.name);
            StoredCrew = StoredCrew.Distinct().ToList();
            //kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
            kerbal.type = ProtoCrewMember.KerbalType.Unowned;
            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Missing;
            UpdateCounts();
            IsFreezeActive = false;
            ActiveKerbal = null;
            removeFreezeEvent(kerbal);
            ScreenMessages.PostScreenMessage(kerbal.name + " frozen", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            ice_freeze.Play();
        }

        public void ThawKerbalConfirm(String frozenkerbal)
        {
            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned) //There's probably a more efficient way to find Protocrewmember from the CrewRoster
            {
                if (kerbal.name == frozenkerbal)
                {
                    machine_hum.Stop();
                    StoredCharge = 0;
                    //part.CrewCapacity = 1;
                    kerbal.type = ProtoCrewMember.KerbalType.Crew;
                    kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                    part.AddCrewmember(kerbal);
                    ScreenMessages.PostScreenMessage(kerbal.name + " thawed out.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    StoredCrew.Remove(kerbal.name);
                    Debug.Log("Thawed out: " + kerbal.name);
                    ToThawKerbal = null;
                    IsThawActive = false;

                    FrozenCrew = String.Join(",", StoredCrew.ToArray());
                    Debug.Log("FrozenCrew =" + FrozenCrew);
                    UpdateCounts();
                    removeThawEvent(frozenkerbal);
                    ding_ding.Play();
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

        public void UpdateCounts()
        {
            FreezerSpace = (FreezerSize - StoredCrew.Count);
            TotalFrozen = StoredCrew.Count;
            /*
            if (FreezerSpace == 0 && !IsCrewableWhenFull)
            {
                part.CrewCapacity = 0;
            }
            else
            {
                part.CrewCapacity = FreezerSpace;
                if (IsCrewableWhenFull)
                {
                    part.CrewCapacity += 1;
                }
            }*/
        }
    }
}