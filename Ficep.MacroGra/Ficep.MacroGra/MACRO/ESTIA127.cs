using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA127 : EyeMacro
    {
        public ESTIA127(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //  radAlfa is REG_01, radBeta is REG_02, gamma is REG_03
            //double radAlfa = Math.Atan((ParD - ParC) / ParB);
            //double radBeta = Math.Atan((ParA - ParB) / ParC); Forse mi sbaglio ma questo beta non è calcolato nel modo giusto
            //double gamma = ParR / Math.Tan((radAlfa + 90 + radBeta) / 2);

            double realBeta = Math.Atan(((ParB + ParF) - ParA) / ParC);
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + TB * Math.Tan(realBeta), TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF + ParB, ParC, 0, ParR));

            //In alternativa il file .MAC usa questi due punti

            //macroPoint.Add(new ProgramPoint(ParF + ParB + gamma * Math.Sin(radBeta), ParC - gamma * Math.Cos(radBeta), 0, 0));
            //macroPoint.Add(new ProgramPoint(ParF + ParB - gamma * Math.Cos(radAlfa), ParC + gamma * Math.Sin(radAlfa), 0, 0, ParR));

            macroPoint.Add(new ProgramPoint(ParF, ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione diagonale ala Side di -realBeta per combaciare con estrusione anima
            //
            extrusionDepth = TB;
            extrusionPlane = Side;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            if (ParG > 0)
            {
                macroPoint.Add(new ProgramPoint(ParA, offsetY - TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParG, offsetY - TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParG, offsetY + TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + TA / 2, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -realBeta);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}