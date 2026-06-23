using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI39 : EyeMacro
    {

        public SCAI39(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            double tanAlfa = Math.Tan(ParALFA.ToRad());
            double actualBeta = VX == "I" ? ParBETA.ToRad() : - ParBETA.ToRad();
            double tanBeta = Math.Tan(actualBeta);

            double offsetX = actualBeta > 0 ? -(TA + Surplus) : 0; // Usato per evitare chegli sfridi rimangano attaccati al pezzo quando actualBeta > 0 ma rimangono problemi quando abs(beta) circa == 90°

            //STRATEGIA 1

            //macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            //macroPoint.Add(new ProgramPoint((ParA / tanAlfa - TB) * tanAlfa - TA * tanBeta / 2, TB, 0, 0));
            //macroPoint.Add(new ProgramPoint(0, (ParA - TA * tanBeta / 2) / tanAlfa, 0, 0));

            //STRATEGIA 2 FILE . MAC

            macroPoint.Add(new ProgramPoint(offsetX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - TA * tanBeta / 2, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX, (ParA - TA * tanBeta / 2) / tanAlfa, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -actualBeta);

            ////
            //// Estrusione ala CHE NON SERVE SE SI SEGUE LA STRATEGIA 2
            ////
            //
            //macroPoint.Clear();
            //
            //extrusionDepth = TB + TolWebFlange;
            //extrusionPlane = Side;
            //
            //macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParA + SB * tanBeta / 2, 0, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParA - SB * tanBeta / 2, SB, 0, 0));
            //macroPoint.Add(new ProgramPoint(0, SB, 0, 0));
            //
            //EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, ParALFA.ToRad());

            //
            // Cianfrino interno ala Side Basso
            //

            extrusionPlane = Side;
            double chamferDepth = ParE;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA + SB * tanBeta / 2 - TB * tanAlfa, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA + (TA / 2 + ParF) * tanBeta - TB * tanAlfa, offsetY - TA / 2 - ParF, 0, 0);
            
            if (!ParC.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParC.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala Side Alto
            //
            extrusionPlane = Side;
            chamferDepth = ParE;

            startChamfer = new ProgramPoint(ParA - (TA / 2 + ParF) * tanBeta - TB * tanAlfa, offsetY + TA / 2 + ParF, 0, 0);
            endChamfer = new ProgramPoint(ParA - SB * tanBeta / 2 - TB * tanAlfa, topY, 0, 0);

            if (!ParC.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParC.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala Side
            //

            chamferDepth = TB - ParD - ParE;
            
            startChamfer = new ProgramPoint(ParA + SB * tanBeta / 2 - TB * tanAlfa + (TB - ParD - ParE) * Math.Tan(ParB.ToRad()), 0, 0, 0);
            endChamfer = new ProgramPoint(ParA - SB * tanBeta / 2 - TB * tanAlfa + (TB - ParD - ParE) * Math.Tan(ParB.ToRad()), SB, 0, 0);
            
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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