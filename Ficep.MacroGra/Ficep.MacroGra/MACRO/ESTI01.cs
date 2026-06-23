using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI01 : EyeMacro
    {

        public ESTI01(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
        }

        //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //
        //  Manca ancora l'implementazione dei parametri M, ALFA, BETA
        //
        //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
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

            double topY = CodePrf == "L" ? SA : SB;
            //
            //  Estrusione anima FF
            //
            double dy = 0,
                   startY = CodePrf != "L" ? TB : TA;

            if (!ParA.IsEqualTo(0, TolLinear))
                dy = ParD * ParE / ParA;

            macroPoint.Add(new ProgramPoint(0, startY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, startY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            if (CodePrf != "L")
                macroPoint.Add(new ProgramPoint(ParE, ParD - dy, 0, ParN));
            else
                macroPoint.Add(new ProgramPoint(ParE, ParD - dy, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0, 0));

            string extrusionPlane = CodePrf != "L" ? "C" : "B";
            double extrusionDepth = topY;

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            //  Estrusione anima FM
            //
            dy = 0;
            if (!ParF.IsEqualTo(0, TolLinear))
                dy = ParI * ParL / ParF;

            if (CodePrf != "L")
            { 
                macroPoint.Add(new ProgramPoint(0, SA - ParI - ParH, 0, 0));
                macroPoint.Add(new ProgramPoint(ParL, SA - ParI + dy, 0, ParO));
                macroPoint.Add(new ProgramPoint(ParF, SA - ParG, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParF, SA - TB, 0, 0));
                macroPoint.Add(new ProgramPoint(0, SA - TB, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(0, SB - ParI - ParH, 0, 0));
                macroPoint.Add(new ProgramPoint(ParL, SB - ParI + dy, 0, 0));
                macroPoint.Add(new ProgramPoint(ParF, SB - ParG, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParF, SB, 0, 0));
                macroPoint.Add(new ProgramPoint(0, SB, 0, 0));
            }
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            double radAlfa = MirrorInizialeFinale ? -ParALFA.ToRad() : ParALFA.ToRad(),
                   radBeta = MirrorInizialeFinale ? -ParBETA.ToRad() : ParBETA.ToRad();

            //
            //  Estrusione ala A
            //
            double dxA = (CodePrf == "I" ? SB / 2 : topY) * Math.Tan(radAlfa);

            ProgramPoint startChamfer = null, endChamfer = null;
            if (CodePrf == "I")
            {
                startChamfer = new ProgramPoint(ParA + dxA, 0, 0, 0);
                endChamfer = new ProgramPoint(ParA - dxA, topY, 0, 0);

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(startChamfer);
                macroPoint.Add(endChamfer);
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            }
            else
            {
                startChamfer = new ProgramPoint(ParA - TolWebFlange, 0, 0, 0);
                endChamfer = new ProgramPoint(ParA - dxA, topY, 0, 0);

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(startChamfer);
                macroPoint.Add(endChamfer);
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            }

            extrusionPlane = "A";
            extrusionDepth = (CodePrf == "L" ? TA : TB) + (radAlfa.IsEqualTo(0, TolAngle) ? 0 : Radius);

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            //**************************************************************************************************************
            // PUT POLYGON 1 IN BREPS LIST AFTER THE SOLID ON FLANGE A IN ORDER TO AVOID BREP.DIFFERENCE FUNCTION FAILURE
            //
            Brep temp = breps[0];
            breps.RemoveAt(0);
            breps.Add(temp);
            //
            //**************************************************************************************************************
            //

            //
            // Cianfrino ala A
            //
            if (!ParM.IsEqualTo(0, TolAngle))
            { 
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParM.ToRad(), TB, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            //  Estrusione ala B
            //
            if (CodePrf != "L")
            {
                double dxB = CodePrf == "I" ? SB / 2 * Math.Tan(radBeta) : SB * Math.Tan(radAlfa);
                if (CodePrf == "I")
                {
                    startChamfer = new ProgramPoint(ParF + dxB, 0, 0, 0);
                    endChamfer = new ProgramPoint(ParF - dxB, topY, 0, 0);
                }
                else
                {
                    startChamfer = new ProgramPoint(ParF - TolWebFlange, 0, 0, 0);
                    endChamfer = new ProgramPoint(ParF - dxB, topY, 0, 0);
                }
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(startChamfer);
                macroPoint.Add(endChamfer);
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

                extrusionPlane = "B";
                extrusionDepth = TB + (radBeta.IsEqualTo(0, TolAngle) ? 0 : Radius);

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

                //
                // Cianfrino ala B
                //
                if (!ParM.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParM.ToRad(), TB, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB))
                        breps.Add(chamferB);
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
