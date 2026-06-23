using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA136 : EyeMacro
    {
        public ESTIA136(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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


            double topY = SB, offsetY = SB / 2, width = SA;

            double radAlfa = MirrorInizialeFinale ? -ParALFA.ToRad() : ParALFA.ToRad();
            double tanAlfa = Math.Tan(radAlfa);

            double radBeta = ParBETA.ToRad();
            //
            //	xFM is the X position of the upper mobile wing core cut.
            //
            double xFM = 0;
            if (ParALFA >= 0)
            {
                xFM = (offsetY - TA / 2) * Math.Abs(tanAlfa);
            }
            else
            {
                xFM = (offsetY + TA / 2) * Math.Abs(tanAlfa);
            }
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0));
            //macroPoint.Add(new ProgramPoint(ParC + (offsetY + TA / 2) * tanAlfa + TB * Math.Tan(Math.Abs(ParBETA)), 0, 0));
            //macroPoint.Add(new ProgramPoint(ParC + (offsetY + TA / 2) * tanAlfa, TB, 0));
            macroPoint.Add(new ProgramPoint(ParC, TB, 0));
            macroPoint.Add(new ProgramPoint(ParC, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(xFM, ParB, 0));
            macroPoint.Add(new ProgramPoint(xFM, width - TB, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Estrusione ala FF
            //
            extrusionDepth = TB;
            extrusionPlane = Side;
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (offsetY + TA / 2) * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (offsetY - TA / 2) * tanAlfa, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radBeta);
            macroPoint.Clear();
            //
            // Estrusione ala FM
            //
            extrusionDepth = TB;
            extrusionPlane = Side == "A" ? "B" : "A";// avendo specchiato forzatamente A e B l'estrusione è stata fatta con false al posto di MirrorSideASideB
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                macroPoint.Add(new ProgramPoint(0, 0, 0));
                macroPoint.Add(new ProgramPoint(topY * Math.Abs(tanAlfa), radAlfa > 0 ? 0 : topY, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}