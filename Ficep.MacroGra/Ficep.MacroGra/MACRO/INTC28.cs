using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTC28 : EyeMacro
    {
        public INTC28(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA;

            double radAlfa = ParALFA.ToRad();

            double tanAngle = ParB / width;
            //
            // Estrusione anima
            //

            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC + ParD - TB * tanAngle, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC + ParD - TB * tanAngle, TB + 2 * ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC - (2 * ParR + TB) * tanAngle, TB + 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC - (width - TB - 2 * ParR) * tanAngle, width - TB - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC - (width - TB - 2 * ParR) * tanAngle + ParD, width - TB - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC - (width - TB - 2 * ParR) * tanAngle + ParD, width - TB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - (ParB + ParC - (width - TB - 2 * ParR) * tanAngle + ParD), width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (ParB + ParC - (width - TB - 2 * ParR) * tanAngle + ParD), width - TB - 2 * ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - (ParB + ParC - (width - TB - 2 * ParR) * tanAngle), width - TB - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (ParB + ParC - (2 * ParR + TB) * tanAngle), TB + 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (ParB + ParC + ParD - TB * tanAngle), TB + 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (ParB + ParC + ParD - TB * tanAngle), TB, 0, 0, ParR));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);


            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}