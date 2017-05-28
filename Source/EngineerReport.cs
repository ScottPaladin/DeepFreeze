using System.Collections.Generic;
using System.Linq;
using PreFlightTests;
using RSTUtils;
using KSP.Localization;

namespace DF
{
    public class DFEngReport : DesignConcernBase
    {
        private ShipConstruct ship;
        private List<Part> cryofreezers = new List<Part>();
        private string Glykerol = "Glykerol";

        // Is the Test OK ?
        public override bool TestCondition()
        {
             Utilities.Log_Debug("DFEngReport Test condition");
            ship = EditorLogic.fetch.ship;
            cryofreezers.Clear();
            cryofreezers = (from p in ship.parts where p.Modules.Contains("DeepFreezer") select p).ToList();
            if (!cryofreezers.Any())
            {
                //No freezer parts
                 Utilities.Log_Debug("DFEngReport No Freezer Parts");
                return true;
            }

            //Search through the ship resources for Glykerol.
            PartResourceDefinition glykerol = PartResourceLibrary.Instance.GetDefinition(Glykerol);
            double glykerolOnBoard = 0;
            foreach (Part part in ship.parts)
            {
                if (part.Resources.Contains(glykerol.id))
                {
                    PartResource glyk = part.Resources.Get(glykerol.id);
                    glykerolOnBoard += glyk.amount;
                }
            }

            //If we found 5 or more units of glykerol all ok.
            if (glykerolOnBoard >= 5)
            {
                 Utilities.Log_Debug("DFEngReport 5 units found");
                return true;
            }
             Utilities.Log_Debug("DFEngReport No Glykerol Found");
            return false;
        }

        // List of affected parts
        public override List<Part> GetAffectedParts()
        {
            return cryofreezers;
        }

        // Title of the problem description
        public override string GetConcernTitle()
        {
            return Localizer.Format("#autoLOC_DF_00003"); // "DeepFreeze";
        }

        // problem description
        public override string GetConcernDescription()
        {
            return Localizer.Format("#autoLOC_DF_00108"); // "There is less than 5 units of Glykerol on-board for your DeepFreeze Freezers";
        }

        // how bad is the problem
        public override DesignConcernSeverity GetSeverity()
        {
            return DesignConcernSeverity.NOTICE;
        }

        // does it applies to Rocket, plane or both
        public override EditorFacilities GetEditorFacilities()
        {
            return EditorFacilities.ALL;
        }
    }
}