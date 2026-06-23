using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI55 : EyeMacro
    {

        public SCAI55(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = SB, offsetY = SB / 2;

            double intChamferDist = TA / 2 + InnerChamferDisFromWeb;

            ////////////////////////////
            //Calcolo angolo Rat Hole REG_02 on SCAI55.MAC
            ////////////////////////////

            double ratHoleAngle02 = 0, root = 0;

            double A = ParF + ParR, B = ParC -ParG - ParR, C = ParR - ParF;
            if (Solve2(A, B, C, ref root))
                ratHoleAngle02 = 2 * Math.Atan(Math.Abs(root));

            ////////////////////////////
            //Calcolo angolo Rat Hole REG_03 on SCAI55.MAC
            ////////////////////////////
            double ratHoleAngle03 = 0;
            A = ParB - ParF + ParR;
            B = ParC + ParA + ParR;
            C = ParR - ParB + ParF;
            if (Solve2(A, B, C, ref root))
                ratHoleAngle03 = 2 * Math.Atan(Math.Abs(root));

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB + ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParG, TB + ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC - ParR + ParR * Math.Sin(ratHoleAngle02), TB + ParF - ParR * Math.Cos(ratHoleAngle02), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC, TB + ParF, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParC - ParR + ParR * Math.Sin(ratHoleAngle03), TB + ParF + ParR * Math.Cos(ratHoleAngle03), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, TB + ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala Side
            //
            extrusionPlane = Side;
            double chamferDepth = TB - ParE - ParD;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, topY   , 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala Side basso
            //
            chamferDepth = ParE;
            
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - intChamferDist, 0, 0);
            
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            
            //
            // Cianfrino interno ala Side alto
            //

            startChamfer = new ProgramPoint(ParA, offsetY + intChamferDist, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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