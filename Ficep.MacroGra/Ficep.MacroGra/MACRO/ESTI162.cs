using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI162 : EyeMacro
    {
        public ESTI162(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "Q";
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

            double width = SA, topY = SB;
            //
            // Estrusione passante da piano C
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, TB - ParG, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + (ParG + ParP) * Math.Tan(ParE.ToRad()), TB + ParP, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParC, TB + ParP, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParC, TB + ParP + ParC, 0, ParC));
            macroPoint.Add(new ProgramPoint(ParB, TB + ParP + ParC));
            macroPoint.Add(new ProgramPoint(ParB - ParC, TB + ParP + ParC, 0, ParC));
            macroPoint.Add(new ProgramPoint(ParB - ParC, TB + ParP, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB + ParP, 0, 0));
            macroPoint.Add(new ProgramPoint(0, TB + ParA + ParP, 0, 0, ParA));
            macroPoint.Add(new ProgramPoint(0, width - (TB + ParH + ParP), 0, 0));
            macroPoint.Add(new ProgramPoint(ParH, width - (TB + ParP), 0, 0, ParH));
            macroPoint.Add(new ProgramPoint(ParI - ParJ, width - (TB + ParP), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI - ParJ, width - (TB + ParP + ParJ), 0, ParJ));
            macroPoint.Add(new ProgramPoint(ParI, width - (TB + ParP + ParJ), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParJ, width - (TB + ParP + ParJ), 0, ParJ));
            macroPoint.Add(new ProgramPoint(ParI + ParJ, width - (TB + ParP)));
            macroPoint.Add(new ProgramPoint(ParI + (ParN + ParP) * Math.Tan(ParL.ToRad()), width - (TB + ParP), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, width - (TB - ParN), 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);


            //
            // Cianfrino esterno ala FF
            //
            extrusionPlane = "A";
            double chamferDepth = TB - ParF - ParG;

            ProgramPoint startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParB, topY, 0, 0);

            if (!ParD.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParD.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParN - ParM;

            startChamfer = new ProgramPoint(ParI, 0, 0, 0);
            endChamfer = new ProgramPoint(ParI, topY, 0, 0);
            if (!ParK.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParK.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno piano C 
            //
            extrusionPlane = "C";
            chamferDepth = TA - ParO;

            startChamfer = new ProgramPoint(0, 0, 0, 0);
            endChamfer = new ProgramPoint(0, width, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno piano D PROVVISORIAMENTE COMMENTATO
            //
            //extrusionPlane = "D";
            //
            //if (!ParALFA.IsEqualTo(0, TolAngle))
            //{
            //    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
            //        breps.Add(chamferA);
            //}


            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}