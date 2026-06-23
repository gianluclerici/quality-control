using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA37 : EyeMacro
    {

        public ESTIA37(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fissa
            ///////////////////////////////
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            double topY = SB, offsetY = SB / 2;

            macroPoint.Add(new ProgramPoint(0, offsetY - TA / 2 - ParB - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, offsetY - TA / 2 - ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY - TA / 2 - ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            //
            //  ESTRUSIONE
            //
            double extrusionDepth = SA;
            string extrusionPlane = Side;                        
            bool mirrorAltoBasso = Side == "A" ? false : true,   //  
                 mirrorSideASideB = false;                       // GESTITO CASO SPECIALE IN CUI QUANDO C'E' SIDE = B
                                                                 // BISOGNA FARE MIRROR ALTO BASSO
                                                                 // 
            List<Brep> breps = new List<Brep>();

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, mirrorSideASideB, mirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
