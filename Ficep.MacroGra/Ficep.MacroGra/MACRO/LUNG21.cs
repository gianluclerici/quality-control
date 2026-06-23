using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;

namespace Ficep.MacroGra
{
    public class LUNG21 : EyeMacroLung
    {
        public LUNG21(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            string extrusionPlane = "C";
            double width = SA;

            //
            // Pezzo 1 anima
            //
            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParC));
            macroPoint.Add(new ProgramPoint(ParA + ParD, ParE));
            macroPoint.Add(new ProgramPoint(ParA, 0));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out List<IEyeCurve> curves);
            ProgrammedCurves.AddRange(curves);
            macroPoint.Clear();
            curves.Clear();

            //
            // Pezzo 2 anima
            //

            double offsetX = 0;
            if (!(ParC - ParE).IsEqualTo(0, TolLinear) && !(ParA + ParD - ParB).IsEqualTo(0, TolLinear)) // TOLGO le divisioni per 0
                offsetX = (2 * ParC - width) / ((ParC - ParE) / (ParA + ParD - ParB));
            macroPoint.Add(new ProgramPoint(2 * ParB + offsetX, width));
            macroPoint.Add(new ProgramPoint(ParB + offsetX, width - ParC));
            macroPoint.Add(new ProgramPoint(2 * ParB + offsetX - ParA - ParD, width - ParE));
            macroPoint.Add(new ProgramPoint(2 * ParB + offsetX - ParA, width));

            EyeGeometryUtils.AddCurves(macroPoint, extrusionPlane, Wp, TolLinear, TolAngle, out curves);
            ProgrammedCurves.AddRange(curves);
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            return true;
        }

    }
}