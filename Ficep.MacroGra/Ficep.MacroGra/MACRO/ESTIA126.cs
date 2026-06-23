using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA126 : EyeMacro
    {
        public ESTIA126(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double radAlfa = ParALFA.ToRad();
            double tanAlfa = Math.Tan(radAlfa), sinAlfa = Math.Sin(radAlfa), cosAlfa = Math.Cos(radAlfa);
            double topY = SB, offsetY = SB / 2, width = SA;
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            if (!MirrorSideASideB)
            {
                macroPoint.Add(new ProgramPoint(width * tanAlfa, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(width * tanAlfa - (ParA - ParB) * sinAlfa, (ParA - ParB) * cosAlfa, 0, 0));
                macroPoint.Add(new ProgramPoint(width * tanAlfa - ParA * sinAlfa + ParB * cosAlfa, ParA * cosAlfa + ParB * sinAlfa, 0, 0));
                macroPoint.Add(new ProgramPoint(width * tanAlfa - (ParA + ParB) * sinAlfa, (ParA + ParB) * cosAlfa, 0, 0));
                if (ParC > 0)
                {
                    macroPoint.Add(new ProgramPoint(width * tanAlfa - (ParA + ParC - ParB) * sinAlfa, (ParA + ParC - ParB) * cosAlfa, 0, 0));
                    macroPoint.Add(new ProgramPoint(width * tanAlfa - (ParA + ParC) * sinAlfa + ParB * cosAlfa, (ParA + ParC) * cosAlfa + ParB * sinAlfa, 0, 0));
                    macroPoint.Add(new ProgramPoint(width * tanAlfa - (ParA + ParB + ParC) * sinAlfa, (ParA + ParB + ParC) * cosAlfa, 0, 0));
                }
            }
            else
            {
                macroPoint.Add(new ProgramPoint((ParA - ParB) * sinAlfa, (ParA - ParB) * cosAlfa, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA * sinAlfa + ParB * cosAlfa, ParA * cosAlfa - ParB * sinAlfa, 0, 0));
                macroPoint.Add(new ProgramPoint((ParA + ParB) * sinAlfa, (ParA + ParB) * cosAlfa, 0, 0));
                if (ParC > 0)
                {
                    macroPoint.Add(new ProgramPoint((ParA + ParC - ParB) * sinAlfa, (ParA + ParC - ParB) * cosAlfa, 0, 0));
                    macroPoint.Add(new ProgramPoint((ParA + ParC) * sinAlfa + ParB * cosAlfa, (ParA + ParC) * cosAlfa - ParB * sinAlfa, 0, 0));
                    macroPoint.Add(new ProgramPoint((ParA + ParB + ParC) * sinAlfa, (ParA + ParB + ParC) * cosAlfa, 0, 0));
                }
                macroPoint.Add(new ProgramPoint(width * tanAlfa, width, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}