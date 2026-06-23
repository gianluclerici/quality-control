using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI38 : EyeMacro
    {
        public SCAI38(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double radALFA = MirrorInizialeFinale ? -ParALFA.ToRad() : ParALFA.ToRad();
            double extrusionDepth = 0;
            string extrusionPlane = "";

            //
            // Estrusione anima
            //
            extrusionDepth = SB;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParE, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, ParB + ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + ParD + ParF, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();
            //
            //  ESTRUSIONE ALA INFERIORE
            //
            extrusionPlane = Side;
            extrusionDepth = TB;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (SB / 2) * radALFA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (TB / 2 + ParM) * radALFA, SB / 2 - TB / 2 - ParM, 0, 0));
            macroPoint.Add(new ProgramPoint(0, SB / 2 - TB / 2 - ParM, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            //  ESTRUSIONE ALA SUPERIORE
            //

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            macroPoint.Add(new ProgramPoint(0, SB / 2 + TB / 2 + ParM, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (TB / 2 + ParM) * radALFA, SB / 2 + TB / 2 + ParM, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (SB / 2) * radALFA, SB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, SB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            //  Punti linee di cianfrinatura
            //
            ProgramPoint bottomChamfer = new ProgramPoint(ParA + (SB / 2) * radALFA, 0, 0, 0);
            ProgramPoint bottomWebChamfer = new ProgramPoint(ParA + (TB / 2 + ParM) * radALFA, SB / 2 - TB / 2 - ParM - TolWebFlange, 0, 0);
            ProgramPoint topWebChamfer = new ProgramPoint(ParA - (TB / 2 + ParM) * radALFA, SB / 2 + TB / 2 + ParM + TolWebFlange, 0, 0);
            ProgramPoint topChamfer = new ProgramPoint(ParA - (SB / 2) * radALFA, SB, 0, 0);

            // 
            // Flag passato alle funzioni del cianfrino per specificare di non eseguire il cianfrino normale alla curva.
            // Quindi il cianfrino sarà parallelo al piano
            //
            bool normalToRailCurve = false;

            //
            // Cianfrino esterno
            //
            if (!ParI.IsEqualTo(0, TolAngle))
            {
                double chamferDepth = TB - ParG - ParH;

                //
                // Cianfrino esterno ala inferiore
                //
                if (EyeGeometryUtils.AddExternalChamfer(bottomChamfer, bottomWebChamfer, Wp, extrusionPlane, ParI.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, normalToRailCurve))
                    breps.Add(chamferA);

                //
                // Cianfrino esterno ala superiore
                //
                if (EyeGeometryUtils.AddExternalChamfer(topWebChamfer, topChamfer, Wp, extrusionPlane, ParI.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB, false, normalToRailCurve))
                    breps.Add(chamferB);
            }

            //
            // Cianfrino interno
            //
            if (!ParL.IsEqualTo(0, TolAngle))
            {
                double chamferDepth = ParG;

                //
                // Cianfrino interno ala inferiore
                //
                if (EyeGeometryUtils.AddInternalChamfer(bottomChamfer, bottomWebChamfer, Wp, extrusionPlane, ParL.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, false, normalToRailCurve))
                    breps.Add(chamferA);

                //
                // Cianfrino interno ala superiore
                //
                if (EyeGeometryUtils.AddInternalChamfer(topWebChamfer, topChamfer, Wp, extrusionPlane, ParL.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferB, false, normalToRailCurve))
                    breps.Add(chamferB);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}