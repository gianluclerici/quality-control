using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA104 : EyeMacro
    {
        public ESTIA104(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA - TB;
            string extrusionPlane = Side;

            double topY = SB, offsetY = SB / 2, width = SA;

            double REG_01 = Math.Sqrt(ParR * ParR - topY * topY / 4);
            double REG_02 = ParR - REG_01;
            double REG_03 = Math.Sqrt(ParR * ParR - ParF * ParF / 4);
            double REG_04 = REG_03 - REG_01;

            if (REG_02 > 0)
            {
               //
               // Estrusione ala Side
               //
               macroPoint.Add(new ProgramPoint(0, 0, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE + ParA + ParB, 0, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE + ParA, topY / 2 - ParC - ParD, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE + ParA, topY / 2 - ParC, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE, topY / 2 - ParC, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE, topY / 2 + ParC, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE + ParA, topY / 2 + ParC, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE + ParA, topY / 2 + ParC + ParD, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE + ParA + ParB, topY, 0));
               macroPoint.Add(new ProgramPoint(0, topY, 0));

               EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
               macroPoint.Clear();

               //
               // Estrusione ala Opposite Side
               //
               extrusionDepth = TB;
               extrusionPlane = Side == "A" ? "B" : "A";
               macroPoint.Add(new ProgramPoint(0, 0, 0));
               macroPoint.Add(new ProgramPoint(REG_04, topY / 2 - ParF / 2, 0, 0, ParR));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE, topY / 2 - ParF / 2, 0));
               macroPoint.Add(new ProgramPoint(REG_02 + ParE, topY / 2 + ParF / 2, 0));
               macroPoint.Add(new ProgramPoint(REG_04, topY / 2 + ParF / 2, 0));
               macroPoint.Add(new ProgramPoint(0, topY, 0, 0, ParR));
               
               EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
               macroPoint.Clear();
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}