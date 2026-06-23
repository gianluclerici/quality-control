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
    public class INTC04 : EyeMacro
    {

        public INTC04(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IQ";
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
            //  Definizioni variabili per calcoli comuni
            //
            double xc = ParE, yc = ParF;

            /* Punto 1 */
            double x1 = xc - ParA / 2 - ParG;
            double y1 = yc - ParB / 2 - ParG;
            /* Punto 2 */
            double x2 = x1;
            double y2 = yc + ParB / 2 + ParG;
            /* Punto 3 */
            double x3 = xc - ParA / 2 + ParD + ParG;
            double y3 = y2;
            /* Punto 4 */
            double x4 = x3;
            double y4 = yc + ParC / 2 + ParG;
            /* Punto 5 */
            double x5 = xc + ParA / 2 - ParD - ParG;
            double y5 = y4;
            /* Punto 6 */
            double x6 = x5;
            double y6 = y3;
            /* Punto 7 */
            double x7 = xc + ParA / 2 + ParG;
            double y7 = y6;
            /* Punto 8 */
            double x8 = x7;
            double y8 = y1;
            /* Punto 9 */
            double x9 = x5;
            double y9 = y8;
            /* Punto 10 */
            double x10 = x9;
            double y10 = yc - ParC / 2 - ParG;
            /* Punto 11 */
            double x11 = x4;
            double y11 = y10;
            /* Punto 12 */
            double x12 = x11;
            double y12 = y1;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            macroPoint.Add(new ProgramPoint(x1, y1, 0, 0));
            macroPoint.Add(new ProgramPoint(x2, y2, 0, 0));
            macroPoint.Add(new ProgramPoint(x3, y3, 0, 0));
            macroPoint.Add(new ProgramPoint(x4, y4, 0, ParR - ParG));
            macroPoint.Add(new ProgramPoint(x5, y5, 0, ParR - ParG));
            macroPoint.Add(new ProgramPoint(x6, y6, 0, 0));
            macroPoint.Add(new ProgramPoint(x7, y7, 0, 0));
            macroPoint.Add(new ProgramPoint(x8, y8, 0, 0));
            macroPoint.Add(new ProgramPoint(x9, y9, 0, 0));
            macroPoint.Add(new ProgramPoint(x10, y10, 0, ParR - ParG));
            macroPoint.Add(new ProgramPoint(x11, y11, 0, ParR - ParG));
            macroPoint.Add(new ProgramPoint(x12, y12, 0, 0));

            //
            //  ESTRUSIONE
            //
            double extrusionDepth = TA;
            string extrusionPlane = "C";
            List<Brep> breps = new List<Brep>();

            if (CodePrf == "I" || CodePrf == "Q" && (ParI == 0 || ParI == 2))
            {
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

                // ROTAZIONE
                if (!ParALFA.IsEqualTo(0, TolAngle))
                {
                    var solid = breps[0];
                    EyeGeometryUtils.RotateSolid(ParALFA.ToRad(), xc, yc, extrusionPlane, Wp, ref solid);
                }
            }

            if (CodePrf == "Q" && (ParI == 1 || ParI == 2))
            {
                extrusionPlane = "D";

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

                // ROTAZIONE
                if (!ParALFA.IsEqualTo(0, TolAngle))
                {
                    var solid = breps[breps.Count() - 1];
                    EyeGeometryUtils.RotateSolid(-ParALFA.ToRad(), xc, yc, extrusionPlane, Wp, ref solid);
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
