using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI52 : EyeMacro
    {

        public ESTI52(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima FF
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParD, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione anima FM
            //
            double width = SA;

            macroPoint.Add(new ProgramPoint(0, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF + ParH, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, width - ParG, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, width - ParI, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala FF
            //
            extrusionDepth = TB;
            extrusionPlane = "A";

            double offsetY = SB / 2, topY = SB;

            double radAlfa = VX == "I" ? ParALFA.ToRad() : - ParALFA.ToRad();
            double tanAlfa = Math.Tan(radAlfa);

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC + offsetY * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParC - offsetY * tanAlfa, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Cianfrino FF
            //
            double chamferDepth = TB;

            double absBeta = Math.Abs(ParBETA.ToRad());
            double chamferSurplus = Radius;

            double distanceFromMidWeb = TA / 2 + InnerChamferDisFromWeb;

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (ParBETA > 0)
                {
                    ProgramPoint startChamfer = new ProgramPoint(ParA + ParC + offsetY * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(ParA + ParC - offsetY * tanAlfa, topY, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, chamferSurplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                else
                {
                    ProgramPoint startChamfer = new ProgramPoint(ParA + ParC + offsetY * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(ParA + ParC + distanceFromMidWeb * tanAlfa, offsetY - distanceFromMidWeb, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                        breps.Add(chamferA);

                    startChamfer = new ProgramPoint(ParA + ParC - distanceFromMidWeb * tanAlfa, offsetY + distanceFromMidWeb, 0, 0);
                    endChamfer = new ProgramPoint(ParA + ParC - offsetY * tanAlfa, topY, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB, false, false))
                        breps.Add(chamferB);
                }
            }

            //
            // Estrusione ala FM
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF + ParH + offsetY * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF + ParH - offsetY * tanAlfa, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino FM
            //
            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (ParBETA > 0)
                {
                    ProgramPoint startChamfer = new ProgramPoint(ParF + ParH + offsetY * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(ParF + ParH - offsetY * tanAlfa, topY, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, chamferSurplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                else
                {
                    ProgramPoint startChamfer = new ProgramPoint(ParF + ParH + offsetY * tanAlfa, 0, 0, 0);
                    ProgramPoint endChamfer = new ProgramPoint(ParF + ParH + distanceFromMidWeb * tanAlfa, offsetY - distanceFromMidWeb, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                        breps.Add(chamferA);

                    startChamfer = new ProgramPoint(ParF + ParH - distanceFromMidWeb * tanAlfa, offsetY + distanceFromMidWeb, 0, 0);
                    endChamfer = new ProgramPoint(ParF + ParH - offsetY * tanAlfa, topY, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, absBeta, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB, false, false))
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