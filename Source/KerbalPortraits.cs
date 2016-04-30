using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using KSP.UI.Screens.Flight;
using UnityEngine;


namespace DeepFreeze
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class DFPortraits : MonoBehaviour
    {

        internal static BindingFlags eFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        //reflecting protected methods inside a public class. Until KSP 1.1.x can rectify the situation.
        internal static void UIControlsUpdate()
        {
            MethodInfo UIControlsUpdateMethod = typeof(KerbalPortraitGallery).GetMethod("UIControlsUpdate", eFlags);
            UIControlsUpdateMethod.Invoke(KerbalPortraitGallery.Instance, null);
        }

        //reflecting protected methods inside a public class. Until KSP 1.1.x can rectify the situation.
        internal static void DespawnInactivePortraits()
        {
            MethodInfo DespawnInactPortMethod = typeof(KerbalPortraitGallery).GetMethod("DespawnInactivePortraits", eFlags);
            DespawnInactPortMethod.Invoke(KerbalPortraitGallery.Instance, null);
        }

        //reflecting protected methods inside a public class. Until KSP 1.1.x can rectify the situation.
        internal static void DespawnPortrait(Kerbal kerbal)
        {
            MethodInfo DespawnPortraitMethod = typeof(KerbalPortraitGallery).GetMethod("DespawnPortrait", eFlags, Type.DefaultBinder, new Type[] { typeof(Kerbal) }, null);
            DespawnPortraitMethod.Invoke(KerbalPortraitGallery.Instance, new object[] { kerbal });
        }

        internal static bool HasPortrait(Kerbal crew, bool checkName = false)
        {
            if (!checkName)
            {
                return KerbalPortraitGallery.Instance.Portraits.Any(p => p.crewMember == crew);
            }
            else
            {
                return KerbalPortraitGallery.Instance.Portraits.Any(p => p.crewMemberName == crew.crewMemberName);
            }
        }

        internal static bool InActiveCrew(Kerbal crew, bool checkName = false)
        {
            if (!checkName)
            {
                return KerbalPortraitGallery.Instance.ActiveCrew.Any(p => p == crew);
            }
            else
            {
                return KerbalPortraitGallery.Instance.ActiveCrew.Any(p => p.crewMemberName == crew.crewMemberName);
            }
        }

        /// <summary>
        /// Destroy Portraits for a kerbal and Unregisters them from the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the Kerbal we want to delete portraits for</param>
        internal static void DestroyPortrait(Kerbal kerbal)
        {
            // set the kerbal InPart to null - this should stop their portrait from re-spawning.
            kerbal.InPart = null;
            //Set them visible in portrait to false
            kerbal.SetVisibleInPortrait(false);
            kerbal.state = Kerbal.States.NO_SIGNAL;
            //Loop through the ActiveCrew portrait List
            for (int i = KerbalPortraitGallery.Instance.ActiveCrew.Count - 1; i >= 0; i--)
            {
                //If we find an ActiveCrew entry where the crewMemberName is equal to our kerbal's
                if (KerbalPortraitGallery.Instance.ActiveCrew[i].crewMemberName == kerbal.crewMemberName)
                {
                    //we Remove them from the list.
                    KerbalPortraitGallery.Instance.ActiveCrew.RemoveAt(i);
                }
            }
            //Portraits List clean-up.
            DespawnInactivePortraits(); //Despawn any portraits where CrewMember == null
            DespawnPortrait(kerbal); //Despawn our Kerbal's portrait
            UIControlsUpdate(); //Update UI controls
        }

        /// <summary>
        /// Restore the Portrait for a kerbal and register them to the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the kerbal we want restored</param>
        /// <param name="part">the part the kerbal is in</param>
        internal static void RestorePortrait(Part part, Kerbal kerbal)
        {
            //We don't process DEAD, Unowned kerbals - Compatibility with DeepFreeze Mod.
            if (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead &&
                kerbal.protoCrewMember.type != ProtoCrewMember.KerbalType.Unowned)
            {
                //Set the Kerbals InPart back to their part.
                kerbal.InPart = part;
                //Set their portrait state to ALIVE and set their portrait back to visible.
                kerbal.state = Kerbal.States.ALIVE;
                kerbal.SetVisibleInPortrait(true);
                //Find an ActiveCrew entry and Portraits entry for our kerbal?
                //If they aren't in ActiveCrew and don't have a Portrait them via the kerbal.Start method.
                if (!InActiveCrew(kerbal) && !HasPortrait(kerbal))
                {
                    kerbal.staticOverlayDuration = 1f;
                    kerbal.randomizeOnStartup = false;
                    kerbal.Start();
                }
                kerbal.state = Kerbal.States.ALIVE;
            }
        }
    }
}
