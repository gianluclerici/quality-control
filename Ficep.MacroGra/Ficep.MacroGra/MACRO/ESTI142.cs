using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI142 : EyeMacro
    {
        public ESTI142(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, topY = SB, offsetY = SB / 2;
            double chamWebDist = ParM > (InnerChamferDisFromWeb + TA / 2) ? ParM : (InnerChamferDisFromWeb + TA / 2);

            double tanA = Math.Tan(ParA.ToRad()), tanE = Math.Tan(ParE.ToRad());
            //
            double radAlfa = Math.Atan((ParN - ParO) / (width - 2 * TB - ParJ - ParK));
            
            //Variabili che lascio commentate se può interessare la soluzione proveniente dal file .MAC
            //double REG_02 = ParR * Math.Tan((90 - radAlfa) / 2);
            //double REG_04 = REG_02 * Math.Cos(90 - radAlfa);
            //double REG_05 = REG_02 * Math.Sin(90 - radAlfa);
            //double REG_06 = ParR / (Math.Tan((90 - radAlfa) / 2));
            //double REG_08 = REG_06 * Math.Sin(radAlfa);
            //double REG_09 = REG_06 * Math.Cos(radAlfa);
            //
            // Estrusione anima
            //

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + (TB - ParC - ParD) * tanA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, TB - ParC - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParN + ParP + ParJ * Math.Tan(radAlfa), TB, 0, 0));
            //Uso il punto successivo per ottenere la curvatura, il file .Mac utilizza i due punti commentati di seguito
            macroPoint.Add(new ProgramPoint(ParN + ParP, TB + ParJ, 0, ParR));
            //macroPoint.Add(new ProgramPoint(ParN + ParP + REG_04, TB + ParJ - REG_05, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParN + ParP - REG_02, TB + ParJ, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParN, TB + ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParO, width - (TB + ParK), 0, 0));
            //Stesso discorso, uso il punto successivo per ottenere la curvatura, il file .Mac utilizza i due punti commentati di seguito
            macroPoint.Add(new ProgramPoint(ParO + ParQ, width - (TB + ParK), 0, ParR));
            //macroPoint.Add(new ProgramPoint(ParO + ParQ - REG_06, width - TB - ParK, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParO + ParQ - REG_08, width - TB - ParK + REG_09, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParO + ParQ - ParK * Math.Tan(radAlfa), width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL, width - (TB - ParH - ParG) * tanE, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL + (TB - ParH - ParG) * tanE, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino interno ala A
            //
            extrusionPlane = "A";
            double chamferDepth = ParD;
            
            ProgramPoint startChamfer = new ProgramPoint(ParI, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParI, offsetY - chamWebDist, 0, 0);            
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParI, topY, 0, 0);
            endChamfer = new ProgramPoint(ParI, offsetY + chamWebDist, 0, 0);
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala B
            //
            extrusionPlane = "B";
            chamferDepth = ParH;

            startChamfer = new ProgramPoint(ParL, 0, 0, 0);
            endChamfer = new ProgramPoint(ParL, offsetY - chamWebDist, 0, 0);
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParL, topY, 0, 0);
            endChamfer = new ProgramPoint(ParL, offsetY + chamWebDist, 0, 0);
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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