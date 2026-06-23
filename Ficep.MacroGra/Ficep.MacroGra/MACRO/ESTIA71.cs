using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA71 : EyeMacro
    {
        public ESTIA71(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC, ParD, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParB, ParD + ParE, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, ParD + ParE, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParD + ParE + ParF, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala Side
            //
            extrusionDepth = TB;
            extrusionPlane = Side == "A" ? "B" : "A";
            
            double topY = SB;
            
            double radBeta = Math.Atan(ParI / (topY - ParM));
            
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG + ParH, ParM, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, ParM, 0, ParL));
            macroPoint.Add(new ProgramPoint(ParI, topY, 0, ParL));
            macroPoint.Add(new ProgramPoint(ParI + ParL, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            // Ho messo false al posto di mirrorSideASideB perchè introduceva un capovolgimento non necessario
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}