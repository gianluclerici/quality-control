using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA10 : EyeMacro
    {
        public ESTIA10(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
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

            double extrusionDepth;
            string extrusionPlane;
            double chamferDepth;
            ProgramPoint startChamfer, endChamfer;

            double offsetY = CodePrf == "I" ? SB / 2 : 0,
                   topY = CodePrf == "L" ? SA : SB,
                   halfWeb = TA / 2 + InnerChamferDisFromWeb;

            double actualC = VY == "A" ? ParC : ParA, actualA = VY == "A" ? ParA : ParC;
            // gamma è l'angolo formato da ParC e ParB nel caso ALTO e tra ParA e ParB nel caso BASSO
            double tanGamma = actualC / (ParB + offsetY), radGamma = Math.Atan(tanGamma), sinGamma = Math.Sin(radGamma), cosGamma = Math.Cos(radGamma);

            //
            // Estrusione ala Side BASSA
            //
            extrusionPlane = "A";
            extrusionDepth = CodePrf == "L" ? SB : SA;

            macroPoint.Add(new ProgramPoint(0, offsetY + ParB, 0, 0));
            if(CodePrf == "I")
            {
                macroPoint.Add(new ProgramPoint(actualC, 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(actualC + ParR, 0));
            }
            else
                macroPoint.Add(new ProgramPoint(actualC, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala Side ALTA
            //
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(actualA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, offsetY + ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //  CIANFRINI (solo profilo a I)
            if (CodePrf == "I")
            {
                if (!ParALFA.IsEqualTo(0, TolAngle))
                {
                    chamferDepth = TB - ParD;

                    // Cianfrino esterno ala A basso
                    startChamfer = new ProgramPoint(actualC, 0, 0, 0);
                    endChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);

                    // Cianfrino esterno ala A alto
                    startChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);
                    endChamfer = new ProgramPoint(actualA, topY, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                        breps.Add(chamferA);
                }
                if (!ParBETA.IsEqualTo(0, TolAngle))
                {
                    chamferDepth = ParE;

                    // Cianfrino interno ala A basso
                    startChamfer = new ProgramPoint(actualC, 0, 0, 0);
                    endChamfer = new ProgramPoint((ParB + halfWeb) * tanGamma, offsetY - halfWeb, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth,
                        MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                        breps.Add(chamferA);

                    startChamfer = new ProgramPoint((ParB - halfWeb) * tanGamma, offsetY + halfWeb, 0, 0);
                    endChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth,
                        MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, false, false))
                        breps.Add(chamferA);

                    // Cianfrino interno ala A alto
                    startChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);
                    endChamfer = new ProgramPoint(actualA, topY, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth,
                        MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, false, false))
                        breps.Add(chamferA);
                }

                extrusionPlane = "B";

                if (!ParF.IsEqualTo(0, TolAngle))
                {
                    chamferDepth = TB - ParL;

                    // Cianfrino esterno ala B                
                    startChamfer = new ProgramPoint(actualC, 0, 0, 0);
                    endChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);

                    startChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);
                    endChamfer = new ProgramPoint(actualA, topY, 0, 0);

                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParF.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                        breps.Add(chamferA);
                }
                if (!ParG.IsEqualTo(0, TolAngle))
                {
                    chamferDepth = ParH;

                    // Cianfrino interno ala B basso
                    startChamfer = new ProgramPoint(actualC, 0, 0, 0);
                    endChamfer = new ProgramPoint((ParB + halfWeb) * tanGamma, offsetY - halfWeb, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth,
                        MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, false))
                        breps.Add(chamferA);

                    startChamfer = new ProgramPoint((ParB - halfWeb) * tanGamma, offsetY + halfWeb, 0, 0);
                    endChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth,
                        MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, false, false))
                        breps.Add(chamferA);

                    // Cianfrino interno ala B alto
                    startChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0);
                    endChamfer = new ProgramPoint(actualA, topY, 0, 0);

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth,
                        MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferC, false, false))
                        breps.Add(chamferC);
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