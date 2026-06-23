using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI48 : EyeMacro
    {

        public SCAI48(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //
            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double topY = SB, offsetY = SB / 2;

            if (ParC > 0 && ParR > 0)
            {
                ////////////////////////////
                //Calcolo angolo Rat Hole
                ////////////////////////////
                double ratHoleAngle = 0, root = 0;

                double A = ParB, B = ParC - ParA - ParR, C = 2 * ParR - ParB;
                if (Solve2(A, B, C, ref root))
                    ratHoleAngle = 2 * Math.Atan(Math.Abs(root));

                macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParC - ParR + ParR * Math.Sin(ratHoleAngle), TB + ParB - ParR - ParR * Math.Cos(ratHoleAngle), 0, 0));
                macroPoint.Add(new ProgramPoint(ParC - ParR, TB + ParB, 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(0, TB + ParB, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }

            //
            // Cianfrino esterno ala A
            //
            extrusionPlane = Side;
            double chamferDepth = TB - ParD - ParE;
            
            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, topY, 0, 0);
            Brep chamferA = null;

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala A
            //
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                extrusionPlane = Side;
                chamferDepth = ParE;

                startChamfer = new ProgramPoint(0, 0, 0, 0);
                endChamfer = new ProgramPoint(0, topY, 0, 0);

                if (ParA > ParE * Math.Tan(ParBETA.ToRad()))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                        breps.Add(chamferA);
                }
                else
                {
                    double intChamferDist = TA / 2 + InnerChamferDisFromWeb;
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, new ProgramPoint(0, offsetY - intChamferDist, 0, 0), Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                        breps.Add(chamferA);

                    if (EyeGeometryUtils.AddInternalChamfer(endChamfer, new ProgramPoint(0, offsetY + intChamferDist, 0, 0), Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
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