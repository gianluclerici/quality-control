using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI177 : EyeMacro
    {
        public ESTI177(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA;
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (TB - ParD) * Math.Tan(ParE.ToRad()), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, width - (TB - ParF), 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + (TB - ParF) * Math.Tan(ParG.ToRad()), width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

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