using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTC18 : EyeMacro
    {
        public INTC18(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //
            
            macroPoint.Add(new ProgramPoint(ParA, ParC + ParS));
            macroPoint.Add(new ProgramPoint(ParA + ParS, ParC, 0, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParA + ParB - ParS, ParD));
            macroPoint.Add(new ProgramPoint(ParA + ParB, ParD + ParS, 0, 0, ParS));

            macroPoint.Add(new ProgramPoint(ParA + ParB, ParD + ParF, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParG + ParH, ParC + ParE + ParI, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParG, ParC + ParE + ParI, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, ParC + ParE, 0, ParR));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MacroName == "INTC18" ? MirrorSideASideB : !MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}