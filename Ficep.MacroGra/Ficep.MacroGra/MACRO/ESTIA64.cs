using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA64 : EyeMacro
    {
        public ESTIA64(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = SB, offsetY = SB / 2;

            double radAlfa = VX == "I" ? ParALFA.ToRad() : - ParALFA.ToRad();
            radAlfa = VY == "A" ? radAlfa : -radAlfa;
            double tanAlfa = Math.Tan(radAlfa);
            double absTanAlfa = Math.Abs(tanAlfa);

            //
            // Estrusione ALA OPPOSITE SIDE
            //
            double extrusionDepth = TB;
            string extrusionPlane = VY == "A" ? "B" : "A";

            macroPoint.Add(new ProgramPoint(0, extrusionPlane == "B" ? 0 : topY, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetY * absTanAlfa - TA / 2 * tanAlfa + ParC + ParK, extrusionPlane == "B" ? 0 : topY, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetY * absTanAlfa - TA / 2 * tanAlfa + ParC, extrusionPlane == "B" ? offsetY - ParL : offsetY + ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetY * absTanAlfa - TA / 2 * tanAlfa + ParC, extrusionPlane == "B" ? offsetY + ParJ : offsetY - ParL, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetY * absTanAlfa - TA / 2 * tanAlfa + ParC + ParI, extrusionPlane == "B" ? topY : 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, extrusionPlane == "B" ? topY : 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione anima
            //
            extrusionDepth = SB;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetY * absTanAlfa - TA / 2 * tanAlfa + ParD + ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetY * absTanAlfa - TA / 2 * tanAlfa + ParD + ParR, ParA, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParA, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(0, SA, 0, 0));
            macroPoint.Add(new ProgramPoint((offsetY - TA / 2) * absTanAlfa + ParC, SA, 0, 0));
            macroPoint.Add(new ProgramPoint((offsetY - TA / 2) * absTanAlfa + ParC, SA - ParB, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, SA - ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ALA SIDE
            //
            extrusionDepth = SA - ParB;
            extrusionPlane = VY;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(topY * absTanAlfa, radAlfa >= 0 ? 0 : topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala Side
            //
            double chamferDepth = TB - ParG - ParH;
            
            ProgramPoint startChamfer = new ProgramPoint(radAlfa >= 0 ? topY * absTanAlfa : 0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(radAlfa >= 0 ? 0 : topY * absTanAlfa, topY, 0, 0);
            
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala Side
            //
            chamferDepth = ParG;

            startChamfer = new ProgramPoint(radAlfa >= 0 ? topY * absTanAlfa : 0, 0, 0, 0);
            endChamfer = new ProgramPoint(radAlfa >= 0 ? 0 : topY * absTanAlfa, topY, 0, 0); 

            if (!ParE.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParE.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}