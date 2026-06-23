using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI17 : EyeMacro
    {

        public SCAI17(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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



            double extrusionDepth = CodePrf == "L" ? SA : SB;
            string extrusionPlane = CodePrf == "L" ? "B" : "C";

            double topY = CodePrf == "L" ? SA : SB, offsetY = CodePrf == "L" || CodePrf == "U" ? 0 : SB / 2;

            double intLowerChamferDist = ParD > InnerChamferDisFromWeb ? TA / 2 + ParD : TA / 2 + InnerChamferDisFromWeb;
            double intUpperChamferDist = ParC > InnerChamferDisFromWeb ? ParC : InnerChamferDisFromWeb;
            if (CodePrf == "L")
                intUpperChamferDist += TB;
            else if (CodePrf == "U")
                intUpperChamferDist += TA;
            else
                intUpperChamferDist += TA / 2;
            double absParR = Math.Abs(ParR); // serve per mantenere dimensioni solido in caso di ParR negativo

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + absParR, 0, 0));
            macroPoint.Add(new ProgramPoint(absParR, ParB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //// Considero paramento C e D che partono a filo anima(SB / 2 +- TA /2) e non da centro anima (SB / 2)
            
            //
            // Cianfrino esterno ala Side Alto
            //
            extrusionPlane = Side;
            double chamferDepth = (CodePrf != "L" ? TB : TA) - ParE - ParF;

            ProgramPoint startChamfer = new ProgramPoint(ParA, offsetY + intUpperChamferDist, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala Side Basso
            //
            if (CodePrf == "I")
            {
                startChamfer = new ProgramPoint(ParA, 0, 0, 0);
                endChamfer = new ProgramPoint(ParA, offsetY - intLowerChamferDist, 0, 0);

                if (!ParALFA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }

            //
            // Cianfrino interno ala Side Alto
            //
            chamferDepth = ParF;

            startChamfer = new ProgramPoint(ParA, offsetY + intUpperChamferDist, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side Basso
            //
            if (CodePrf == "I")
            {
                startChamfer = new ProgramPoint(ParA, 0, 0, 0);
                endChamfer = new ProgramPoint(ParA, offsetY - intLowerChamferDist, 0, 0);

                if (!ParBETA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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