using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA87 : EyeMacro
    {
        public ESTIA87(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double tanGamma = ParB / (SB - ParE), radGamma = Math.Atan(tanGamma);
           
            double offsetXFM = (ParD - SB / 2 + TA / 2) * ParA / ParD;
            double offsetXFF = ParA + (SB / 2 - TA / 2 - ParE) * tanGamma - (offsetXFM + ParI);

            double tanAlfa = offsetXFF / (ParG - TB), radAlfa = Math.Atan(tanAlfa);

            double topY = SB, offsetY = SB / 2, width = SA;
            //
            // Estrusione ala Side
            //
            double extrusionDepth = TB;
            string extrusionPlane = Side;
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Cianfrino esterno ala Side
            //
            double chamferDepth = TB;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA, topY);
            ProgramPoint endChamfer = new ProgramPoint(ParA, 0);
            
            if (!radAlfa.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            startChamfer = new ProgramPoint(ParA + ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY - ParE, 0, 0);
            
            if (!radAlfa.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Estrusione ala Opposite Side
            //
            extrusionDepth = TB + Surplus;
            extrusionPlane = Side == "A" ? "B" : "A";
            macroPoint.Add(new ProgramPoint(0, Side == "A" ? 0 : topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, Side == "A" ? 0 : topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, Side == "A" ? ParC : topY - ParC, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            
            extrusionDepth = SA - ParG;
            macroPoint.Add(new ProgramPoint(0, Side == "A" ? topY - ParD : ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, Side == "B" ? 0 : topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, Side == "B" ? 0 : topY, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);            
            macroPoint.Clear();
            
            //
            // Cianfrino interno ala Opposite Side
            //
            chamferDepth = TB;
            
            startChamfer = new ProgramPoint(ParF, 0, 0, 0);
            endChamfer = new ProgramPoint(0, ParC, 0, 0);
            
            if (!radAlfa.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(0, ParC, 0, 0);
            endChamfer = new ProgramPoint(0, topY - ParD, 0, 0);
            
            if (!radAlfa.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Estrusione anima Side
            //
            extrusionDepth = SB;
            extrusionPlane = "C";
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));          
            macroPoint.Add(new ProgramPoint(offsetXFM + ParI + offsetXFF, TB, 0, 0));              
            macroPoint.Add(new ProgramPoint(offsetXFM + ParI, ParG, 0, 0));              
            macroPoint.Add(new ProgramPoint(offsetXFM, ParG + ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParG + ParH, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            //
            // Cianfrino anima
            //
            chamferDepth = TA;
            
            startChamfer = new ProgramPoint(offsetXFM + ParI + offsetXFF, Side == "A" ? TB : (width - TB), 0, 0);
            endChamfer = new ProgramPoint(offsetXFM, Side == "A" ? (ParG + ParI / tanAlfa) : (width - (ParG + ParI / tanAlfa)), 0, 0);
            
            if (!radGamma.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radGamma, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(offsetXFM + ParI, Side == "A" ? ParG : width - ParG, 0, 0);
            endChamfer = new ProgramPoint(0, Side == "A" ? (ParG + ParH + offsetXFM / (ParI / ParH)) : (width - (ParG + ParH + offsetXFM / (ParI / ParH))), 0, 0);
            
            if (!radGamma.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radGamma, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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