using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA147 : EyeMacro
    {
        public ESTIA147(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "U";
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

            double width = SA;
            //
            // Estrusione anima
            //
            //	webAngleFF e' l'angolo di inclinazione torcia nel taglio ala mobile.
            double webAngleFF = Math.Atan(ParB / ParA);
            double sinWebAngleFF = Math.Sin(webAngleFF);
            double cosWebAngleFF = Math.Cos(webAngleFF);

            //	Coeff. angolare retta per i punti 3 e 4.
            double coeff34 = -Math.Tan(Math.PI / 2 - webAngleFF);

            double x2 = ParC - ParG * sinWebAngleFF;
            double y2 = ParG * cosWebAngleFF;

            double x4 = ParD * sinWebAngleFF + ParE * cosWebAngleFF + ParF * sinWebAngleFF - ParH * cosWebAngleFF;
            double y4 = width - ParB - ParD * cosWebAngleFF + ParE * sinWebAngleFF - ParF * cosWebAngleFF - ParH * sinWebAngleFF;
            //	Coordinate X e Y del punto 3 ottenute come intersezione delle rette per 2 e 4 che devono essere perpendicolari.
            double x3 = (y2 - y4 + x2 / coeff34 + coeff34 * x4) / (coeff34 + 1 / coeff34);
            double y3 = y4 + coeff34 * (x3 - x4);

            macroPoint.Add(new ProgramPoint(ParC, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(x2, y2, 0, ParR));
            macroPoint.Add(new ProgramPoint(x3, y3, 0, 0));
            macroPoint.Add(new ProgramPoint(x4, y4, 0, 0));
            macroPoint.Add(new ProgramPoint(x4 + ParH * cosWebAngleFF, y4 + ParH * sinWebAngleFF, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParD * sinWebAngleFF + ParE * cosWebAngleFF, width - ParB - ParD * cosWebAngleFF + ParE * sinWebAngleFF, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParD * sinWebAngleFF, width - ParB - ParD * cosWebAngleFF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}