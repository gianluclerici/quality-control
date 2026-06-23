using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI05 : EyeMacro
    {

        public SCAI05(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUQ";
        }

        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fissa
            ///////////////////////////////
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;
            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double extrusionDepth = 0;
            string extrusionPlane = "";

            //
            //  ESTRUSIONE ANIMA
            //
            if (ParR > 0)
            {
                extrusionDepth = TA + 2 * Radius;
                extrusionPlane = "C";

                macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParR, TB, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, TB + ParR, 0, 0, ParR));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }

            //
            // Cianfrino
            //
            // Punti su cui eseguire il cianfrino
            ProgramPoint startExtChamfer = new ProgramPoint(0, ParD, 0, 0),
                         endExtChamfer = new ProgramPoint(0, SB - ParD, 0, 0),
                         startIntInfChamfer = null,
                         endIntInfChamfer = new ProgramPoint(0, SB / 2 - TA / 2 - Radius - ParB, 0, 0),
                         startIntSupChamfer = new ProgramPoint(0, SB / 2 + TA / 2 + Radius + ParB, 0, 0),
                         endIntSupChamfer = endExtChamfer;

            extrusionPlane = Side;

            if (!ParALFA.IsEqualTo(0, TolAngle))
            { 
                if(EyeGeometryUtils.AddExternalChamfer(startExtChamfer, endExtChamfer, Wp, extrusionPlane, ParALFA.ToRad(), TB - ParA - ParC, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep extChamfer))
                    breps.Add(extChamfer);
            }

            if (!ParBETA.IsEqualTo(0,TolAngle))
            {
                bool bigRadius = ParR > 35;
                bool singleInternalChamfer = bigRadius || CodePrf != "I";

                if (CodePrf == "I")
                    startIntInfChamfer = startExtChamfer;
                else
                {
                    if (singleInternalChamfer && bigRadius)
                        startIntInfChamfer = new ProgramPoint(0, 0, 0, 0);
                    else
                        startIntInfChamfer = new ProgramPoint(0, TA + Radius, 0, 0);
                }

                if (singleInternalChamfer)
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startIntInfChamfer, endIntSupChamfer, Wp, extrusionPlane, ParALFA.ToRad(), TB - ParA - ParC, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep IntInfChamfer))
                        breps.Add(IntInfChamfer);
                }
                else
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startIntInfChamfer, endIntInfChamfer, Wp, extrusionPlane, ParALFA.ToRad(), TB - ParA - ParC, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep IntInfChamfer))
                        breps.Add(IntInfChamfer);

                    if (EyeGeometryUtils.AddInternalChamfer(startIntSupChamfer, endIntSupChamfer, Wp, extrusionPlane, ParALFA.ToRad(), TB - ParA - ParC, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep IntSupChamfer))
                        breps.Add(IntSupChamfer);
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
