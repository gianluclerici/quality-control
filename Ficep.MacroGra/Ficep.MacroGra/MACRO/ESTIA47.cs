using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA47 : EyeMacro
    {
        public ESTIA47(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;
            string extrusionPlane = "A";

            double offsetY = SB / 2, topY = SB;

            double tanAlfa = ParA / (offsetY + TA / 2), radAlfa = Math.Atan(tanAlfa), sinAlfa = Math.Sin(radAlfa), cosAlfa = Math.Cos(radAlfa);
            double delta = ((offsetY - TA / 2) / tanAlfa - ParB) * sinAlfa;

            //
            // Estrusione ala sup
            //
            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(new ProgramPoint(ParB, 0));
            macroPoint.Add(new ProgramPoint(ParC, (ParB - ParC) * tanAlfa));
            macroPoint.Add(new ProgramPoint(ParC + delta * sinAlfa, (ParB - ParC) * tanAlfa + delta * cosAlfa, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, offsetY - TA / 2));
            macroPoint.Add(new ProgramPoint(ParA, topY));
            macroPoint.Add(new ProgramPoint(0, topY));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}