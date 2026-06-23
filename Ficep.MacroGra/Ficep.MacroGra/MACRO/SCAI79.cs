using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI79 : EyeMacro
    {

        public SCAI79(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            List<Brep> breps = new List<Brep>();

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            //
            // Estrusione anima
            //

            //	REG_01 is the distance from the inner wing of the circle arc
            //	saturated at its minimum value ParB - ParR - TB
            //
            double REG_01, REG_02;
            if (ParC <= ParB - ParR - TB)
            {
                REG_01 = ParB - ParR - TB;
                REG_02 = 0;
            }
            else
            {
                REG_01 = ParC;
                REG_02 = Math.Sqrt(ParR * ParR - (ParB - ParC - TB) * (ParB - ParC - TB));
            }

            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + REG_02, TB + REG_01, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, ParB + ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParB + Math.Sqrt(ParR * ParR - ParD * ParD), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}