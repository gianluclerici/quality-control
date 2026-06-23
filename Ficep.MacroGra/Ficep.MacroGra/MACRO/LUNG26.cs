using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class LUNG26 : EyeMacroLung
    {
        public LUNG26(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double halfwidth = SA / 2;
            double extrusionDepth = SB;
            string extrusionPlane = "C";

            // Circular arc x = 0
            macroPoint.Add(new ProgramPoint(0, halfwidth - ParR, 0));
            macroPoint.Add(new ProgramPoint(ParR, halfwidth, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, halfwidth + ParR, 0, 0, ParR));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            // Circular arc x = Lp
            macroPoint.Add(new ProgramPoint(Lp, halfwidth - ParR, 0));
            macroPoint.Add(new ProgramPoint(Lp - ParR, halfwidth, 0, 0, -ParR));
            macroPoint.Add(new ProgramPoint(Lp, halfwidth + ParR, 0, 0, -ParR));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            double currentCutX = ParA;

            // Initial linear segment
            macroPoint.Add(new ProgramPoint(ParR, halfwidth, 0));
            macroPoint.Add(new ProgramPoint(currentCutX, halfwidth, 0));
            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();
            while (currentCutX + ParB < Lp)
            {
                // Internal cut
                macroPoint.Add(new ProgramPoint(currentCutX, halfwidth, 0));
                macroPoint.Add(new ProgramPoint(currentCutX, halfwidth - ParD, 0));
                macroPoint.Add(new ProgramPoint(currentCutX + 2 * ParR, halfwidth - ParD, 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(currentCutX + ParB - 2 * ParR, halfwidth - ParD, 0));
                macroPoint.Add(new ProgramPoint(currentCutX + ParB, halfwidth - ParD, 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(currentCutX + ParB, halfwidth, 0));
                macroPoint.Add(new ProgramPoint(currentCutX + ParB, halfwidth + ParD, 0));
                macroPoint.Add(new ProgramPoint(currentCutX + ParB - 2 * ParR, halfwidth + ParD, 0, 0, ParR));
                macroPoint.Add(new ProgramPoint(currentCutX + 2 * ParR, halfwidth + ParD, 0));
                macroPoint.Add(new ProgramPoint(currentCutX, halfwidth + ParD, 0, 0, ParR));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();

                // Linear segment
                if (currentCutX + 2 * ParB + ParC < Lp)
                {
                    macroPoint.Add(new ProgramPoint(currentCutX + ParB, halfwidth, 0));
                    macroPoint.Add(new ProgramPoint(currentCutX + ParB + ParC, halfwidth, 0));
                }
                else// Final linear segment
                {
                    macroPoint.Add(new ProgramPoint(currentCutX + ParB, halfwidth, 0));
                    macroPoint.Add(new ProgramPoint(Lp - ParR, halfwidth, 0));
                }
                EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
                ProgrammedCurves.AddRange(curves);
                macroPoint.Clear();
                curves.Clear();
                currentCutX = currentCutX + ParB + ParC;
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}