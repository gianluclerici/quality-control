using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA145 : EyeMacro
    {
        public ESTIA145(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IU";
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

            double topY = SB,
                   offsetY = SB / 2, 
                   width = SA;

            double webChamferStart = MirrorSideASideB ? width - (TB + TolWebFlange) : TB + TolWebFlange;
            double webChamferEnd = MirrorSideASideB ? TB + TolWebFlange : width - (TB + TolWebFlange);

            double intChamfDist = TA / 2 + InnerChamferDisFromWeb;

            double tanA = Math.Tan(ParA.ToRad());


            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0));
            macroPoint.Add(new ProgramPoint(width * tanA, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino superiore anima
            //
            double chamferDepth = TA - ParE;
            
            ProgramPoint startChamfer = new ProgramPoint((width - TB - TolWebFlange) * tanA, webChamferStart, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint((TB + TolWebFlange) * tanA, webChamferEnd, 0, 0);
            
            if (!ParD.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParD.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                    breps.Add(chamferA);
            }
            
            //
            // Cianfrino esterno ala Side
            //
            extrusionPlane = Side;
            chamferDepth = TB - ParF - ParC;

            startChamfer = new ProgramPoint((width - (TB - ParF - ParC)) * tanA, 0, 0, 0);
            endChamfer = new ProgramPoint((width - (TB - ParF - ParC)) * tanA, topY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Cianfrino interno ala Side
            //
            if ( CodePrf == "I")
            {
                chamferDepth = ParF;

                startChamfer = new ProgramPoint((width - (TB - ParF)) * tanA, 0, 0, 0);
                endChamfer = new ProgramPoint((width - (TB - ParF)) * tanA, offsetY - intChamfDist, 0, 0);

                if (!ParG.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                startChamfer = new ProgramPoint((width - (TB - ParF)) * tanA, topY, 0, 0);
                endChamfer = new ProgramPoint((width - (TB - ParF)) * tanA, offsetY + intChamfDist, 0, 0);

                if (!ParG.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            
            //
            // Cianfrino esterno ala Opposite Side
            //
            extrusionPlane = Side == "A" ? "B" : "A";
            chamferDepth = TB - ParH - ParB;
            
            startChamfer = new ProgramPoint((TB - ParH - ParB) * tanA, 0, 0, 0);
            endChamfer = new ProgramPoint((TB - ParH - ParB) * tanA, topY, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala Opposite Side
            //
            if (CodePrf == "I")
            {
                chamferDepth = ParH;

                startChamfer = new ProgramPoint((TB - ParH) * tanA, 0, 0, 0);
                endChamfer = new ProgramPoint((TB - ParH) * tanA, offsetY - intChamfDist, 0, 0);

                if (!ParG.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                startChamfer = new ProgramPoint((TB - ParH) * tanA, topY, 0, 0);
                endChamfer = new ProgramPoint((TB - ParH) * tanA, offsetY + intChamfDist, 0, 0);

                if (!ParG.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }            
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}