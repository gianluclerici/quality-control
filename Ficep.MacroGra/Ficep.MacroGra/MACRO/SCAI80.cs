using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI80 : EyeMacro
    {
        public SCAI80(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            EyeParam eyeParam = new EyeParam(TolLinear, TolAngle, TolBrep, TolWebFlange, Surplus, InnerChamferDisFromWeb);

            List<EyeFeature> features = new List<EyeFeature>();
            bool returnValue = SCXX80(Wp, Params, eyeParam, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, ref features);

            Features = features;

            return returnValue;
        }

        public static bool SCXX80(IWorkPiece wp, ICopeParams param, EyeParam eyeParam, bool MirrorInizialeFinale, bool MirrorSideASideB, bool MirrorAltoBasso, ref List<EyeFeature> Features)
        {
            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double topY = wp.Prf.SB, offsetY = wp.Prf.SB / 2, width = wp.Prf.SA;

            double ParA = param.A, ParB = param.B, ParC = param.C, ParD = param.D, ParE = param.E, ParF = param.F, ParALFA = param.ALFA, ParBETA = param.BETA;

            double actualR = param.R.IsEqualTo(offsetY, eyeParam.Tol.Linear) ? param.R + eyeParam.Surplus / 2 : param.R;

            double chamferR = MirrorSideASideB ? -actualR : actualR;

            double extrusionDepth = wp.Prf.SB;
            string extrusionPlane = "C";
            
            // offsetXFF calculation
            double offsetXFF = 0; // REG_ 01
            double chamferXFF = 0; // REG_02
            if (actualR >= offsetY && actualR >= ParA) // La validate impedirà actualR < ParA
            {
                offsetXFF = actualR - Math.Sqrt(actualR * actualR - (offsetY + eyeParam.Surplus) * (offsetY + eyeParam.Surplus));
                chamferXFF = actualR - Math.Sqrt(actualR * actualR - (wp.Prf.TA / 2 + eyeParam.InnerChamferDisFromWeb) * (wp.Prf.TA / 2 + eyeParam.InnerChamferDisFromWeb));
            }

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, wp.Prf.TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, wp.Prf.TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB - ParF, 0, 0));
            if (!ParE.IsEqualTo(0, eyeParam.Tol.Angle))
                macroPoint.Add(new ProgramPoint(ParA - ParE, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, wp, eyeParam.Tol.Brep, eyeParam.Tol.Linear, eyeParam.Tol.Angle, eyeParam.Tol.WebFlange, ref breps, eyeParam.Surplus);
            macroPoint.Clear();

            //
            //  Estrusione ala Side fatta direttamente utilizzando il cerchio
            //
            extrusionDepth = wp.Prf.TB;
            extrusionPlane = param.SIDE;

            Brep feature = null;
            Point2D centre = new Point2D(ParA - actualR, offsetY);

            EyeGeometryUtils.AddCircleExtrusion(centre, actualR, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, wp, eyeParam.Tol.Brep, eyeParam.Tol.Linear, eyeParam.Tol.Angle, eyeParam.Tol.WebFlange, ref feature, eyeParam.Surplus);
            Features.Add(new EyeFeature(feature));

            //
            // Cianfrino esterno ala Side
            //
            double chamferDepth = wp.Prf.TB - ParD - ParC;

            ProgramPoint startChamfer = new ProgramPoint(ParA - offsetXFF, - eyeParam.Surplus, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA - offsetXFF, topY + eyeParam.Surplus, 0, 0, chamferR);

            if (!ParALFA.IsEqualTo(0, eyeParam.Tol.Angle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, eyeParam.Surplus, eyeParam.Tol.Brep, eyeParam.Tol.Linear, eyeParam.Tol.WebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala Side
            //
            chamferDepth = ParD;

            startChamfer = new ProgramPoint(ParA - offsetXFF, -eyeParam.Surplus, 0, 0);
            endChamfer = new ProgramPoint(ParA - chamferXFF, offsetY - (wp.Prf.TA / 2 + eyeParam.InnerChamferDisFromWeb), 0, 0, chamferR);

            if (!ParBETA.IsEqualTo(0, eyeParam.Tol.Angle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, eyeParam.Surplus, eyeParam.Tol.Brep, eyeParam.Tol.Linear, eyeParam.Tol.WebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            startChamfer = new ProgramPoint(ParA - chamferXFF, offsetY + (wp.Prf.TA / 2 + eyeParam.InnerChamferDisFromWeb), 0, 0);
            endChamfer = new ProgramPoint(ParA - offsetXFF, topY + eyeParam.Surplus, 0, 0, chamferR);

            if (!ParBETA.IsEqualTo(0, eyeParam.Tol.Angle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, eyeParam.Surplus, eyeParam.Tol.Brep, eyeParam.Tol.Linear, eyeParam.Tol.WebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

        public override ErrMacro Validate()
        {
            // 
            // Il Raggio non può essere < SB /2. Inoltre internamente alla macro il raggio verrà saturato a SB / 2 + surplus / 2
            // se R = SB / 2 
            //

            return base.Validate();
        }

    }
}