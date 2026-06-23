
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System.Collections.Generic;
using Ficep.Utils;
using System.Linq;
using System;

namespace Ficep.MacroGra
{
	public class ESTI11 : EyeMacro
	{

		public ESTI11(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        public override bool CreateMacro()
		{
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
            //  Estrusione anima
            //
            ProgramPoint startWebChamfer = new ProgramPoint(ParI, TB + ParR + ParJ, 0, 0),
                         endWebChamfer = new ProgramPoint(ParI, SA - TB - ParR - ParJ, 0, 0);
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParN + ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParN + ParR, TB + ParR, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParI + ParN, TB + ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParK, TB + ParR, 0, 0));
            macroPoint.Add(startWebChamfer);
            macroPoint.Add(endWebChamfer);
            macroPoint.Add(new ProgramPoint(ParI + ParK, SA - TB - ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParN , SA - TB - ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI + ParN + ParR, SA - TB - ParR, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParI + ParN + ParR, SA - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, SA - TB, 0, 0));

            string extrusionPlane = "C";
            double extrusionDepth = TA + 2 * Radius;

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino anima superiore e inferiore
            //
            if(EyeGeometryUtils.AddExternalChamfer(startWebChamfer, endWebChamfer, Wp, extrusionPlane, ParALFA.ToRad(), ParL, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep webChamfer))
                breps.Add(webChamfer);
            if (EyeGeometryUtils.AddInternalChamfer(startWebChamfer, endWebChamfer, Wp, extrusionPlane, ParBETA.ToRad(), TA - ParS - ParL, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out webChamfer))
                breps.Add(webChamfer);

            //  Estrusione ala A
            //
            extrusionPlane = "A";

            //
            // Cianfrino ala A
            //

            // Cianfrino esterno 
            Brep chamferA = null, 
                 chamferB = null;
            ProgramPoint startExtChamfer = new ProgramPoint(0, 0, 0, 0),
                         endExtChamfer = new ProgramPoint(0, SB, 0, 0),
                         startIntInferiorChamfer = startExtChamfer,
                         endIntInferiorChamfer = new ProgramPoint(0, SB / 2 - TA / 2 - ParM, 0, 0),
                         startIntSuperiorChamfer = new ProgramPoint(0, SB / 2 + TA / 2 + ParM, 0, 0),
                         endIntSuperiorChamfer = endExtChamfer;

            if (!ParA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startExtChamfer, endExtChamfer, Wp, "A", ParA.ToRad(), TB - ParC - ParD, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
            }
            if (!ParE.IsEqualTo(0, TolAngle))
            { 
                if (EyeGeometryUtils.AddExternalChamfer(startExtChamfer, endExtChamfer, Wp, "B", ParE.ToRad(), TB - ParG - ParH, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferB))
                    breps.Add(chamferB);
            }

            // Cianfrino interno ala A
            if (!ParB.IsEqualTo(0, TolAngle))
            { 
                if  (ParR.IsEqualTo (0, TolLinear))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startIntInferiorChamfer, endExtChamfer, Wp, "A", ParB.ToRad(), ParD, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                        breps.Add(chamferA);
                    if (EyeGeometryUtils.AddInternalChamfer(startIntSuperiorChamfer, endIntSuperiorChamfer, Wp, "A", ParB.ToRad(), ParD, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                        breps.Add(chamferA);
                }
                else
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startIntInferiorChamfer, endIntSuperiorChamfer, Wp, "A", ParB.ToRad(), ParD, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                        breps.Add(chamferA);
                }
            }

            // Cianfrino interno ala B
            if (!ParF.IsEqualTo(0, TolAngle))
            {
                if (ParR.IsEqualTo(0, TolLinear))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startIntInferiorChamfer, endIntInferiorChamfer, Wp, "B", ParF.ToRad(), ParH, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferB))
                        breps.Add(chamferB);
                    if (EyeGeometryUtils.AddInternalChamfer(startIntSuperiorChamfer, endIntSuperiorChamfer, Wp, "B", ParF.ToRad(), ParH, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferB))
                        breps.Add(chamferB);
                }
                else
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startIntInferiorChamfer, endIntSuperiorChamfer, Wp, "B", ParF.ToRad(), ParH, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferB))
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
