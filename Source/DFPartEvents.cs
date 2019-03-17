using System.Collections.Generic;
using KSP.Localization;
using RSTUtils;


namespace DF
{    
    public class CryopodEvents
    {
        public class EventData
        {
            public string kerbalName;
            public BaseEvent eventItem;

            public EventData(string kerbalName, BaseEvent eventItem)
            {
                this.kerbalName = kerbalName;
                this.eventItem = eventItem;
            }
        }

        public List<EventData> FreezeEvents;
        public List<EventData> ThawEvents;
        public Part partRef;
        public bool IsRTModInstalled;
        public DeepFreezer freezerModuleRef;

        public CryopodEvents(Part partRef, DeepFreezer freezerRef)
        {
            FreezeEvents = new List<EventData>();
            ThawEvents = new List<EventData>();
            this.partRef = partRef;
            IsRTModInstalled = DFInstalledMods.IsRTInstalled;
            freezerModuleRef = freezerRef;
        }

        public bool ContainsFreezeEvent(BaseEvent freezeEvent)
        {
            for (int i = 0; i < FreezeEvents.Count; i++)
            {
                if (FreezeEvents[i].eventItem.name == freezeEvent.name)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsFreezeEvent(string kerbalName, out BaseEvent returnEvent)
        {
            returnEvent = null;
            for (int i = 0; i < FreezeEvents.Count; i++)
            {
                if (FreezeEvents[i].kerbalName == kerbalName)
                {
                    returnEvent = FreezeEvents[i].eventItem;
                    return true;
                }
            }
            return false;
        }

        public bool ContainsThawEvent(BaseEvent thawEvent)
        {
            for (int i = 0; i < ThawEvents.Count; i++)
            {
                if (ThawEvents[i].eventItem.name == thawEvent.name)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsThawEvent(string kerbalName, out BaseEvent returnEvent)
        {
            returnEvent = null;
            for (int i = 0; i < ThawEvents.Count; i++)
            {
                if (ThawEvents[i].kerbalName == kerbalName)
                {
                    returnEvent = ThawEvents[i].eventItem;
                    return true;
                }
            }
            return false;
        }

        public void RemoveFreezeEvent(BaseEvent freezeEvent)
        {
            if (partRef != null)
            {
                partRef.Events.Remove(freezeEvent);
            }
            for (int i = 0; i < FreezeEvents.Count; i++)
            {
                if (FreezeEvents[i].eventItem == freezeEvent)
                {
                    FreezeEvents.RemoveAt(i);
                    break;
                }
            }
        }

        public bool RemoveFreezeEvent(string kerbalName)
        {
            for (int i = 0; i < FreezeEvents.Count; i++)
            {
                if (FreezeEvents[i].kerbalName == kerbalName)
                {
                    if (partRef != null)
                    {
                        partRef.Events.Remove(FreezeEvents[i].eventItem);
                    }
                    FreezeEvents.RemoveAt(i);
                    return true;                    
                }
            }
            return false;
        }

        public void RemoveThawEvent(BaseEvent thawEvent)
        {
            if (partRef != null)
            {
                partRef.Events.Remove(thawEvent);
            }
            for (int i = 0; i < ThawEvents.Count; i++)
            {
                if (ThawEvents[i].eventItem == thawEvent)
                {
                    ThawEvents.RemoveAt(i);
                    break;
                }
            }
        }

        public bool RemoveThawEvent(string kerbalName)
        {
            for (int i = 0; i < ThawEvents.Count; i++)
            {
                if (ThawEvents[i].kerbalName == kerbalName)
                {
                    if (partRef != null)
                    {
                        partRef.Events.Remove(ThawEvents[i].eventItem);
                    }
                    ThawEvents.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void AddFreezeEvent(string kerbalName, BaseEvent eventItem, bool addToPart)
        {
            BaseEvent eventData = null;
            if (!ContainsFreezeEvent(kerbalName, out eventData))
            {
                EventData newItem = new EventData(kerbalName, eventItem);
                FreezeEvents.Add(newItem);
                if (addToPart && partRef != null)
                {
                    partRef.Events.AddUnique(eventItem);
                }
            }
        }

        private void AddThawEvent(string kerbalName, BaseEvent eventItem, bool addToPart)
        {
            BaseEvent eventData = null;
            if (!ContainsThawEvent(kerbalName, out eventData))
            {
                EventData newItem = new EventData(kerbalName, eventItem);
                ThawEvents.Add(newItem);
                if (addToPart && partRef != null)
                {
                    partRef.Events.AddUnique(eventItem);
                }
            }
        }

        public void AddFreezeEvent(string kerbalName, bool addToPart)
        {
            if (partRef == null)
            {
                return;
            }
            BaseEvent eventData = null;
            if (!ContainsFreezeEvent(kerbalName, out eventData))
            {
                ProtoCrewMember kerbal = null;
                for (int j = 0; j < partRef.protoModuleCrew.Count; j++)
                {
                    if (partRef.protoModuleCrew[j].name == kerbalName)
                    {
                        kerbal = partRef.protoModuleCrew[j];
                        break;
                    }
                }
                if (kerbal != null)
                {
                    BaseEvent newEvent = createFreezeEvent(kerbal);
                    if (newEvent != null)
                    {
                        EventData newItem = new EventData(kerbalName, newEvent);
                        FreezeEvents.Add(newItem);
                        if (addToPart && partRef != null)
                        {
                            partRef.Events.AddUnique(newEvent);
                        }
                    }
                }
            }
        }

        public void AddThawEvent(string kerbalName, bool addToPart)
        {
            BaseEvent eventData = null;
            if (!ContainsThawEvent(kerbalName, out eventData))
            {
                BaseEvent newEvent = createThawEvent(kerbalName);
                if (newEvent != null)
                {
                    EventData newItem = new EventData(kerbalName, newEvent);
                    ThawEvents.Add(newItem);
                    if (addToPart && partRef != null)
                    {
                        partRef.Events.AddUnique(newEvent);
                    }
                }
            }
        }

        public void RemoveAllEventsFromPart()
        {
            if (partRef == null)
            {
                return;
            }
            for (int i = 0; i < ThawEvents.Count; i++)
            {
                partRef.Events.Remove(ThawEvents[i].eventItem);
            }
            for (int i = 0; i < FreezeEvents.Count; i++)
            {
                partRef.Events.Remove(FreezeEvents[i].eventItem);
            }
        }

        public void AddAllEventsToPart()
        {
            if (partRef == null)
            {
                return;
            }
            RemoveAllEventsFromPart();
            for (int i = 0; i < ThawEvents.Count; i++)
            {
                partRef.Events.Add(ThawEvents[i].eventItem);
            }
            for (int i = 0; i < FreezeEvents.Count; i++)
            {
                partRef.Events.Add(FreezeEvents[i].eventItem);
            }
        }

        private BaseEvent createFreezeEvent(ProtoCrewMember kerbal)
        {
            if (partRef == null || freezerModuleRef == null)
            {
                return null;
            }
            return new BaseEvent(partRef.Events, "Freeze " + kerbal.name, () =>
            {
               freezerModuleRef.beginFreezeKerbal(kerbal);
            }, new KSPEvent { guiName = Localizer.Format("#autoLOC_DF_00199", kerbal.name), guiActive = true });            
        }

        private BaseEvent createThawEvent(string kerbalName)
        {
            if (partRef == null || freezerModuleRef == null)
            {
                return null;
            }
            return new BaseEvent(partRef.Events, "Thaw " + kerbalName, () =>
            {
                freezerModuleRef.beginThawKerbal(kerbalName);
            }, new KSPEvent { guiName = Localizer.Format("#autoLOC_DF_00200", kerbalName), guiActive = true });
        }


        public void SyncEvents()
        {
            //Check Freeze and Thaw Events are for Kerbals still in the Vessel.
            if (partRef == null || freezerModuleRef == null)
            {
                return;
            }
            //If RemoteTech is installed show or don't show all the events, depending on connection state.
            if (IsRTModInstalled)
            {
                if (!freezerModuleRef.isRTConnected)
                {
                    RemoveAllEventsFromPart();
                    return;
                }
                else
                {
                    AddAllEventsToPart();
                }
            }

            FrznCrewList frozenCrew = freezerModuleRef.DFIStoredCrewList;
            //Check the ThawEvents against the frozen crew list in the PartModule and Remove Thaw events for kerbals that aren't in the frozen list.
            for (int i = ThawEvents.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = 0; j < frozenCrew.Count; j++)
                {
                    if (frozenCrew[j].CrewName == ThawEvents[i].kerbalName)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) //We didn't find a match. remove the ThawEvent.
                {
                    RemoveThawEvent(ThawEvents[i].kerbalName);
                }
            }
            //Check the FreezeEvents against the  part's crew list and Remove Freeze events for kerbals that aren't in the part.
            for (int i = FreezeEvents.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = 0; j < partRef.protoModuleCrew.Count; j++)
                {
                    if (partRef.protoModuleCrew[j].name == FreezeEvents[i].kerbalName)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) //We didn't find a match. remove the ThawEvent.
                {
                    RemoveFreezeEvent(FreezeEvents[i].kerbalName);
                }
            }

            //Check the Stored Crew (frozen) against the ThawEvents list and Add missing Thaw Event items.            
            for (int i = 0; i < frozenCrew.Count; i++)
            {
                BaseEvent eventData = null;
                if (!ContainsThawEvent(frozenCrew[i].CrewName, out eventData))
                {                    
                    BaseEvent newEvent = createThawEvent(frozenCrew[i].CrewName);
                    if (newEvent != null)
                    {
                        AddThawEvent(frozenCrew[i].CrewName, newEvent, true);
                    }                    
                }
            }
            //Check the Crew in the part against the FreezeEvents list and Add missing Freeze Event items.       
            for (int i = 0; i < partRef.protoModuleCrew.Count; i++)
            {
                BaseEvent eventData = null;
                if (!ContainsFreezeEvent(partRef.protoModuleCrew[i].name, out eventData))
                {                    
                    BaseEvent newEvent = createFreezeEvent(partRef.protoModuleCrew[i]);
                    if (newEvent != null)
                    {
                        AddFreezeEvent(partRef.protoModuleCrew[i].name, newEvent, true);
                    }                    
                }
            }
        }
    }    
}
