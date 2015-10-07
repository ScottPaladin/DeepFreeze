using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace DF
{    
    public class DFEngReport : PreFlightTests.DesignConcernBase
    {
        private ShipConstruct ship;        
        private List<Part> cryofreezers = new List<Part>();
        private string Glykerol = "Glykerol";

        // Is the Test OK ?
        public override bool TestCondition()
        {
            this.Log_Debug("DFEngReport Test condition");            
            this.ship = EditorLogic.fetch.ship;            
            cryofreezers.Clear();                                  
            cryofreezers = (from p in ship.parts where p.Modules.Contains("DeepFreezer") select p).ToList();
            if (cryofreezers.Count() <=0)
            {
                //No freezer parts
                this.Log_Debug("DFEngReport No Freezer Parts");
                return true;
            }

            //Search through the ship resources for Glykerol.
            PartResourceDefinition glykerol = PartResourceLibrary.Instance.GetDefinition(Glykerol);
            double glykerolOnBoard = 0;
            foreach ( Part part in ship.parts)
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
                this.Log_Debug("DFEngReport 5 units found");
                return true;
            }
            this.Log_Debug("DFEngReport No Glykerol Found");
            return false;
        }

        // List of affected parts
        public override List<Part> GetAffectedParts()
        {            
            return this.cryofreezers;
        }

        // Title of the problem description
        public override string GetConcernTitle()
        {
            return "DeepFreeze";
        }

        // problem description
        public override string GetConcernDescription()
        {
            return "There is less than 5 units of Glykerol on-board for your DeepFreeze Freezers";
        }

        // how bad is the problem
        public override PreFlightTests.DesignConcernSeverity GetSeverity()
        {
            return PreFlightTests.DesignConcernSeverity.NOTICE;
        }

        // does it applies to Rocket, plane or both
        public override EditorFacilities GetEditorFacilities()
        {
            return EditorFacilities.ALL;
        }
    }
}
